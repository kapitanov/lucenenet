using Xunit;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System;
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
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [Trait("Category", "Ignored")]
    public class Test2BDocs : LuceneTestCase, IClassFixture<Test2BDocsFixture>
    {
        private readonly Test2BDocsFixture _testFixture;

        public Test2BDocs(Test2BDocsFixture fixture)
            : base()
        {
            _testFixture = fixture;
        }

        [Fact]
        public virtual void TestOverflow()
        {
            DirectoryReader ir = DirectoryReader.Open(_testFixture.Dir);
            IndexReader[] subReaders = new IndexReader[8192];
            Arrays.Fill(subReaders, ir);
            Assert.Throws<System.ArgumentException>(() =>
            {
                new MultiReader(subReaders);
            });
            ir.Dispose();
        }

        [Fact]
        public virtual void TestExactlyAtLimit()
        {
            Directory dir2 = NewFSDirectory(CreateTempDir("2BDocs2"));
            IndexWriter iw = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            for (int i = 0; i < 262143; i++)
            {
                iw.AddDocument(doc);
            }
            iw.Dispose();
            DirectoryReader ir = DirectoryReader.Open(_testFixture.Dir);
            DirectoryReader ir2 = DirectoryReader.Open(dir2);
            IndexReader[] subReaders = new IndexReader[8192];
            Arrays.Fill(subReaders, ir);
            subReaders[subReaders.Length - 1] = ir2;
            MultiReader mr = new MultiReader(subReaders);
            Assert.Equal(int.MaxValue, mr.MaxDoc);
            Assert.Equal(int.MaxValue, mr.NumDocs);
            ir.Dispose();
            ir2.Dispose();
            dir2.Dispose();
        }
    }

    public class Test2BDocsFixture : IDisposable
    {
        internal Directory Dir { get; private set; }

        public Test2BDocsFixture()
        {
            var tempDirectory = LuceneTestCase.CreateTempDir("2Bdocs");
            Dir = LuceneTestCase.NewFSDirectory(tempDirectory);
            IndexWriter iw = new IndexWriter(Dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            for (int i = 0; i < 262144; i++)
            {
                iw.AddDocument(doc);
            }
            iw.ForceMerge(1);
            iw.Dispose();
        }

        public void Dispose()
        {
            Dir.Dispose();
            Dir = null;
        }
    }
}