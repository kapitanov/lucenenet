using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Xunit;

namespace Lucene.Net.Facet.Taxonomy
{

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
    using Document = Lucene.Net.Documents.Document;
    using Store = Lucene.Net.Documents.Field.Store;
    using StringField = Lucene.Net.Documents.StringField;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using NoMergePolicy = Lucene.Net.Index.NoMergePolicy;
    using Term = Lucene.Net.Index.Term;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Util;

    public class TestTaxonomyFacetCounts2 : FacetTestCase, IClassFixture<TestTaxonomyFacetCounts2Fixture>
    {
        private readonly TestTaxonomyFacetCounts2Fixture _fixture;

        private const string CP_A = TestTaxonomyFacetCounts2Fixture.CP_A;
        private const string CP_B = TestTaxonomyFacetCounts2Fixture.CP_B;
        private const string CP_C = TestTaxonomyFacetCounts2Fixture.CP_C;
        private const string CP_D = TestTaxonomyFacetCounts2Fixture.CP_D;
        private const int NUM_CHILDREN_CP_A = TestTaxonomyFacetCounts2Fixture.NUM_CHILDREN_CP_A;
        private const int NUM_CHILDREN_CP_B = TestTaxonomyFacetCounts2Fixture.NUM_CHILDREN_CP_B;
        private const int NUM_CHILDREN_CP_C = TestTaxonomyFacetCounts2Fixture.NUM_CHILDREN_CP_C;
        private const int NUM_CHILDREN_CP_D = TestTaxonomyFacetCounts2Fixture.NUM_CHILDREN_CP_D;

        public TestTaxonomyFacetCounts2(TestTaxonomyFacetCounts2Fixture fixture) : base()
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void TestDifferentNumResults()
        {
            // test the collector w/ FacetRequests and different numResults
            DirectoryReader indexReader = DirectoryReader.Open(_fixture.indexDir);
            var taxoReader = new DirectoryTaxonomyReader(_fixture.taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            TermQuery q = new TermQuery(_fixture.A);
            searcher.Search(q, sfc);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, _fixture.Config, sfc);
            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_A, CP_A);
            Assert.Equal(-1, (int)result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                var expected = _fixture.termExpectedCounts[CP_A + "/" + labelValue.label].Value;
                Assert.Equal(expected, labelValue.value);
            }
            result = facets.GetTopChildren(NUM_CHILDREN_CP_B, CP_B);
            Assert.Equal(_fixture.termExpectedCounts[CP_B].Value, result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.termExpectedCounts[CP_B + "/" + labelValue.label].Value, labelValue.value);
            }

            IOUtils.Close(indexReader, taxoReader);
        }

        [Fact]
        public virtual void TestAllCounts()
        {
            DirectoryReader indexReader = DirectoryReader.Open(_fixture.indexDir);
            var taxoReader = new DirectoryTaxonomyReader(_fixture.taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, _fixture.Config, sfc);

            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_A, CP_A);
            Assert.Equal(-1, (int)result.Value);
            int prevValue = int.MaxValue;
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_A + "/" + labelValue.label].Value, labelValue.value);
                Assert.True((int)labelValue.value <= prevValue, "wrong sort order of sub results: labelValue.value=" + labelValue.value + " prevValue=" + prevValue);
                prevValue = (int)labelValue.value;
            }

            result = facets.GetTopChildren(NUM_CHILDREN_CP_B, CP_B);
            Assert.Equal(_fixture.allExpectedCounts[CP_B].Value, result.Value);
            prevValue = int.MaxValue;
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_B + "/" + labelValue.label].Value, labelValue.value);
                Assert.True((int)labelValue.value <= prevValue, "wrong sort order of sub results: labelValue.value=" + labelValue.value + " prevValue=" + prevValue);
                prevValue = (int)labelValue.value;
            }

            IOUtils.Close(indexReader, taxoReader);
        }

        [Fact]
        public virtual void TestBigNumResults()
        {
            DirectoryReader indexReader = DirectoryReader.Open(_fixture.indexDir);
            var taxoReader = new DirectoryTaxonomyReader(_fixture.taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, _fixture.Config, sfc);

            FacetResult result = facets.GetTopChildren(int.MaxValue, CP_A);
            Assert.Equal(-1, (int)result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_A + "/" + labelValue.label].Value, labelValue.value);
            }
            result = facets.GetTopChildren(int.MaxValue, CP_B);
            Assert.Equal(_fixture.allExpectedCounts[CP_B].Value, result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_B + "/" + labelValue.label].Value, labelValue.value);
            }

            IOUtils.Close(indexReader, taxoReader);
        }

        [Fact]
        public virtual void TestNoParents()
        {
            DirectoryReader indexReader = DirectoryReader.Open(_fixture.indexDir);
            var taxoReader = new DirectoryTaxonomyReader(_fixture.taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            var sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, _fixture.Config, sfc);
            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_C, CP_C);
            Assert.Equal(_fixture.allExpectedCounts[CP_C].Value, result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_C + "/" + labelValue.label].Value, labelValue.value);
            }
            result = facets.GetTopChildren(NUM_CHILDREN_CP_D, CP_D);
            Assert.Equal(_fixture.allExpectedCounts[CP_C].Value, result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.Equal(_fixture.allExpectedCounts[CP_D + "/" + labelValue.label].Value, labelValue.value);
            }

            IOUtils.Close(indexReader, taxoReader);
        }
    }

    public class TestTaxonomyFacetCounts2Fixture : IDisposable
    {
        internal const string CP_A = "A", CP_B = "B";
        internal const string CP_C = "C", CP_D = "D"; // indexed w/ NO_PARENTS
        internal const int NUM_CHILDREN_CP_A = 5, NUM_CHILDREN_CP_B = 3;
        internal const int NUM_CHILDREN_CP_C = 5, NUM_CHILDREN_CP_D = 5;

        internal readonly FacetField[] CATEGORIES_A, CATEGORIES_B;
        internal readonly FacetField[] CATEGORIES_C, CATEGORIES_D;
        internal readonly Term A = new Term("f", "a");

        internal Net.Store.Directory indexDir { get; private set; }
        internal Net.Store.Directory taxoDir { get; private set; }

        internal IDictionary<string, int?> allExpectedCounts { get; private set; }
        internal IDictionary<string, int?> termExpectedCounts { get; private set; }

        public TestTaxonomyFacetCounts2Fixture()
        {
            CATEGORIES_A = new FacetField[NUM_CHILDREN_CP_A];
            for (int i = 0; i < NUM_CHILDREN_CP_A; i++)
            {
                CATEGORIES_A[i] = new FacetField(CP_A, Convert.ToString(i));
            }
            CATEGORIES_B = new FacetField[NUM_CHILDREN_CP_B];
            for (int i = 0; i < NUM_CHILDREN_CP_B; i++)
            {
                CATEGORIES_B[i] = new FacetField(CP_B, Convert.ToString(i));
            }

            // NO_PARENTS categories
            CATEGORIES_C = new FacetField[NUM_CHILDREN_CP_C];
            for (int i = 0; i < NUM_CHILDREN_CP_C; i++)
            {
                CATEGORIES_C[i] = new FacetField(CP_C, Convert.ToString(i));
            }

            // Multi-level categories
            CATEGORIES_D = new FacetField[NUM_CHILDREN_CP_D];
            for (int i = 0; i < NUM_CHILDREN_CP_D; i++)
            {
                string val = Convert.ToString(i);
                CATEGORIES_D[i] = new FacetField(CP_D, val, val + val); // e.g. D/1/11, D/2/22...
            }

            BeforeClassCountingFacetsAggregatorTest();
        }

        internal FacetsConfig Config
        {
            get
            {
                FacetsConfig config = new FacetsConfig();
                config.SetMultiValued("A", true);
                config.SetMultiValued("B", true);
                config.SetRequireDimCount("B", true);
                config.SetHierarchical("D", true);
                return config;
            }
        }

        private void BeforeClassCountingFacetsAggregatorTest()
        {
            indexDir = LuceneTestCase.NewDirectory();
            taxoDir = LuceneTestCase.NewDirectory();

            // create an index which has:
            // 1. Segment with no categories, but matching results
            // 2. Segment w/ categories, but no results
            // 3. Segment w/ categories and results
            // 4. Segment w/ categories, but only some results

            IndexWriterConfig conf = LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random()));
            //conf.MergePolicy = NoMergePolicy.INSTANCE; // prevent merges, so we can control the index segments
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);
            TaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);

            allExpectedCounts = newCounts();
            termExpectedCounts = newCounts();

            // segment w/ no categories
            IndexDocsNoFacets(indexWriter);

            // segment w/ categories, no content
            IndexDocsWithFacetsNoTerms(indexWriter, taxoWriter, allExpectedCounts);

            // segment w/ categories and content
            IndexDocsWithFacetsAndTerms(indexWriter, taxoWriter, allExpectedCounts);

            // segment w/ categories and some content
            IndexDocsWithFacetsAndSomeTerms(indexWriter, taxoWriter, allExpectedCounts);

            IOUtils.Close(indexWriter, taxoWriter);
        }

        // initialize expectedCounts w/ 0 for all categories
        private IDictionary<string, int?> newCounts()
        {
            IDictionary<string, int?> counts = new Dictionary<string, int?>();
            counts[CP_A] = 0;
            counts[CP_B] = 0;
            counts[CP_C] = 0;
            counts[CP_D] = 0;
            foreach (FacetField ff in CATEGORIES_A)
            {
                counts[ff.dim + "/" + ff.path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_B)
            {
                counts[ff.dim + "/" + ff.path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_C)
            {
                counts[ff.dim + "/" + ff.path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_D)
            {
                counts[ff.dim + "/" + ff.path[0]] = 0;
            }
            return counts;
        }

        private IList<FacetField> RandomCategories(Random random)
        {
            // add random categories from the two dimensions, ensuring that the same
            // category is not added twice.
            int numFacetsA = random.Next(3) + 1; // 1-3
            int numFacetsB = random.Next(2) + 1; // 1-2
            List<FacetField> categories_a = new List<FacetField>();
            categories_a.AddRange(Arrays.AsList(CATEGORIES_A));
            List<FacetField> categories_b = new List<FacetField>();
            categories_b.AddRange(Arrays.AsList(CATEGORIES_B));
            categories_a = CollectionsHelper.Shuffle(categories_a).ToList();
            categories_b = CollectionsHelper.Shuffle(categories_b).ToList();

            List<FacetField> categories = new List<FacetField>();
            categories.AddRange(categories_a.SubList(0, numFacetsA));
            categories.AddRange(categories_b.SubList(0, numFacetsB));

            // add the NO_PARENT categories
            categories.Add(CATEGORIES_C[random.Next(NUM_CHILDREN_CP_C)]);
            categories.Add(CATEGORIES_D[random.Next(NUM_CHILDREN_CP_D)]);

            return categories;
        }

        private void AddField(Document doc)
        {
            doc.Add(new StringField(A.Field, A.Text(), Store.NO));
        }

        private void AddFacets(Document doc, FacetsConfig config, bool updateTermExpectedCounts)
        {
            IList<FacetField> docCategories = RandomCategories(LuceneTestCase.Random());
            foreach (FacetField ff in docCategories)
            {
                doc.Add(ff);
                string cp = ff.dim + "/" + ff.path[0];
                allExpectedCounts[cp] = allExpectedCounts[cp] + 1;
                if (updateTermExpectedCounts)
                {
                    termExpectedCounts[cp] = termExpectedCounts[cp] + 1;
                }
            }
            // add 1 to each NO_PARENTS dimension
            allExpectedCounts[CP_B] = allExpectedCounts[CP_B] + 1;
            allExpectedCounts[CP_C] = allExpectedCounts[CP_C] + 1;
            allExpectedCounts[CP_D] = allExpectedCounts[CP_D] + 1;
            if (updateTermExpectedCounts)
            {
                termExpectedCounts[CP_B] = termExpectedCounts[CP_B] + 1;
                termExpectedCounts[CP_C] = termExpectedCounts[CP_C] + 1;
                termExpectedCounts[CP_D] = termExpectedCounts[CP_D] + 1;
            }
        }

        private void IndexDocsNoFacets(IndexWriter indexWriter)
        {
            int numDocs = LuceneTestCase.AtLeast(2);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddField(doc);
                indexWriter.AddDocument(doc);
            }
            indexWriter.Commit(); // flush a segment
        }

        private void IndexDocsWithFacetsNoTerms(IndexWriter indexWriter, TaxonomyWriter taxoWriter, IDictionary<string, int?> expectedCounts)
        {
            Random random = LuceneTestCase.Random();
            int numDocs = LuceneTestCase.AtLeast(random, 2);
            FacetsConfig config = Config;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddFacets(doc, config, false);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        private void IndexDocsWithFacetsAndTerms(IndexWriter indexWriter, TaxonomyWriter taxoWriter, IDictionary<string, int?> expectedCounts)
        {
            Random random = LuceneTestCase.Random();
            int numDocs = LuceneTestCase.AtLeast(random, 2);
            FacetsConfig config = Config;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddFacets(doc, config, true);
                AddField(doc);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        private void IndexDocsWithFacetsAndSomeTerms(IndexWriter indexWriter, TaxonomyWriter taxoWriter, IDictionary<string, int?> expectedCounts)
        {
            Random random = LuceneTestCase.Random();
            int numDocs = LuceneTestCase.AtLeast(random, 2);
            FacetsConfig config = Config;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                bool hasContent = random.NextBoolean();
                if (hasContent)
                {
                    AddField(doc);
                }
                AddFacets(doc, config, hasContent);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        public void Dispose()
        {
            IOUtils.Close(indexDir, taxoDir);
        }
    }
}