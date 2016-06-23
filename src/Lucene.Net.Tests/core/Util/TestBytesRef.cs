using Xunit;

namespace Lucene.Net.Util
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

    public class TestBytesRef : LuceneTestCase
    {
        [Fact]
        public virtual void TestEmpty()
        {
            BytesRef b = new BytesRef();
            Assert.Equal(BytesRef.EMPTY_BYTES, b.Bytes);
            Assert.Equal(0, b.Offset);
            Assert.Equal(0, b.Length);
        }

        [Fact]
        public virtual void TestFromBytes()
        {
            var bytes = new [] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes);
            Assert.Equal(bytes, b.Bytes);
            Assert.Equal(0, b.Offset);
            Assert.Equal(4, b.Length);

            BytesRef b2 = new BytesRef(bytes, 1, 3);
            Assert.Equal("bcd", b2.Utf8ToString());

            Assert.False(b.Equals(b2));
        }

        [Fact]
        public virtual void TestFromChars()
        {
            for (int i = 0; i < 100; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random());
                string s2 = (new BytesRef(s)).Utf8ToString();
                Assert.Equal(s, s2);
            }

            // only for 4.x
            Assert.Equal("\uFFFF", (new BytesRef("\uFFFF")).Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you append to a bytesref with offset != 0
        [Fact]
        public virtual void TestAppend()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.Append(new BytesRef("e"));
            Assert.Equal("bcde", b.Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a bytesref with offset != 0
        [Fact]
        public virtual void TestCopyBytes()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.CopyBytes(new BytesRef("bcde"));
            Assert.Equal("bcde", b.Utf8ToString());
        }
    }
}