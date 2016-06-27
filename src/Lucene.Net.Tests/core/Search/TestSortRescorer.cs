using System;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestSortRescorer : LuceneTestCase
    {
        internal IndexSearcher Searcher;
        internal DirectoryReader Reader;
        internal Directory Dir;

        public TestSortRescorer() : base()
        {
            Dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), Dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            doc.Add(NewTextField("body", "some contents and more contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 5));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            doc.Add(NewTextField("body", "another document with different contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 20));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            doc.Add(NewTextField("body", "crappy contents", Field.Store.NO));
            doc.Add(new NumericDocValuesField("popularity", 2));
            iw.AddDocument(doc);

            Reader = iw.Reader;
            Searcher = new IndexSearcher(Reader);
            iw.Dispose();
        }

        public override void Dispose()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.Dispose();
        }

        [Fact]
        public virtual void TestBasic()
        {
            // create a sort field and sort by it (reverse order)
            Query query = new TermQuery(new Term("body", "contents"));
            IndexReader r = Searcher.IndexReader;

            // Just first pass query
            TopDocs hits = Searcher.Search(query, 10);
            Assert.Equal(3, hits.TotalHits);
            Assert.Equal("3", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.Equal("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
            Assert.Equal("2", r.Document(hits.ScoreDocs[2].Doc).Get("id"));

            // Now, rescore:
            Sort sort = new Sort(new SortField("popularity", SortField.Type_e.INT, true));
            Rescorer rescorer = new SortRescorer(sort);
            hits = rescorer.Rescore(Searcher, hits, 10);
            Assert.Equal(3, hits.TotalHits);
            Assert.Equal("2", r.Document(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.Equal("1", r.Document(hits.ScoreDocs[1].Doc).Get("id"));
            Assert.Equal("3", r.Document(hits.ScoreDocs[2].Doc).Get("id"));

            string expl = rescorer.Explain(Searcher, Searcher.Explain(query, hits.ScoreDocs[0].Doc), hits.ScoreDocs[0].Doc).ToString();

            // Confirm the explanation breaks out the individual
            // sort fields:
            Assert.True(expl.Contains("= sort field <int: \"popularity\">! value=20"));

            // Confirm the explanation includes first pass details:
            Assert.True(expl.Contains("= first pass score"));
            Assert.True(expl.Contains("body:contents in"));
        }

        [Fact]
        public virtual void TestRandom()
        {
            Directory dir = NewDirectory();
            int numDocs = AtLeast(1000);
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir);

            int[] idToNum = new int[numDocs];
            int maxValue = TestUtil.NextInt(Random(), 10, 1000000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                int numTokens = TestUtil.NextInt(Random(), 1, 10);
                StringBuilder b = new StringBuilder();
                for (int j = 0; j < numTokens; j++)
                {
                    b.Append("a ");
                }
                doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
                idToNum[i] = Random().Next(maxValue);
                doc.Add(new NumericDocValuesField("num", idToNum[i]));
                w.AddDocument(doc);
            }
            IndexReader r = w.Reader;
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            int numHits = TestUtil.NextInt(Random(), 1, numDocs);
            bool reverse = Random().NextBoolean();

            TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);

            Rescorer rescorer = new SortRescorer(new Sort(new SortField("num", SortField.Type_e.INT, reverse)));
            TopDocs hits2 = rescorer.Rescore(s, hits, numHits);

            int[] expected = new int[numHits];
            for (int i = 0; i < numHits; i++)
            {
                expected[i] = hits.ScoreDocs[i].Doc;
            }

            int reverseInt = reverse ? -1 : 1;

            Array.Sort(expected, new ComparatorAnonymousInnerClassHelper(this, idToNum, r, reverseInt));

            bool fail = false;
            for (int i = 0; i < numHits; i++)
            {
                fail |= (int)expected[i] != hits2.ScoreDocs[i].Doc;
            }
            Assert.False(fail);

            r.Dispose();
            dir.Dispose();
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<int>
        {
            private readonly TestSortRescorer OuterInstance;

            private int[] IdToNum;
            private IndexReader r;
            private int ReverseInt;

            public ComparatorAnonymousInnerClassHelper(TestSortRescorer outerInstance, int[] idToNum, IndexReader r, int reverseInt)
            {
                this.OuterInstance = outerInstance;
                this.IdToNum = idToNum;
                this.r = r;
                this.ReverseInt = reverseInt;
            }

            public virtual int Compare(int a, int b)
            {
                try
                {
                    int av = IdToNum[Convert.ToInt32(r.Document(a).Get("id"))];
                    int bv = IdToNum[Convert.ToInt32(r.Document(b).Get("id"))];
                    if (av < bv)
                    {
                        return -ReverseInt;
                    }
                    else if (bv < av)
                    {
                        return ReverseInt;
                    }
                    else
                    {
                        // Tie break by docID
                        return a - b;
                    }
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.Message, ioe);
                }
            }
        }
    }
}