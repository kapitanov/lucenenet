using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Xunit;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

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
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestSegmentReader : LuceneTestCase
    {
        private Directory Dir;
        private Document TestDoc;
        private SegmentReader Reader;

        //TODO: Setup the reader w/ multiple documents
        public TestSegmentReader() : base()
        {
            Dir = NewDirectory();
            TestDoc = new Document();
            DocHelper.SetupDoc(TestDoc);
            SegmentCommitInfo info = DocHelper.WriteDoc(Random(), Dir, TestDoc);
            Reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.READ);
        }

        public override void Dispose()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.Dispose();
        }

        [Fact]
        public virtual void Test()
        {
            Assert.True(Dir != null);
            Assert.True(Reader != null);
            Assert.True(DocHelper.NameValues.Count > 0);
            Assert.True(DocHelper.NumFields(TestDoc) == DocHelper.All.Count);
        }

        [Fact]
        public virtual void TestDocument()
        {
            Assert.True(Reader.NumDocs == 1);
            Assert.True(Reader.MaxDoc >= 1);
            Document result = Reader.Document(0);
            Assert.True(result != null);
            //There are 2 unstored fields on the document that are not preserved across writing
            Assert.True(DocHelper.NumFields(result) == DocHelper.NumFields(TestDoc) - DocHelper.Unstored.Count);

            IList<IndexableField> fields = result.Fields;
            foreach (IndexableField field in fields)
            {
                Assert.True(field != null);
                Assert.True(DocHelper.NameValues.ContainsKey(field.Name()));
            }
        }

        [Fact]
        public virtual void TestGetFieldNameVariations()
        {
            ICollection<string> allFieldNames = new HashSet<string>();
            ICollection<string> indexedFieldNames = new HashSet<string>();
            ICollection<string> notIndexedFieldNames = new HashSet<string>();
            ICollection<string> tvFieldNames = new HashSet<string>();
            ICollection<string> noTVFieldNames = new HashSet<string>();

            foreach (FieldInfo fieldInfo in Reader.FieldInfos)
            {
                string name = fieldInfo.Name;
                allFieldNames.Add(name);
                if (fieldInfo.Indexed)
                {
                    indexedFieldNames.Add(name);
                }
                else
                {
                    notIndexedFieldNames.Add(name);
                }
                if (fieldInfo.HasVectors())
                {
                    tvFieldNames.Add(name);
                }
                else if (fieldInfo.Indexed)
                {
                    noTVFieldNames.Add(name);
                }
            }

            Assert.True(allFieldNames.Count == DocHelper.All.Count);
            foreach (string s in allFieldNames)
            {
                Assert.True(DocHelper.NameValues.ContainsKey(s) == true || s.Equals(""));
            }

            Assert.True(indexedFieldNames.Count == DocHelper.Indexed.Count);
            foreach (string s in indexedFieldNames)
            {
                Assert.True(DocHelper.Indexed.ContainsKey(s) == true || s.Equals(""));
            }

            Assert.True(notIndexedFieldNames.Count == DocHelper.Unindexed.Count);
            //Get all indexed fields that are storing term vectors
            Assert.True(tvFieldNames.Count == DocHelper.Termvector.Count);

            Assert.True(noTVFieldNames.Count == DocHelper.Notermvector.Count);
        }

        [Fact]
        public virtual void TestTerms()
        {
            Fields fields = MultiFields.GetFields(Reader);
            foreach (string field in fields)
            {
                Terms terms = fields.Terms(field);
                Assert.NotNull(terms);
                TermsEnum termsEnum = terms.Iterator(null);
                while (termsEnum.Next() != null)
                {
                    BytesRef term = termsEnum.Term();
                    Assert.True(term != null);
                    string fieldValue = (string)DocHelper.NameValues[field];
                    Assert.True(fieldValue.IndexOf(term.Utf8ToString()) != -1);
                }
            }

            DocsEnum termDocs = TestUtil.Docs(Random(), Reader, DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(Reader), null, 0);
            Assert.True(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            termDocs = TestUtil.Docs(Random(), Reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper.NO_NORMS_TEXT), MultiFields.GetLiveDocs(Reader), null, 0);

            Assert.True(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(Reader, MultiFields.GetLiveDocs(Reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
            // NOTE: prior rev of this test was failing to first
            // call next here:
            Assert.True(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.True(positions.DocID() == 0);
            Assert.True(positions.NextPosition() >= 0);
        }

        [Fact]
        public virtual void TestNorms()
        {
            //TODO: Not sure how these work/should be tested
            /*
                try {
                  byte [] norms = reader.norms(DocHelper.TEXT_FIELD_1_KEY);
                  System.out.println("Norms: " + norms);
                  Assert.True(norms != null);
                } catch (IOException e) {
                  e.printStackTrace();
                  Assert.True(false);
                }
            */

            CheckNorms(Reader);
        }

        public static void CheckNorms(AtomicReader reader)
        {
            // test omit norms
            for (int i = 0; i < DocHelper.Fields.Length; i++)
            {
                IndexableField f = DocHelper.Fields[i];
                if (f.FieldType().Indexed)
                {
                    Assert.Equal(reader.GetNormValues(f.Name()) != null, !f.FieldType().OmitNorms);
                    Assert.Equal(reader.GetNormValues(f.Name()) != null, !DocHelper.NoNorms.ContainsKey(f.Name()));
                    if (reader.GetNormValues(f.Name()) == null)
                    {
                        // test for norms of null
                        NumericDocValues norms = MultiDocValues.GetNormValues(reader, f.Name());
                        Assert.Null(norms);
                    }
                }
            }
        }

        [Fact]
        public virtual void TestTermVectors()
        {
            Terms result = Reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.NotNull(result);
            Assert.Equal(3, result.Size());
            TermsEnum termsEnum = result.Iterator(null);
            while (termsEnum.Next() != null)
            {
                string term = termsEnum.Term().Utf8ToString();
                int freq = (int)termsEnum.TotalTermFreq();
                Assert.True(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
                Assert.True(freq > 0);
            }

            Fields results = Reader.GetTermVectors(0);
            Assert.True(results != null);
            Assert.Equal(3, results.Size); //, "We do not have 3 term freq vectors");
        }

        [Fact]
        public virtual void TestOutOfBoundsAccess()
        {
            int numDocs = Reader.MaxDoc;
            Assert.Throws<System.IndexOutOfRangeException>(() => Reader.Document(-1));

            Assert.Throws<System.IndexOutOfRangeException>(() => Reader.GetTermVectors(-1));

            Assert.Throws<System.IndexOutOfRangeException>(() => Reader.Document(numDocs));

            Assert.Throws<System.IndexOutOfRangeException>(() => Reader.GetTermVectors(numDocs));
        }
    }
}