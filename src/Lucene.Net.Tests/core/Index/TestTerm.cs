using Xunit;

namespace Lucene.Net.Index
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    public class TestTerm : LuceneTestCase
    {
        [Fact]
        public virtual void TestEquals()
        {
            Term @base = new Term("same", "same");
            Term same = new Term("same", "same");
            Term differentField = new Term("different", "same");
            Term differentText = new Term("same", "different");
            const string differentType = "AString";
            Assert.Equal(@base, @base);
            Assert.Equal(@base, same);
            Assert.False(@base.Equals(differentField));
            Assert.False(@base.Equals(differentText));
            Assert.False(@base.Equals(differentType));
        }
    }
}