using System.Collections.Generic;
using Lucene.Net.Randomized.Generators;
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

    public class TestSentinelIntSet : LuceneTestCase
    {
        [Fact]
        public virtual void Test()
        {
            SentinelIntSet set = new SentinelIntSet(10, -1);
            Assert.False(set.Exists(50));
            set.Put(50);
            Assert.True(set.Exists(50));
            Assert.Equal(1, set.Size());
            Assert.Equal(-11, set.Find(10));
            Assert.Equal(1, set.Size());
            set.Clear();
            Assert.Equal(0, set.Size());
            Assert.Equal(50, set.Hash(50));
            //force a rehash
            for (int i = 0; i < 20; i++)
            {
                set.Put(i);
            }
            Assert.Equal(20, set.Size());
            Assert.Equal(24, set.RehashCount);
        }

        [Fact]
        public virtual void TestRandom()
        {
            for (int i = 0; i < 10000; i++)
            {
                int initSz = Random().Next(20);
                int num = Random().Next(30);
                int maxVal = (Random().NextBoolean() ? Random().Next(50) : Random().Next(int.MaxValue)) + 1;

                HashSet<int> a = new HashSet<int>(/*initSz*/);
                SentinelIntSet b = new SentinelIntSet(initSz, -1);

                for (int j = 0; j < num; j++)
                {
                    int val = Random().Next(maxVal);
                    bool exists = !a.Add(val);
                    bool existsB = b.Exists(val);
                    Assert.Equal(exists, existsB);
                    int slot = b.Find(val);
                    Assert.Equal(exists, slot >= 0);
                    b.Put(val);

                    Assert.Equal(a.Count, b.Size());
                }
            }
        }
    }
}