using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using System;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using Xunit;
    using ChildScorer = Lucene.Net.Search.Scorer.ChildScorer;

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
    using Occur = Lucene.Net.Search.BooleanClause.Occur;

    public class TestSubScorerFreqs : LuceneTestCase, IClassFixture<TestSubScorerFreqsFixture>
    {
        private const float FLOAT_TOLERANCE = 0.00001F;
        private readonly TestSubScorerFreqsFixture _fixture;

        public TestSubScorerFreqs(TestSubScorerFreqsFixture fixture)
        {
            _fixture = fixture;
        }

        private class CountingCollector : Collector
        {
            internal readonly Collector Other;
            internal int DocBase;

            public readonly IDictionary<int?, IDictionary<Query, float?>> DocCounts = new Dictionary<int?, IDictionary<Query, float?>>();

            internal readonly IDictionary<Query, Scorer> SubScorers = new Dictionary<Query, Scorer>();
            internal readonly ISet<string> Relationships;

            public CountingCollector(Collector other)
                : this(other, new HashSet<string> { "MUST", "SHOULD", "MUST_NOT" })
            {
            }

            public CountingCollector(Collector other, ISet<string> relationships)
            {
                this.Other = other;
                this.Relationships = relationships;
            }

            public override Scorer Scorer
            {
                set
                {
                    Other.Scorer = value;
                    SubScorers.Clear();
                    SetSubScorers(value, "TOP");
                }
            }

            public virtual void SetSubScorers(Scorer scorer, string relationship)
            {
                foreach (ChildScorer child in scorer.Children)
                {
                    if (scorer is AssertingScorer || Relationships.Contains(child.Relationship))
                    {
                        SetSubScorers(child.Child, child.Relationship);
                    }
                }
                SubScorers[scorer.Weight.Query] = scorer;
            }

            public override void Collect(int doc)
            {
                IDictionary<Query, float?> freqs = new Dictionary<Query, float?>();
                foreach (KeyValuePair<Query, Scorer> ent in SubScorers)
                {
                    Scorer value = ent.Value;
                    int matchId = value.DocID();
                    freqs[ent.Key] = matchId == doc ? value.Freq() : 0.0f;
                }
                DocCounts[doc + DocBase] = freqs;
                Other.Collect(doc);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    DocBase = value.DocBase;
                    Other.NextReader = value;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return Other.AcceptsDocsOutOfOrder();
            }
        }

        [Fact]
        public virtual void TestTermQuery()
        {
            TermQuery q = new TermQuery(new Term("f", "d"));
            CountingCollector c = new CountingCollector(TopScoreDocCollector.Create(10, true));
            _fixture.Searcher.Search(q, null, c);
            int maxDocs = _fixture.Searcher.IndexReader.MaxDoc;
            Assert.Equal(maxDocs, c.DocCounts.Count);
            for (int i = 0; i < maxDocs; i++)
            {
                IDictionary<Query, float?> doc0 = c.DocCounts[i];
                Assert.Equal(1, doc0.Count);

                Assert.NotNull(doc0[q]);
                assertEquals(4.0F, doc0[q].Value, FLOAT_TOLERANCE);

                IDictionary<Query, float?> doc1 = c.DocCounts[++i];
                Assert.Equal(1, doc1.Count);

                Assert.NotNull(doc1[q]);
                assertEquals(1.0F, doc1[q].Value, FLOAT_TOLERANCE);
            }
        }

        [Fact]
        public virtual void TestBooleanQuery()
        {
            TermQuery aQuery = new TermQuery(new Term("f", "a"));
            TermQuery dQuery = new TermQuery(new Term("f", "d"));
            TermQuery cQuery = new TermQuery(new Term("f", "c"));
            TermQuery yQuery = new TermQuery(new Term("f", "y"));

            BooleanQuery query = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();

            inner.Add(cQuery, Occur.SHOULD);
            inner.Add(yQuery, Occur.MUST_NOT);
            query.Add(inner, Occur.MUST);
            query.Add(aQuery, Occur.MUST);
            query.Add(dQuery, Occur.MUST);

            // Only needed in Java6; Java7+ has a @SafeVarargs annotated Arrays#asList()!
            // see http://docs.oracle.com/javase/7/docs/api/java/lang/SafeVarargs.html
            IEnumerable<ISet<string>> occurList = Arrays.AsList(Collections.Singleton("MUST"), new HashSet<string>(Arrays.AsList("MUST", "SHOULD")));

            foreach (var occur in occurList)
            {
                var c = new CountingCollector(TopScoreDocCollector.Create(10, true), occur);
                _fixture.Searcher.Search(query, null, c);
                int maxDocs = _fixture.Searcher.IndexReader.MaxDoc;
                Assert.Equal(maxDocs, c.DocCounts.Count);
                bool includeOptional = occur.Contains("SHOULD");

                float value10 = 1.0F;
                float value30 = 3.0F;
                float value40 = 4.0F;

                for (int i = 0; i < maxDocs; i++)
                {
                    IDictionary<Query, float?> doc0 = c.DocCounts[i];
                    Assert.Equal(includeOptional ? 5 : 4, doc0.Count);

                    Assert.True(doc0[aQuery].HasValue);
                    assertEquals(value10, doc0[aQuery].Value, FLOAT_TOLERANCE);

                    Assert.True(doc0[dQuery].HasValue);
                    assertEquals(value40, doc0[dQuery].Value, FLOAT_TOLERANCE);

                    if (includeOptional)
                    {
                        Assert.True(doc0[cQuery].HasValue);
                        assertEquals(value30, doc0[cQuery].Value, FLOAT_TOLERANCE);
                    }

                    IDictionary<Query, float?> doc1 = c.DocCounts[++i];
                    Assert.Equal(includeOptional ? 5 : 4, doc1.Count);

                    Assert.True(doc1[aQuery].HasValue);
                    assertEquals(value10, doc1[aQuery].Value, FLOAT_TOLERANCE);

                    Assert.True(doc1[dQuery].HasValue);
                    assertEquals(value10, doc1[dQuery].Value, FLOAT_TOLERANCE);

                    if (includeOptional)
                    {
                        Assert.True(doc1[cQuery].HasValue);
                        assertEquals(value10, doc1[cQuery].Value, FLOAT_TOLERANCE);
                    }
                }
            }
        }

        [Fact]
        public virtual void TestPhraseQuery()
        {
            PhraseQuery q = new PhraseQuery();
            q.Add(new Term("f", "b"));
            q.Add(new Term("f", "c"));
            CountingCollector c = new CountingCollector(TopScoreDocCollector.Create(10, true));
            _fixture.Searcher.Search(q, null, c);
            int maxDocs = _fixture.Searcher.IndexReader.MaxDoc;
            Assert.Equal(maxDocs, c.DocCounts.Count);

            float value10 = 1.0F;
            float value20 = 2.0F;

            for (int i = 0; i < maxDocs; i++)
            {
                IDictionary<Query, float?> doc0 = c.DocCounts[i];
                Assert.Equal(1, doc0.Count);

                Assert.True(doc0[q].HasValue);
                assertEquals(value20, doc0[q].Value, FLOAT_TOLERANCE);

                IDictionary<Query, float?> doc1 = c.DocCounts[++i];
                Assert.Equal(1, doc1.Count);

                Assert.True(doc1[q].HasValue);
                assertEquals(value10, doc1[q].Value, FLOAT_TOLERANCE);
            }
        }
    }

    public class TestSubScorerFreqsFixture : IDisposable
    {
        internal Directory Dir { get; private set; }
        internal IndexSearcher Searcher { get; private set; }

        public TestSubScorerFreqsFixture()
        {
            var random = LuceneTestCase.Random();
            Dir = new RAMDirectory();
            RandomIndexWriter w = new RandomIndexWriter(random, Dir, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random))
                .SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));

            // make sure we have more than one segment occationally
            int num = LuceneTestCase.AtLeast(31);
            for (int i = 0; i < num; i++)
            {
                Documents.Document doc = new Documents.Document();
                doc.Add(LuceneTestCase.NewTextField("f", "a b c d b c d c d d", Field.Store.NO));
                w.AddDocument(doc);

                doc = new Documents.Document();
                doc.Add(LuceneTestCase.NewTextField("f", "a b c d", Field.Store.NO));
                w.AddDocument(doc);
            }

            Searcher = LuceneTestCase.NewSearcher(w.Reader);
            w.Dispose();
        }

        public void Dispose()
        {
            Searcher.IndexReader.Dispose();
            Searcher = null;
            Dir.Dispose();
            Dir = null;
        }

    }
}