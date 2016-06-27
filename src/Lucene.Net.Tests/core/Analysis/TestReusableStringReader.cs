using System.Text;
using Xunit;

namespace Lucene.Net.Analysis
{
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

    public class TestReusableStringReader : LuceneTestCase
    {
        [Fact]
        public virtual void Test()
        {
            ReusableStringReader reader = new ReusableStringReader();
            Assert.Equal(-1, reader.Read());
            Assert.Equal(-1, reader.Read(new char[1], 0, 1));
            Assert.Equal(-1, reader.Read(new char[2], 1, 1));
            //Assert.Equal(-1, reader.Read(CharBuffer.wrap(new char[2])));

            reader.Value = "foobar";
            char[] buf = new char[4];
            Assert.Equal(4, reader.Read(buf, 0, 4));
            Assert.Equal("foob", new string(buf));
            Assert.Equal(2, reader.Read(buf, 0, 2));
            Assert.Equal("ar", new string(buf, 0, 2));
            Assert.Equal(-1, reader.Read(buf, 2, 0));
            reader.Close();

            reader.Value = "foobar";
            Assert.Equal(0, reader.Read(buf, 1, 0));
            Assert.Equal(3, reader.Read(buf, 1, 3));
            Assert.Equal("foo", new string(buf, 1, 3));
            Assert.Equal(2, reader.Read(buf, 2, 2));
            Assert.Equal("ba", new string(buf, 2, 2));
            Assert.Equal('r', (char)reader.Read());
            Assert.Equal(-1, reader.Read(buf, 2, 0));
            reader.Close();

            reader.Value = "foobar";
            StringBuilder sb = new StringBuilder();
            int ch;
            while ((ch = reader.Read()) != -1)
            {
                sb.Append((char)ch);
            }
            reader.Close();
            Assert.Equal("foobar", sb.ToString());
        }
    }
}