using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Search.Spans
{
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
    /// Tests for <seealso cref="SpanMultiTermQueryWrapper"/>, wrapping a few MultiTermQueries.
    /// </summary>
    public class TestSpanMultiTermQueryWrapper : LuceneTestCase
    {
        private Directory Directory;
        private IndexReader Reader;
        private IndexSearcher Searcher;

        public TestSpanMultiTermQueryWrapper() : base()
        {
            Directory = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), Directory);
            Document doc = new Document();
            Field field = NewTextField("field", "", Field.Store.NO);
            doc.Add(field);

            field.StringValue = "quick brown fox";
            iw.AddDocument(doc);
            field.StringValue = "jumps over lazy broun dog";
            iw.AddDocument(doc);
            field.StringValue = "jumps over extremely very lazy broxn dog";
            iw.AddDocument(doc);
            Reader = iw.Reader;
            iw.Dispose();
            Searcher = NewSearcher(Reader);
        }

        public override void Dispose()
        {
            Reader.Dispose();
            Directory.Dispose();
            base.Dispose();
        }

        [Fact]
        public virtual void TestWildcard()
        {
            WildcardQuery wq = new WildcardQuery(new Term("field", "bro?n"));
            SpanQuery swq = new SpanMultiTermQueryWrapper<MultiTermQuery>(wq);
            // will only match quick brown fox
            SpanFirstQuery sfq = new SpanFirstQuery(swq, 2);
            Assert.Equal(1, Searcher.Search(sfq, 10).TotalHits);
        }

        [Fact]
        public virtual void TestPrefix()
        {
            WildcardQuery wq = new WildcardQuery(new Term("field", "extrem*"));
            SpanQuery swq = new SpanMultiTermQueryWrapper<MultiTermQuery>(wq);
            // will only match "jumps over extremely very lazy broxn dog"
            SpanFirstQuery sfq = new SpanFirstQuery(swq, 3);
            Assert.Equal(1, Searcher.Search(sfq, 10).TotalHits);
        }

        [Fact]
        public virtual void TestFuzzy()
        {
            FuzzyQuery fq = new FuzzyQuery(new Term("field", "broan"));
            SpanQuery sfq = new SpanMultiTermQueryWrapper<MultiTermQuery>(fq);
            // will not match quick brown fox
            SpanPositionRangeQuery sprq = new SpanPositionRangeQuery(sfq, 3, 6);
            Assert.Equal(2, Searcher.Search(sprq, 10).TotalHits);
        }

        [Fact]
        public virtual void TestFuzzy2()
        {
            // maximum of 1 term expansion
            FuzzyQuery fq = new FuzzyQuery(new Term("field", "broan"), 1, 0, 1, false);
            SpanQuery sfq = new SpanMultiTermQueryWrapper<MultiTermQuery>(fq);
            // will only match jumps over lazy broun dog
            SpanPositionRangeQuery sprq = new SpanPositionRangeQuery(sfq, 0, 100);
            Assert.Equal(1, Searcher.Search(sprq, 10).TotalHits);
        }

        [Fact]
        public virtual void TestNoSuchMultiTermsInNear()
        {
            //test to make sure non existent multiterms aren't throwing null pointer exceptions
            FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false);
            SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(fuzzyNoSuch);
            SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
            SpanQuery near = new SpanNearQuery(new SpanQuery[] { term, spanNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);
            //flip order
            near = new SpanNearQuery(new SpanQuery[] { spanNoSuch, term }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
            SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(wcNoSuch);
            near = new SpanNearQuery(new SpanQuery[] { term, spanWCNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
            SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(rgxNoSuch);
            near = new SpanNearQuery(new SpanQuery[] { term, spanRgxNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
            SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(prfxNoSuch);
            near = new SpanNearQuery(new SpanQuery[] { term, spanPrfxNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            //test single noSuch
            near = new SpanNearQuery(new SpanQuery[] { spanPrfxNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            //test double noSuch
            near = new SpanNearQuery(new SpanQuery[] { spanPrfxNoSuch, spanPrfxNoSuch }, 1, true);
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);
        }

        [Fact]
        public virtual void TestNoSuchMultiTermsInNotNear()
        {
            //test to make sure non existent multiterms aren't throwing non-matching field exceptions
            FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false);
            SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(fuzzyNoSuch);
            SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
            SpanNotQuery notNear = new SpanNotQuery(term, spanNoSuch, 0, 0);
            Assert.Equal(1, Searcher.Search(notNear, 10).TotalHits);

            //flip
            notNear = new SpanNotQuery(spanNoSuch, term, 0, 0);
            Assert.Equal(0, Searcher.Search(notNear, 10).TotalHits);

            //both noSuch
            notNear = new SpanNotQuery(spanNoSuch, spanNoSuch, 0, 0);
            Assert.Equal(0, Searcher.Search(notNear, 10).TotalHits);

            WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
            SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(wcNoSuch);
            notNear = new SpanNotQuery(term, spanWCNoSuch, 0, 0);
            Assert.Equal(1, Searcher.Search(notNear, 10).TotalHits);

            RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
            SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(rgxNoSuch);
            notNear = new SpanNotQuery(term, spanRgxNoSuch, 1, 1);
            Assert.Equal(1, Searcher.Search(notNear, 10).TotalHits);

            PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
            SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(prfxNoSuch);
            notNear = new SpanNotQuery(term, spanPrfxNoSuch, 1, 1);
            Assert.Equal(1, Searcher.Search(notNear, 10).TotalHits);
        }

        [Fact]
        public virtual void TestNoSuchMultiTermsInOr()
        {
            //test to make sure non existent multiterms aren't throwing null pointer exceptions
            FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false);
            SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(fuzzyNoSuch);
            SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
            SpanOrQuery near = new SpanOrQuery(new SpanQuery[] { term, spanNoSuch });
            Assert.Equal(1, Searcher.Search(near, 10).TotalHits);

            //flip
            near = new SpanOrQuery(new SpanQuery[] { spanNoSuch, term });
            Assert.Equal(1, Searcher.Search(near, 10).TotalHits);

            WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
            SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(wcNoSuch);
            near = new SpanOrQuery(new SpanQuery[] { term, spanWCNoSuch });
            Assert.Equal(1, Searcher.Search(near, 10).TotalHits);

            RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
            SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(rgxNoSuch);
            near = new SpanOrQuery(new SpanQuery[] { term, spanRgxNoSuch });
            Assert.Equal(1, Searcher.Search(near, 10).TotalHits);

            PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
            SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(prfxNoSuch);
            near = new SpanOrQuery(new SpanQuery[] { term, spanPrfxNoSuch });
            Assert.Equal(1, Searcher.Search(near, 10).TotalHits);

            near = new SpanOrQuery(new SpanQuery[] { spanPrfxNoSuch });
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);

            near = new SpanOrQuery(new SpanQuery[] { spanPrfxNoSuch, spanPrfxNoSuch });
            Assert.Equal(0, Searcher.Search(near, 10).TotalHits);
        }

        [Fact]
        public virtual void TestNoSuchMultiTermsInSpanFirst()
        {
            //this hasn't been a problem
            FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false);
            SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(fuzzyNoSuch);
            SpanQuery spanFirst = new SpanFirstQuery(spanNoSuch, 10);

            Assert.Equal(0, Searcher.Search(spanFirst, 10).TotalHits);

            WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
            SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(wcNoSuch);
            spanFirst = new SpanFirstQuery(spanWCNoSuch, 10);
            Assert.Equal(0, Searcher.Search(spanFirst, 10).TotalHits);

            RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
            SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(rgxNoSuch);
            spanFirst = new SpanFirstQuery(spanRgxNoSuch, 10);
            Assert.Equal(0, Searcher.Search(spanFirst, 10).TotalHits);

            PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
            SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<MultiTermQuery>(prfxNoSuch);
            spanFirst = new SpanFirstQuery(spanPrfxNoSuch, 10);
            Assert.Equal(0, Searcher.Search(spanFirst, 10).TotalHits);
        }
    }
}