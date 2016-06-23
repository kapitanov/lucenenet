using Lucene.Net.Documents;
using Lucene.Net.Util;
using Xunit;

namespace Lucene.Net.Search
{
    using System;
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
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SpanFirstQuery = Lucene.Net.Search.Spans.SpanFirstQuery;
    using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
    using SpanNotQuery = Lucene.Net.Search.Spans.SpanNotQuery;
    using SpanOrQuery = Lucene.Net.Search.Spans.SpanOrQuery;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Tests primitive queries (ie: that rewrite to themselves) to
    /// insure they match the expected set of docs, and that the score of each
    /// match is equal to the value of the scores explanation.
    ///
    /// <p>
    /// The assumption is that if all of the "primitive" queries work well,
    /// then anything that rewrites to a primitive will work well also.
    /// </p>
    /// </summary>
    /// <seealso cref= "Subclasses for actual tests" </seealso>
    public class TestExplanations : LuceneTestCaseWithReducedFloatPrecision, IClassFixture<TestExplanationsFixture>
    {
        protected readonly TestExplanationsFixture _fixture;

        public TestExplanations(TestExplanationsFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// check the expDocNrs first, then check the query (and the explanations) </summary>
        public virtual void Qtest(Query q, int[] expDocNrs)
        {
            CheckHits.CheckHitCollector(Random(), q, TestExplanationsFixture.FIELD, _fixture.Searcher, expDocNrs);
        }

        /// <summary>
        /// Tests a query using qtest after wrapping it with both optB and reqB </summary>
        /// <seealso cref= #qtest </seealso>
        /// <seealso cref= #reqB </seealso>
        /// <seealso cref= #optB </seealso>
        public virtual void Bqtest(Query q, int[] expDocNrs)
        {
            Qtest(ReqB(q), expDocNrs);
            Qtest(OptB(q), expDocNrs);
        }

        /// <summary>
        /// Convenience subclass of FieldCacheTermsFilter
        /// </summary>
        public class ItemizedFilter : FieldCacheTermsFilter
        {
            internal static string[] Int2str(int[] terms)
            {
                string[] @out = new string[terms.Length];
                for (int i = 0; i < terms.Length; i++)
                {
                    @out[i] = "" + terms[i];
                }
                return @out;
            }

            public ItemizedFilter(string keyField, int[] keys)
                : base(keyField, Int2str(keys))
            {
            }

            public ItemizedFilter(int[] keys)
                : base(TestExplanationsFixture.KEY, Int2str(keys))
            {
            }
        }

        /// <summary>
        /// helper for generating MultiPhraseQueries </summary>
        public static Term[] Ta(string[] s)
        {
            Term[] t = new Term[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                t[i] = new Term(TestExplanationsFixture.FIELD, s[i]);
            }
            return t;
        }

        /// <summary>
        /// MACRO for SpanTermQuery </summary>
        public virtual SpanTermQuery St(string s)
        {
            return new SpanTermQuery(new Term(TestExplanationsFixture.FIELD, s));
        }

        /// <summary>
        /// MACRO for SpanNotQuery </summary>
        public virtual SpanNotQuery Snot(SpanQuery i, SpanQuery e)
        {
            return new SpanNotQuery(i, e);
        }

        /// <summary>
        /// MACRO for SpanOrQuery containing two SpanTerm queries </summary>
        public virtual SpanOrQuery Sor(string s, string e)
        {
            return Sor(St(s), St(e));
        }

        /// <summary>
        /// MACRO for SpanOrQuery containing two SpanQueries </summary>
        public virtual SpanOrQuery Sor(SpanQuery s, SpanQuery e)
        {
            return new SpanOrQuery(s, e);
        }

        /// <summary>
        /// MACRO for SpanOrQuery containing three SpanTerm queries </summary>
        public virtual SpanOrQuery Sor(string s, string m, string e)
        {
            return Sor(St(s), St(m), St(e));
        }

        /// <summary>
        /// MACRO for SpanOrQuery containing two SpanQueries </summary>
        public virtual SpanOrQuery Sor(SpanQuery s, SpanQuery m, SpanQuery e)
        {
            return new SpanOrQuery(s, m, e);
        }

        /// <summary>
        /// MACRO for SpanNearQuery containing two SpanTerm queries </summary>
        public virtual SpanNearQuery Snear(string s, string e, int slop, bool inOrder)
        {
            return Snear(St(s), St(e), slop, inOrder);
        }

        /// <summary>
        /// MACRO for SpanNearQuery containing two SpanQueries </summary>
        public virtual SpanNearQuery Snear(SpanQuery s, SpanQuery e, int slop, bool inOrder)
        {
            return new SpanNearQuery(new SpanQuery[] { s, e }, slop, inOrder);
        }

        /// <summary>
        /// MACRO for SpanNearQuery containing three SpanTerm queries </summary>
        public virtual SpanNearQuery Snear(string s, string m, string e, int slop, bool inOrder)
        {
            return Snear(St(s), St(m), St(e), slop, inOrder);
        }

        /// <summary>
        /// MACRO for SpanNearQuery containing three SpanQueries </summary>
        public virtual SpanNearQuery Snear(SpanQuery s, SpanQuery m, SpanQuery e, int slop, bool inOrder)
        {
            return new SpanNearQuery(new SpanQuery[] { s, m, e }, slop, inOrder);
        }

        /// <summary>
        /// MACRO for SpanFirst(SpanTermQuery) </summary>
        public virtual SpanFirstQuery Sf(string s, int b)
        {
            return new SpanFirstQuery(St(s), b);
        }

        /// <summary>
        /// MACRO: Wraps a Query in a BooleanQuery so that it is optional, along
        /// with a second prohibited clause which will never match anything
        /// </summary>
        public virtual Query OptB(Query q)
        {
            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(q, BooleanClause.Occur.SHOULD);
            bq.Add(new TermQuery(new Term("NEVER", "MATCH")), BooleanClause.Occur.MUST_NOT);
            return bq;
        }

        /// <summary>
        /// MACRO: Wraps a Query in a BooleanQuery so that it is required, along
        /// with a second optional clause which will match everything
        /// </summary>
        public virtual Query ReqB(Query q)
        {
            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(q, BooleanClause.Occur.MUST);
            bq.Add(new TermQuery(new Term(TestExplanationsFixture.FIELD, "w1")), BooleanClause.Occur.SHOULD);
            return bq;
        }

        /// <summary>
        /// Placeholder: JUnit freaks if you don't have one test ... making
        /// class abstract doesn't help
        /// </summary>
        [Fact]
        public virtual void TestNoop()
        {
            /* NOOP */
        }
    }

    public class TestExplanationsFixture : IDisposable
    {
        internal readonly string[] DocFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3 zz", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3 zz" };

        internal IndexSearcher Searcher;
        internal IndexReader Reader;
        internal Directory Directory;

        public const string KEY = "KEY";

        // boost on this field is the same as the iterator for the doc
        public const string FIELD = "field";

        // same contents, but no field boost
        public const string ALTFIELD = "alt";

        public TestExplanationsFixture()
        {
            var random = LuceneTestCase.Random();
            Directory = LuceneTestCase.NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, Directory, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(LuceneTestCase.NewLogMergePolicy()));
            for (int i = 0; i < DocFields.Length; i++)
            {
                Document doc = new Document();
                doc.Add(LuceneTestCase.NewStringField(KEY, "" + i, Field.Store.NO));
                Field f = LuceneTestCase.NewTextField(FIELD, DocFields[i], Field.Store.NO);
                f.Boost = i;
                doc.Add(f);
                doc.Add(LuceneTestCase.NewTextField(ALTFIELD, DocFields[i], Field.Store.NO));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = LuceneTestCase.NewSearcher(Reader);
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