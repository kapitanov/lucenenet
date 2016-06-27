using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
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
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestSegmentTermDocs : LuceneTestCase
    {
        private Document TestDoc;
        private Directory Dir;
        private SegmentCommitInfo Info;

        public TestSegmentTermDocs() : base()
        {
            TestDoc = new Document();
            Dir = NewDirectory();
            DocHelper.SetupDoc(TestDoc);
            Info = DocHelper.WriteDoc(Random(), Dir, TestDoc);
        }

        public override void Dispose()
        {
            Dir.Dispose();
            base.Dispose();
        }

        [Fact]
        public virtual void Test()
        {
            Assert.True(Dir != null);
        }

        [Fact]
        public virtual void TestTermDocs()
        {
            TestTermDocs(1);
        }

        public virtual void TestTermDocs(int indexDivisor)
        {
            //After adding the document, we should be able to read it back in
            SegmentReader reader = new SegmentReader(Info, indexDivisor, NewIOContext(Random()));
            Assert.True(reader != null);
            Assert.Equal(indexDivisor, reader.TermInfosIndexDivisor);

            TermsEnum terms = reader.Fields.Terms(DocHelper.TEXT_FIELD_2_KEY).Iterator(null);
            terms.SeekCeil(new BytesRef("field"));
            DocsEnum termDocs = TestUtil.Docs(Random(), terms, reader.LiveDocs, null, DocsEnum.FLAG_FREQS);
            if (termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                int docId = termDocs.DocID();
                Assert.True(docId == 0);
                int freq = termDocs.Freq();
                Assert.True(freq == 3);
            }
            reader.Dispose();
        }

        [Fact]
        public virtual void TestBadSeek()
        {
            TestBadSeek(1);
        }

        public virtual void TestBadSeek(int indexDivisor)
        {
            {
                //After adding the document, we should be able to read it back in
                SegmentReader reader = new SegmentReader(Info, indexDivisor, NewIOContext(Random()));
                Assert.True(reader != null);
                DocsEnum termDocs = TestUtil.Docs(Random(), reader, "textField2", new BytesRef("bad"), reader.LiveDocs, null, 0);

                Assert.Null(termDocs);
                reader.Dispose();
            }
            {
                //After adding the document, we should be able to read it back in
                SegmentReader reader = new SegmentReader(Info, indexDivisor, NewIOContext(Random()));
                Assert.True(reader != null);
                DocsEnum termDocs = TestUtil.Docs(Random(), reader, "junk", new BytesRef("bad"), reader.LiveDocs, null, 0);
                Assert.Null(termDocs);
                reader.Dispose();
            }
        }

        [Fact]
        public virtual void TestSkipTo()
        {
            TestSkipTo(1);
        }

        public virtual void TestSkipTo(int indexDivisor)
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));

            Term ta = new Term("content", "aaa");
            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer, "aaa aaa aaa aaa");
            }

            Term tb = new Term("content", "bbb");
            for (int i = 0; i < 16; i++)
            {
                AddDoc(writer, "bbb bbb bbb bbb");
            }

            Term tc = new Term("content", "ccc");
            for (int i = 0; i < 50; i++)
            {
                AddDoc(writer, "ccc ccc ccc ccc");
            }

            // assure that we deal with a single segment
            writer.ForceMerge(1);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir, indexDivisor);

            DocsEnum tdocs = TestUtil.Docs(Random(), reader, ta.Field, new BytesRef(ta.Text()), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

            // without optimization (assumption skipInterval == 16)

            // with next
            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(0, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(1, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.Advance(2) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(2, tdocs.DocID());
            Assert.True(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(4, tdocs.DocID());
            Assert.True(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(9, tdocs.DocID());
            Assert.False(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS);

            // without next
            tdocs = TestUtil.Docs(Random(), reader, ta.Field, new BytesRef(ta.Text()), MultiFields.GetLiveDocs(reader), null, 0);

            Assert.True(tdocs.Advance(0) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(0, tdocs.DocID());
            Assert.True(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(4, tdocs.DocID());
            Assert.True(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(9, tdocs.DocID());
            Assert.False(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS);

            // exactly skipInterval documents and therefore with optimization

            // with next
            tdocs = TestUtil.Docs(Random(), reader, tb.Field, new BytesRef(tb.Text()), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(10, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(11, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.Advance(12) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(12, tdocs.DocID());
            Assert.True(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(15, tdocs.DocID());
            Assert.True(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(24, tdocs.DocID());
            Assert.True(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(25, tdocs.DocID());
            Assert.False(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS);

            // without next
            tdocs = TestUtil.Docs(Random(), reader, tb.Field, new BytesRef(tb.Text()), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

            Assert.True(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(10, tdocs.DocID());
            Assert.True(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(15, tdocs.DocID());
            Assert.True(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(24, tdocs.DocID());
            Assert.True(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(25, tdocs.DocID());
            Assert.False(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS);

            // much more than skipInterval documents and therefore with optimization

            // with next
            tdocs = TestUtil.Docs(Random(), reader, tc.Field, new BytesRef(tc.Text()), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(26, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(27, tdocs.DocID());
            Assert.Equal(4, tdocs.Freq());
            Assert.True(tdocs.Advance(28) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(28, tdocs.DocID());
            Assert.True(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(40, tdocs.DocID());
            Assert.True(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(57, tdocs.DocID());
            Assert.True(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(74, tdocs.DocID());
            Assert.True(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(75, tdocs.DocID());
            Assert.False(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS);

            //without next
            tdocs = TestUtil.Docs(Random(), reader, tc.Field, new BytesRef(tc.Text()), MultiFields.GetLiveDocs(reader), null, 0);
            Assert.True(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(26, tdocs.DocID());
            Assert.True(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(40, tdocs.DocID());
            Assert.True(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(57, tdocs.DocID());
            Assert.True(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(74, tdocs.DocID());
            Assert.True(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.Equal(75, tdocs.DocID());
            Assert.False(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS);

            reader.Dispose();
            dir.Dispose();
        }

        [Fact]
        public virtual void TestIndexDivisor()
        {
            TestDoc = new Document();
            DocHelper.SetupDoc(TestDoc);
            DocHelper.WriteDoc(Random(), Dir, TestDoc);
            TestTermDocs(2);
            TestBadSeek(2);
            TestSkipTo(2);
        }

        private void AddDoc(IndexWriter writer, string value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", value, Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}