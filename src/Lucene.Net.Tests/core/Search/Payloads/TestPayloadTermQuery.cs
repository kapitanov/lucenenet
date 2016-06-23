using System.IO;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search.Payloads
{
    using System;    /*
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

    using Lucene.Net.Analysis;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiSpansWrapper = Lucene.Net.Search.Spans.MultiSpansWrapper;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Spans = Lucene.Net.Search.Spans.Spans;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;

    public class TestPayloadTermQuery : LuceneTestCase, IClassFixture<TestPayloadTermQueryFixture>
    {
        private readonly TestPayloadTermQueryFixture _fixture;

        public TestPayloadTermQuery(TestPayloadTermQueryFixture fixture) : base()
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void Test()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term("field", "seventy"), new MaxPayloadFunction());
            TopDocs hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.True(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            Assert.True(hits.MaxScore == 1, hits.MaxScore + " does not equal: " + 1);
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                Assert.True(doc.Score == 1, doc.Score + " does not equal: " + 1);
            }
            CheckHits.CheckExplanations(query, PayloadHelper.FIELD, _fixture.Searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, query);
            Assert.True(spans != null, "spans is null and it shouldn't be");
            /*float score = hits.Score(0);
            for (int i =1; i < hits.Length(); i++)
            {
              Assert.True(score == hits.Score(i), "scores are not equal and they should be");
            }*/
        }

        [Fact]
        public virtual void TestQuery()
        {
            PayloadTermQuery boostingFuncTermQuery = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
            QueryUtils.Check(boostingFuncTermQuery);

            SpanTermQuery spanTermQuery = new SpanTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"));

            Assert.True(boostingFuncTermQuery.Equals(spanTermQuery) == spanTermQuery.Equals(boostingFuncTermQuery));

            PayloadTermQuery boostingFuncTermQuery2 = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new AveragePayloadFunction());

            QueryUtils.CheckUnequal(boostingFuncTermQuery, boostingFuncTermQuery2);
        }

        [Fact]
        public virtual void TestMultipleMatchesPerDoc()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
            TopDocs hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.True(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            //System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
            Assert.True(hits.MaxScore == 4.0, hits.MaxScore + " does not equal: " + 4.0);
            //there should be exactly 10 items that score a 4, all the rest should score a 2
            //The 10 items are: 70 + i*100 where i in [0-9]
            int numTens = 0;
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                if (doc.Doc % 10 == 0)
                {
                    numTens++;
                    Assert.True(doc.Score == 4.0, doc.Score + " does not equal: " + 4.0);
                }
                else
                {
                    Assert.True(doc.Score == 2, doc.Score + " does not equal: " + 2);
                }
            }
            Assert.True(numTens == 10, numTens + " does not equal: " + 10);
            CheckHits.CheckExplanations(query, "field", _fixture.Searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, query);
            Assert.True(spans != null, "spans is null and it shouldn't be");
            //should be two matches per document
            int count = 0;
            //100 hits times 2 matches per hit, we should have 200 in count
            while (spans.Next())
            {
                count++;
            }
            Assert.True(count == 200, count + " does not equal: " + 200);
        }

        //Set includeSpanScore to false, in which case just the payload score comes through.
        [Fact]
        public virtual void TestIgnoreSpanScorer()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction(), false);

            IndexReader reader = DirectoryReader.Open(_fixture.Directory);
            IndexSearcher theSearcher = NewSearcher(reader);
            theSearcher.Similarity = new TestPayloadTermQueryFixture.FullSimilarity();
            TopDocs hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.True(hits.TotalHits == 100, "hits Size: " + hits.TotalHits + " is not: " + 100);

            //they should all have the exact same score, because they all contain seventy once, and we set
            //all the other similarity factors to be 1

            //System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
            Assert.True(hits.MaxScore == 4.0, hits.MaxScore + " does not equal: " + 4.0);
            //there should be exactly 10 items that score a 4, all the rest should score a 2
            //The 10 items are: 70 + i*100 where i in [0-9]
            int numTens = 0;
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc doc = hits.ScoreDocs[i];
                if (doc.Doc % 10 == 0)
                {
                    numTens++;
                    Assert.True(doc.Score == 4.0, doc.Score + " does not equal: " + 4.0);
                }
                else
                {
                    Assert.True(doc.Score == 2, doc.Score + " does not equal: " + 2);
                }
            }
            Assert.True(numTens == 10, numTens + " does not equal: " + 10);
            CheckHits.CheckExplanations(query, "field", _fixture.Searcher, true);
            Spans spans = MultiSpansWrapper.Wrap(_fixture.Searcher.TopReaderContext, query);
            Assert.True(spans != null, "spans is null and it shouldn't be");
            //should be two matches per document
            int count = 0;
            //100 hits times 2 matches per hit, we should have 200 in count
            while (spans.Next())
            {
                count++;
            }
            reader.Dispose();
        }

        [Fact]
        public virtual void TestNoMatch()
        {
            PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.FIELD, "junk"), new MaxPayloadFunction());
            TopDocs hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.True(hits.TotalHits == 0, "hits Size: " + hits.TotalHits + " is not: " + 0);
        }

        [Fact]
        public virtual void TestNoPayload()
        {
            PayloadTermQuery q1 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "zero"), new MaxPayloadFunction());
            PayloadTermQuery q2 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "foo"), new MaxPayloadFunction());
            BooleanClause c1 = new BooleanClause(q1, BooleanClause.Occur.MUST);
            BooleanClause c2 = new BooleanClause(q2, BooleanClause.Occur.MUST_NOT);
            BooleanQuery query = new BooleanQuery();
            query.Add(c1);
            query.Add(c2);
            TopDocs hits = _fixture.Searcher.Search(query, null, 100);
            Assert.True(hits != null, "hits is null and it shouldn't be");
            Assert.True(hits.TotalHits == 1, "hits Size: " + hits.TotalHits + " is not: " + 1);
            int[] results = new int[1];
            results[0] = 0; //hits.ScoreDocs[0].Doc;
            CheckHits.CheckHitCollector(Random(), query, PayloadHelper.NO_PAYLOAD_FIELD, _fixture.Searcher, results);
        }
    }

    public class TestPayloadTermQueryFixture : IDisposable
    {
        internal Directory Directory { get; private set; }
        internal IndexSearcher Searcher { get; private set; }
        internal IndexReader Reader { get; private set; }
        internal readonly Similarity Similarity = new BoostingSimilarity();

        internal static readonly byte[] PayloadField = { 1 };
        internal static readonly byte[] PayloadMultiField1 = { 2 };
        internal static readonly byte[] PayloadMultiField2 = { 4 };

        public TestPayloadTermQueryFixture()
        {
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(LuceneTestCase.Random(), Directory, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new PayloadAnalyzer()).SetSimilarity(Similarity).SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));
            //writer.infoStream = System.out;
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                Field noPayloadField = LuceneTestCase.NewTextField(PayloadHelper.NO_PAYLOAD_FIELD, English.IntToEnglish(i), Field.Store.YES);
                //noPayloadField.setBoost(0);
                doc.Add(noPayloadField);
                doc.Add(LuceneTestCase.NewTextField("field", English.IntToEnglish(i), Field.Store.YES));
                doc.Add(LuceneTestCase.NewTextField("multiField", English.IntToEnglish(i) + "  " + English.IntToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();

            Searcher = LuceneTestCase.NewSearcher(Reader);
            Searcher.Similarity = Similarity;
        }

        public void Dispose()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }

        internal class BoostingSimilarity : DefaultSimilarity
        {
            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return 1;
            }

            // TODO: Remove warning after API has been finalized
            public override float ScorePayload(int docId, int start, int end, BytesRef payload)
            {
                //we know it is size 4 here, so ignore the offset/length
                return payload.Bytes[payload.Offset];
            }

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Make everything else 1 so we see the effect of the payload
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            public override float LengthNorm(FieldInvertState state)
            {
                return state.Boost;
            }

            public override float SloppyFreq(int distance)
            {
                return 1;
            }

            public override float Idf(long docFreq, long numDocs)
            {
                return 1;
            }

            public override float Tf(float freq)
            {
                return freq == 0 ? 0 : 1;
            }
        }

        internal class FullSimilarity : DefaultSimilarity
        {
            public virtual float ScorePayload(int docId, string fieldName, sbyte[] payload, int offset, int length)
            {
                //we know it is size 4 here, so ignore the offset/length
                return payload[offset];
            }
        }

        internal class PayloadFilter : TokenFilter
        {
            internal readonly string FieldName;
            internal int NumSeen = 0;

            internal readonly IPayloadAttribute PayloadAtt;

            public PayloadFilter(TokenStream input, string fieldName)
                : base(input)
            {
                this.FieldName = fieldName;
                PayloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool hasNext = input.IncrementToken();
                if (hasNext)
                {
                    if (FieldName.Equals("field"))
                    {
                        PayloadAtt.Payload = new BytesRef(PayloadField);
                    }
                    else if (FieldName.Equals("multiField"))
                    {
                        if (NumSeen % 2 == 0)
                        {
                            PayloadAtt.Payload = new BytesRef(PayloadMultiField1);
                        }
                        else
                        {
                            PayloadAtt.Payload = new BytesRef(PayloadMultiField2);
                        }
                        NumSeen++;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.NumSeen = 0;
            }
        }

        private class PayloadAnalyzer : Analyzer
        {
            internal PayloadAnalyzer()
                : base(PER_FIELD_REUSE_STRATEGY)
            {
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(result, fieldName));
            }
        }
    }
}