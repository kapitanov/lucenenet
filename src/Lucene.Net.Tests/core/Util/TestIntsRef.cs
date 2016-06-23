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

    public class TestIntsRef : LuceneTestCase
    {
        [Fact]
        public virtual void TestEmpty()
        {
            IntsRef i = new IntsRef();
            Assert.Equal(IntsRef.EMPTY_INTS, i.Ints);
            Assert.Equal(0, i.Offset);
            Assert.Equal(0, i.Length);
        }

        [Fact]
        public virtual void TestFromInts()
        {
            int[] ints = new int[] { 1, 2, 3, 4 };
            IntsRef i = new IntsRef(ints, 0, 4);
            Assert.Equal(ints, i.Ints);
            Assert.Equal(0, i.Offset);
            Assert.Equal(4, i.Length);

            IntsRef i2 = new IntsRef(ints, 1, 3);
            Assert.Equal(new IntsRef(new int[] { 2, 3, 4 }, 0, 3), i2);

            Assert.False(i.Equals(i2));
        }
    }
}