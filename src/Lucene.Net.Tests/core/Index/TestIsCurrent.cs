using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Index
{
    using Lucene.Net.Store;
    using Lucene.Net.Util;
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

    public class TestIsCurrent : LuceneTestCase
    {
        private RandomIndexWriter Writer;

        private Directory Directory;

        public TestIsCurrent() : base()
        {
            // initialize directory
            Directory = NewDirectory();
            Writer = new RandomIndexWriter(Random(), Directory);

            // write document
            Document doc = new Document();
            doc.Add(NewTextField("UUID", "1", Field.Store.YES));
            Writer.AddDocument(doc);
            Writer.Commit();
        }

        public override void Dispose()
        {
            base.Dispose();
            Writer.Dispose();
            Directory.Dispose();
        }

        /// <summary>
        /// Failing testcase showing the trouble
        /// </summary>
        [Fact]
        public virtual void TestDeleteByTermIsCurrent()
        {
            // get reader
            DirectoryReader reader = Writer.Reader;

            // assert index has a document and reader is up2date
            Assert.Equal(1, Writer.NumDocs()); //, "One document should be in the index");
            Assert.True(reader.Current, "One document added, reader should be current");

            // remove document
            Term idTerm = new Term("UUID", "1");
            Writer.DeleteDocuments(idTerm);
            Writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.Equal(0, Writer.NumDocs()); //, "Document should be removed");
            Assert.False(reader.Current, "Reader should be stale");

            reader.Dispose();
        }

        /// <summary>
        /// Testcase for example to show that writer.deleteAll() is working as expected
        /// </summary>
        [Fact]
        public virtual void TestDeleteAllIsCurrent()
        {
            // get reader
            DirectoryReader reader = Writer.Reader;

            // assert index has a document and reader is up2date
            Assert.Equal(1, Writer.NumDocs()); //, "One document should be in the index");
            Assert.True(reader.Current, "Document added, reader should be stale ");

            // remove all documents
            Writer.DeleteAll();
            Writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.Equal(0, Writer.NumDocs()); //, "Document should be removed");
            Assert.False(reader.Current, "Reader should be stale");

            reader.Dispose();
        }
    }
}