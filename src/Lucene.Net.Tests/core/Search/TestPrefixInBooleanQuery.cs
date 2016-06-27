using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using System;
    using Xunit;
    using Directory = Lucene.Net.Store.Directory;

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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// https://issues.apache.org/jira/browse/LUCENE-1974
    ///
    /// represent the bug of
    ///
    ///    BooleanScorer.Score(Collector collector, int max, int firstDocID)
    ///
    /// Line 273, end=8192, subScorerDocID=11378, then more got false?
    /// </summary>
    public class TestPrefixInBooleanQuery : LuceneTestCase, IClassFixture<TestPrefixInBooleanQueryFixture>
    {
        internal const string FIELD = "name";
        private readonly TestPrefixInBooleanQueryFixture _fixture;

        public TestPrefixInBooleanQuery(TestPrefixInBooleanQueryFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void TestPrefixQuery()
        {
            Query query = new PrefixQuery(new Term(FIELD, "tang"));
            Assert.Equal(2, _fixture.Searcher.Search(query, null, 1000).TotalHits); //, "Number of matched documents");
        }

        [Fact]
        public virtual void TestTermQuery()
        {
            Query query = new TermQuery(new Term(FIELD, "tangfulin"));
            Assert.Equal(2, _fixture.Searcher.Search(query, null, 1000).TotalHits); //, "Number of matched documents");
        }

        [Fact]
        public virtual void TestTermBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(FIELD, "tangfulin")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur.SHOULD);
            Assert.Equal(2, _fixture.Searcher.Search(query, null, 1000).TotalHits); //, "Number of matched documents");
        }

        [Fact]
        public virtual void TestPrefixBooleanQuery()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new PrefixQuery(new Term(FIELD, "tang")), BooleanClause.Occur.SHOULD);
            query.Add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur.SHOULD);
            Assert.Equal(2, _fixture.Searcher.Search(query, null, 1000).TotalHits); //, "Number of matched documents");
        }
    }

    public class TestPrefixInBooleanQueryFixture : IDisposable
    {
        internal Directory Directory { get; private set; }
        internal IndexReader Reader { get; private set; }
        internal IndexSearcher Searcher { get; private set; }

        public TestPrefixInBooleanQueryFixture()
        {
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(LuceneTestCase.Random(), Directory);

            Document doc = new Document();
            Field field = LuceneTestCase.NewStringField(TestPrefixInBooleanQuery.FIELD, "meaninglessnames", Field.Store.NO);
            doc.Add(field);

            for (int i = 0; i < 5137; ++i)
            {
                writer.AddDocument(doc);
            }

            field.StringValue = "tangfulin";
            writer.AddDocument(doc);

            field.StringValue = "meaninglessnames";
            for (int i = 5138; i < 11377; ++i)
            {
                writer.AddDocument(doc);
            }

            field.StringValue = "tangfulin";
            writer.AddDocument(doc);

            Reader = writer.Reader;
            Searcher = LuceneTestCase.NewSearcher(Reader);
            writer.Dispose();
        }

        public void Dispose()
        {
            Searcher = null;
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
        }
    }
}