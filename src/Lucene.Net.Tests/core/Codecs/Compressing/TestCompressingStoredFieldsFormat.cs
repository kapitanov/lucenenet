using Lucene.Net.Documents;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Codecs.Compressing
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System;
    using BaseStoredFieldsFormatTestCase = Lucene.Net.Index.BaseStoredFieldsFormatTestCase;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IntField = IntField;

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

    [TestFixture]
    public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
    {
        protected override Codec Codec
        {
            get
            {
                return CompressingCodec.RandomInstance(Random());
            }
        }

        [Test]
        public virtual void TestDeletePartiallyWrittenFilesIfAbort()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConf.SetMaxBufferedDocs(RandomInts.NextIntBetween(Random(), 2, 30));
            iwConf.SetCodec(CompressingCodec.RandomInstance(Random()));
            // disable CFS because this test checks file names
            iwConf.SetMergePolicy(NewLogMergePolicy(false));
            iwConf.SetUseCompoundFile(false);
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);

            Document validDoc = new Document();
            validDoc.Add(new IntField("id", 0, Field.Store.YES));
            iw.AddDocument(validDoc);
            iw.Commit();

            // make sure that #writeField will fail to trigger an abort
            Document invalidDoc = new Document();
            FieldType fieldType = new FieldType();
            fieldType.Stored = true;
            invalidDoc.Add(new FieldAnonymousInnerClassHelper(this, fieldType));

            Assert.Throws<ArgumentException>(() =>
            {
                try
                {
                    iw.AddDocument(invalidDoc);
                    iw.Commit();
                }
                finally
                {
                    int counter = 0;
                    foreach (string fileName in dir.ListAll())
                    {
                        if (fileName.EndsWith(".fdt") || fileName.EndsWith(".fdx"))
                        {
                            counter++;
                        }
                    }
                    // Only one .fdt and one .fdx files must have been found
                    Assert.AreEqual(2, counter);
                    iw.Dispose();
                    dir.Dispose();
                }
            });
        }

        private class FieldAnonymousInnerClassHelper : Field
        {
            private readonly TestCompressingStoredFieldsFormat OuterInstance;

            public FieldAnonymousInnerClassHelper(TestCompressingStoredFieldsFormat outerInstance, FieldType fieldType)
                : base("invalid", fieldType)
            {
                this.OuterInstance = outerInstance;
            }
        }


        #region BaseStoredFieldsFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestRandomStoredFields()
        {
            base.TestRandomStoredFields();
        }

        [Test]
        // LUCENE-1727: make sure doc fields are stored in order
        public override void TestStoredFieldsOrder()
        {
            base.TestStoredFieldsOrder();
        }

        [Test]
        // LUCENE-1219
        public override void TestBinaryFieldOffsetLength()
        {
            base.TestBinaryFieldOffsetLength();
        }

        [Test]
        public override void TestNumericField()
        {
            base.TestNumericField();
        }

        [Test]
        public override void TestIndexedBit()
        {
            base.TestIndexedBit();
        }

        [Test]
        public override void TestReadSkip()
        {
            base.TestReadSkip();
        }

        [Test]
        public override void TestEmptyDocs()
        {
            base.TestEmptyDocs();
        }

        [Test]
        public override void TestConcurrentReads()
        {
            base.TestConcurrentReads();
        }

        [Test]
        public override void TestWriteReadMerge()
        {
            base.TestWriteReadMerge();
        }

        [Test, Timeout(300000)]
        public override void TestBigDocuments()
        {
            base.TestBigDocuments();
        }

        [Test]
        public override void TestBulkMergeWithDeletes()
        {
            base.TestBulkMergeWithDeletes();
        }

        #endregion

        #region BaseIndexFileFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestMergeStability()
        {
            base.TestMergeStability();
        }

        #endregion
    }
}