using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Index
{
    using System.IO;
    using BaseDirectory = Lucene.Net.Store.BaseDirectory;
    using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DocumentStoredFieldVisitor = DocumentStoredFieldVisitor;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
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
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestFieldsReader : LuceneTestCase, IClassFixture<TestFieldsReaderFixture>
    {
        private readonly TestFieldsReaderFixture _fixture;

        public TestFieldsReader(TestFieldsReaderFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public virtual void Test()
        {
            Assert.True(_fixture.Dir != null);
            Assert.True(_fixture.FieldInfos != null);
            IndexReader reader = DirectoryReader.Open(_fixture.Dir);
            Document doc = reader.Document(0);
            Assert.True(doc != null);
            Assert.True(doc.GetField(DocHelper.TEXT_FIELD_1_KEY) != null);

            Field field = (Field)doc.GetField(DocHelper.TEXT_FIELD_2_KEY);
            Assert.True(field != null);
            Assert.True(field.FieldType().StoreTermVectors);

            Assert.False(field.FieldType().OmitNorms);
            Assert.True(field.FieldType().IndexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            field = (Field)doc.GetField(DocHelper.TEXT_FIELD_3_KEY);
            Assert.True(field != null);
            Assert.False(field.FieldType().StoreTermVectors);
            Assert.True(field.FieldType().OmitNorms);
            Assert.True(field.FieldType().IndexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            field = (Field)doc.GetField(DocHelper.NO_TF_KEY);
            Assert.True(field != null);
            Assert.False(field.FieldType().StoreTermVectors);
            Assert.False(field.FieldType().OmitNorms);
            Assert.True(field.FieldType().IndexOptions == FieldInfo.IndexOptions.DOCS_ONLY);

            DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(DocHelper.TEXT_FIELD_3_KEY);
            reader.Document(0, visitor);
            IList<IndexableField> fields = visitor.Document.Fields;
            Assert.Equal(1, fields.Count);
            Assert.Equal(DocHelper.TEXT_FIELD_3_KEY, fields[0].Name());
            reader.Dispose();
        }

        public class FaultyFSDirectory : BaseDirectory
        {
            internal Directory FsDir;

            public FaultyFSDirectory(DirectoryInfo dir)
            {
                FsDir = NewFSDirectory(dir);
                _lockFactory = FsDir.LockFactory;
            }

            public override IndexInput OpenInput(string name, IOContext context)
            {
                return new FaultyIndexInput(FsDir.OpenInput(name, context));
            }

            public override string[] ListAll()
            {
                return FsDir.ListAll();
            }

            public override bool FileExists(string name)
            {
                return FsDir.FileExists(name);
            }

            public override void DeleteFile(string name)
            {
                FsDir.DeleteFile(name);
            }

            public override long FileLength(string name)
            {
                return FsDir.FileLength(name);
            }

            public override IndexOutput CreateOutput(string name, IOContext context)
            {
                return FsDir.CreateOutput(name, context);
            }

            public override void Sync(ICollection<string> names)
            {
                FsDir.Sync(names);
            }

            public override void Dispose()
            {
                FsDir.Dispose();
            }
        }

        // LUCENE-1262
        [Fact]
        public virtual void TestExceptions()
        {
            DirectoryInfo indexDir = CreateTempDir("testfieldswriterexceptions");

            try
            {
                Directory dir = new FaultyFSDirectory(indexDir);
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE);
                IndexWriter writer = new IndexWriter(dir, iwc);
                for (int i = 0; i < 2; i++)
                {
                    writer.AddDocument(_fixture.TestDoc);
                }
                writer.ForceMerge(1);
                writer.Dispose();

                IndexReader reader = DirectoryReader.Open(dir);

                FaultyIndexInput.DoFail = true;

                bool exc = false;

                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        reader.Document(i);
                    }
                    catch (IOException ioe)
                    {
                        // expected
                        exc = true;
                    }
                    try
                    {
                        reader.Document(i);
                    }
                    catch (IOException ioe)
                    {
                        // expected
                        exc = true;
                    }
                }
                Assert.True(exc);
                reader.Dispose();
                dir.Dispose();
            }
            finally
            {
                System.IO.Directory.Delete(indexDir.FullName, true);
            }
        }
    }

    public class TestFieldsReaderFixture : IDisposable
    {
        internal Directory Dir { get; private set; }
        internal Document TestDoc { get; private set; }
        internal FieldInfos.Builder FieldInfos { get; private set; }

        public TestFieldsReaderFixture()
        {
            TestDoc = new Document();
            FieldInfos = new FieldInfos.Builder();
            DocHelper.SetupDoc(TestDoc);
            foreach (IndexableField field in TestDoc)
            {
                FieldInfos.AddOrUpdate(field.Name(), field.FieldType());
            }
            Dir = LuceneTestCase.NewDirectory();
            IndexWriterConfig conf = LuceneTestCase.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random())).SetMergePolicy(LuceneTestCase.NewLogMergePolicy());
            conf.MergePolicy.NoCFSRatio = 0.0;
            IndexWriter writer = new IndexWriter(Dir, conf);
            writer.AddDocument(TestDoc);
            writer.Dispose();
            FaultyIndexInput.DoFail = false;
        }

        public void Dispose()
        {
            Dir.Dispose();
            Dir = null;
            FieldInfos = null;
            TestDoc = null;
        }
    }

    internal class FaultyIndexInput : BufferedIndexInput
    {
        internal IndexInput @delegate;
        internal static bool DoFail;
        internal int Count;

        internal FaultyIndexInput(IndexInput @delegate)
            : base("FaultyIndexInput(" + @delegate + ")", BufferedIndexInput.BUFFER_SIZE)
        {
            this.@delegate = @delegate;
        }

        internal virtual void SimOutage()
        {
            if (DoFail && Count++ % 2 == 1)
            {
                throw new IOException("Simulated network outage");
            }
        }

        protected override void ReadInternal(byte[] b, int offset, int length)
        {
            SimOutage();
            @delegate.Seek(FilePointer);
            @delegate.ReadBytes(b, offset, length);
        }

        protected override void SeekInternal(long pos)
        {
        }

        public override long Length()
        {
            return @delegate.Length();
        }

        public override void Dispose()
        {
            @delegate.Dispose();
        }

        public override object Clone()
        {
            FaultyIndexInput i = new FaultyIndexInput((IndexInput)@delegate.Clone());
            // seek the clone to our current position
            try
            {
                i.Seek(FilePointer);
            }
            catch (IOException)
            {
                throw new Exception();
            }
            return i;
        }
    }
}