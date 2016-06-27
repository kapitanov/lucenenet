using System;
using System.Text;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search
{
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class BaseTestRangeFilter : LuceneTestCase, IClassFixture<BaseTestRangeFilterFixture>
    {
        public const bool F = false;
        public const bool T = true;

        internal static readonly int IntLength = Convert.ToString(int.MaxValue).Length;

        protected readonly BaseTestRangeFilterFixture _fixture;

        public BaseTestRangeFilter(BaseTestRangeFilterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void TestPad()
        {
            int[] tests = new int[] { -9999999, -99560, -100, -3, -1, 0, 3, 9, 10, 1000, 999999999 };
            for (int i = 0; i < tests.Length - 1; i++)
            {
                int a = tests[i];
                int b = tests[i + 1];
                string aa = Pad(a);
                string bb = Pad(b);
                string label = a + ":" + aa + " vs " + b + ":" + bb;
                Assert.Equal(aa.Length, bb.Length); //, "i=" + i + ": length of " + label);
                Assert.True(System.String.Compare(aa, bb, System.StringComparison.Ordinal) < 0, "i=" + i + ": compare less than " + label);
            }
        }

        /// <summary>
        /// a simple padding function that should work with any int
        /// </summary>
        public static string Pad(int n)
        {
            StringBuilder b = new StringBuilder(40);
            string p = "0";
            if (n < 0)
            {
                p = "-";
                n = int.MaxValue + n + 1;
            }
            b.Append(p);
            string s = Convert.ToString(n);
            for (int i = s.Length; i <= IntLength; i++)
            {
                b.Append("0");
            }
            b.Append(s);

            return b.ToString();
        }
    }

    public class BaseTestRangeFilterFixture : IDisposable
    {
        internal IndexReader SignedIndexReader { get; private set; }
        internal IndexReader UnsignedIndexReader { get; private set; }

        internal TestIndex SignedIndexDir { get; private set; }
        internal TestIndex UnsignedIndexDir { get; private set; }

        internal int MinId { get; private set; }
        internal int MaxId { get; private set; }

        public BaseTestRangeFilterFixture()
        {
            var random = LuceneTestCase.Random();

            MinId = 0;
            MaxId = LuceneTestCase.AtLeast(500);

            SignedIndexDir = new TestIndex(random, int.MaxValue, int.MinValue, true);
            UnsignedIndexDir = new TestIndex(random, int.MaxValue, 0, false);
            SignedIndexReader = Build(random, SignedIndexDir);
            UnsignedIndexReader = Build(random, UnsignedIndexDir);
        }

        private IndexReader Build(Random random, TestIndex index)
        {
            /* build an index */

            Document doc = new Document();
            Field idField = LuceneTestCase.NewStringField(random, "id", "", Field.Store.YES);
            Field randField = LuceneTestCase.NewStringField(random, "rand", "", Field.Store.YES);
            Field bodyField = LuceneTestCase.NewStringField(random, "body", "", Field.Store.NO);
            doc.Add(idField);
            doc.Add(randField);
            doc.Add(bodyField);

            RandomIndexWriter writer = new RandomIndexWriter(random, index.Index, LuceneTestCase.NewIndexWriterConfig(random, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random))
                .SetOpenMode(OpenMode.CREATE)
                .SetMaxBufferedDocs(TestUtil.NextInt(random, 50, 1000))
                .SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));

            TestUtil.ReduceOpenFiles(writer.w);

            while (true)
            {
                int minCount = 0;
                int maxCount = 0;

                for (int d = MinId; d <= MaxId; d++)
                {
                    idField.StringValue = BaseTestRangeFilter.Pad(d);
                    int r = index.AllowNegativeRandomInts ? random.Next() : random.Next(int.MaxValue);
                    if (index.MaxR < r)
                    {
                        index.MaxR = r;
                        maxCount = 1;
                    }
                    else if (index.MaxR == r)
                    {
                        maxCount++;
                    }

                    if (r < index.MinR)
                    {
                        index.MinR = r;
                        minCount = 1;
                    }
                    else if (r == index.MinR)
                    {
                        minCount++;
                    }
                    randField.StringValue = BaseTestRangeFilter.Pad(r);
                    bodyField.StringValue = "body";
                    writer.AddDocument(doc);
                }

                if (minCount == 1 && maxCount == 1)
                {
                    // our subclasses rely on only 1 doc having the min or
                    // max, so, we loop until we satisfy that.  it should be
                    // exceedingly rare (Yonik calculates 1 in ~429,000)
                    // times) that this loop requires more than one try:
                    IndexReader ir = writer.Reader;
                    writer.Dispose();
                    return ir;
                }

                // try again
                writer.DeleteAll();
            }
        }

        public void Dispose()
        {
            SignedIndexReader.Dispose();
            UnsignedIndexReader.Dispose();
            SignedIndexDir.Index.Dispose();
            UnsignedIndexDir.Index.Dispose();
            SignedIndexReader = null;
            UnsignedIndexReader = null;
            SignedIndexDir = null;
            UnsignedIndexDir = null;
        }
    }

    /// <summary>
    /// Collation interacts badly with hyphens -- collation produces different
    /// ordering than Unicode code-point ordering -- so two indexes are created:
    /// one which can't have negative random integers, for testing collated ranges,
    /// and the other which can have negative random integers, for all other tests.
    /// </summary>
    internal class TestIndex
    {
        internal int MaxR;
        internal int MinR;
        internal bool AllowNegativeRandomInts;
        internal Directory Index;

        internal TestIndex(Random random, int minR, int maxR, bool allowNegativeRandomInts)
        {
            this.MinR = minR;
            this.MaxR = maxR;
            this.AllowNegativeRandomInts = allowNegativeRandomInts;
            Index = LuceneTestCase.NewDirectory(random);
        }
    }
}