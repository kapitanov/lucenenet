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

    using Document = Lucene.Net.Documents.Document;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using System;
    using Util;    /// <summary>
                   /// Test for associations 
                   /// </summary>
    public class TestTaxonomyFacetAssociations : FacetTestCase, IClassFixture<TestTaxonomyFacetAssociationsFixture>
    {
        private readonly TestTaxonomyFacetAssociationsFixture _fixture;

        public TestTaxonomyFacetAssociations(TestTaxonomyFacetAssociationsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void TestIntSumAssociation()
        {
            FacetsCollector fc = new FacetsCollector();

            IndexSearcher searcher = NewSearcher(_fixture.Reader);
            searcher.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new TaxonomyFacetSumIntAssociations("$facets.int", _fixture.TaxoReader, _fixture.FacetsConfig, fc);
            Assert.Equal("dim=int path=[] value=-1 childCount=2\n  a (200)\n  b (150)\n", facets.GetTopChildren(10, "int").ToString());
            Assert.Equal(200, (int)facets.GetSpecificValue("int", "a")); //, "Wrong count for category 'a'!");
            Assert.Equal(150, (int)facets.GetSpecificValue("int", "b")); //, "Wrong count for category 'b'!");
        }

        [Fact]
        public virtual void TestFloatSumAssociation()
        {
            FacetsCollector fc = new FacetsCollector();

            IndexSearcher searcher = NewSearcher(_fixture.Reader);
            searcher.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new TaxonomyFacetSumFloatAssociations("$facets.float", _fixture.TaxoReader, _fixture.FacetsConfig, fc);
            assertEquals("dim=float path=[] value=-1.0 childCount=2\n  a (50.0)\n  b (9.999995)\n", facets.GetTopChildren(10, "float").ToString());
            assertEquals(50f, (float)facets.GetSpecificValue("float", "a"), 0.00001); //, "Wrong count for category 'a'!");
            assertEquals(10f, (float)facets.GetSpecificValue("float", "b"), 0.00001); //, "Wrong count for category 'b'!");
        }

        /// <summary>
        /// Make sure we can test both int and float assocs in one
        ///  index, as long as we send each to a different field. 
        /// </summary>
        [Fact]
        public virtual void TestIntAndFloatAssocation()
        {
            FacetsCollector fc = new FacetsCollector();

            IndexSearcher searcher = NewSearcher(_fixture.Reader);
            searcher.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new TaxonomyFacetSumFloatAssociations("$facets.float", _fixture.TaxoReader, _fixture.FacetsConfig, fc);
            assertEquals(50f, (float)facets.GetSpecificValue("float", "a"), 0.00001); //, "Wrong count for category 'a'!");
            assertEquals(10f, (float)facets.GetSpecificValue("float", "b"), 0.00001); //, "Wrong count for category 'b'!");

            facets = new TaxonomyFacetSumIntAssociations("$facets.int", _fixture.TaxoReader, _fixture.FacetsConfig, fc);
            Assert.Equal(200, (int)facets.GetSpecificValue("int", "a")); //, "Wrong count for category 'a'!");
            Assert.Equal(150, (int)facets.GetSpecificValue("int", "b")); //, "Wrong count for category 'b'!");
        }

        
        [Fact]
        public virtual void TestWrongIndexFieldName()
        {
            FacetsCollector fc = new FacetsCollector();

            IndexSearcher searcher = NewSearcher(_fixture.Reader);
            searcher.Search(new MatchAllDocsQuery(), fc);
            Facets facets = new TaxonomyFacetSumFloatAssociations(_fixture.TaxoReader, _fixture.FacetsConfig, fc);
            try
            {
                facets.GetSpecificValue("float");
                True(false, "should have hit exc");
            }
            catch (System.ArgumentException)
            {
                // expected
            }

            try
            {
                facets.GetTopChildren(10, "float");
                True(false, "should have hit exc");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
        }

        [Fact]
        public virtual void TestMixedTypesInSameIndexField()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            TaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);

            Document doc = new Document();
            doc.Add(new IntAssociationFacetField(14, "a", "x"));
            doc.Add(new FloatAssociationFacetField(55.0f, "b", "y"));
            try
            {
                writer.AddDocument(config.Build(taxoWriter, doc));
                True(false, "did not hit expected exception");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            IOUtils.Close(writer, taxoWriter, dir, taxoDir);
        }

        [Fact]
        public virtual void TestNoHierarchy()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            TaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("a", true);
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);

            Document doc = new Document();
            doc.Add(new IntAssociationFacetField(14, "a", "x"));
            try
            {
                writer.AddDocument(config.Build(taxoWriter, doc));
                True(false, "did not hit expected exception");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            IOUtils.Close(writer, taxoWriter, dir, taxoDir);
        }

        [Fact]
        public virtual void TestRequireDimCount()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            TaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            config.SetRequireDimCount("a", true);
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);

            Document doc = new Document();
            doc.Add(new IntAssociationFacetField(14, "a", "x"));
            try
            {
                writer.AddDocument(config.Build(taxoWriter, doc));
                True(false, "did not hit expected exception");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            IOUtils.Close(writer, taxoWriter, dir, taxoDir);
        }

        [Fact]
        public virtual void TestIntSumAssociationDrillDown()
        {
            FacetsCollector fc = new FacetsCollector();

            IndexSearcher searcher = NewSearcher(_fixture.Reader);
            DrillDownQuery q = new DrillDownQuery(_fixture.FacetsConfig);
            q.Add("int", "b");
            searcher.Search(q, fc);

            Facets facets = new TaxonomyFacetSumIntAssociations("$facets.int", _fixture.TaxoReader, _fixture.FacetsConfig, fc);
            Assert.Equal("dim=int path=[] value=-1 childCount=2\n  b (150)\n  a (100)\n", facets.GetTopChildren(10, "int").ToString());
            Assert.Equal(100, (int)facets.GetSpecificValue("int", "a")); //, "Wrong count for category 'a'!");
            Assert.Equal(150, (int)facets.GetSpecificValue("int", "b")); //, "Wrong count for category 'b'!");
        }

    }

    public class TestTaxonomyFacetAssociationsFixture : IDisposable
    {
        internal Store.Directory Directory { get; private set; }
        internal IndexReader Reader { get; private set; }
        internal Store.Directory TaxoDirectory { get; private set; }
        internal TaxonomyReader TaxoReader { get; private set; }
        internal FacetsConfig FacetsConfig { get; private set; }

        public TestTaxonomyFacetAssociationsFixture()
        {
            Directory = LuceneTestCase.NewDirectory();
            TaxoDirectory = LuceneTestCase.NewDirectory();
            // preparations - index, taxonomy, content

            var taxoWriter = new DirectoryTaxonomyWriter(TaxoDirectory);

            // Cannot mix ints & floats in the same indexed field:
            FacetsConfig = new FacetsConfig();
            FacetsConfig.SetIndexFieldName("int", "$facets.int");
            FacetsConfig.SetMultiValued("int", true);
            FacetsConfig.SetIndexFieldName("float", "$facets.float");
            FacetsConfig.SetMultiValued("float", true);

            var writer = new RandomIndexWriter(LuceneTestCase.Random(), Directory);

            // index documents, 50% have only 'b' and all have 'a'
            for (int i = 0; i < 110; i++)
            {
                Document doc = new Document();
                // every 11th document is added empty, this used to cause the association
                // aggregators to go into an infinite loop
                if (i % 11 != 0)
                {
                    doc.Add(new IntAssociationFacetField(2, "int", "a"));
                    doc.Add(new FloatAssociationFacetField(0.5f, "float", "a"));
                    if (i % 2 == 0) // 50
                    {
                        doc.Add(new IntAssociationFacetField(3, "int", "b"));
                        doc.Add(new FloatAssociationFacetField(0.2f, "float", "b"));
                    }
                }
                writer.AddDocument(FacetsConfig.Build(taxoWriter, doc));
            }

            taxoWriter.Dispose();
            Reader = writer.Reader;
            writer.Dispose();
            TaxoReader = new DirectoryTaxonomyReader(TaxoDirectory);
        }

        public void Dispose()
        {
            Reader.Dispose();
            Reader = null;
            Directory.Dispose();
            Directory = null;
            TaxoReader.Dispose(true);
            TaxoReader = null;
            TaxoDirectory.Dispose();
            TaxoDirectory = null;
        }
    }
}