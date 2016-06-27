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
using System;
using System.Linq;
using Xunit;

namespace Lucene.Net.Util
{
    public class TestVersion : LuceneTestCase
    {
        [Fact]
        public virtual void Test()
        {
            foreach (LuceneVersion v in Enum.GetValues(typeof(LuceneVersion)))
            {
                Assert.True(LuceneVersion.LUCENE_CURRENT.OnOrAfter(v), "LUCENE_CURRENT must be always onOrAfter(" + v + ")");
            }
            Assert.True(LuceneVersion.LUCENE_40.OnOrAfter(LuceneVersion.LUCENE_31));
            Assert.True(LuceneVersion.LUCENE_40.OnOrAfter(LuceneVersion.LUCENE_40));
            Assert.False(LuceneVersion.LUCENE_30.OnOrAfter(LuceneVersion.LUCENE_31));
        }

        [Fact]
        public virtual void TestParseLeniently()
        {
            Assert.Equal(LuceneVersion.LUCENE_40, LuceneVersionHelpers.ParseLeniently("4.0"));
            Assert.Equal(LuceneVersion.LUCENE_40, LuceneVersionHelpers.ParseLeniently("LUCENE_40"));
            Assert.Equal(LuceneVersion.LUCENE_CURRENT, LuceneVersionHelpers.ParseLeniently("LUCENE_CURRENT"));
        }

        [Fact]
        public virtual void TestDeprecations()
        {
            LuceneVersion[] values = Enum.GetValues(typeof(LuceneVersion)).Cast<LuceneVersion>().ToArray();
            // all but the latest version should be deprecated
            for (int i = 0; i < values.Length; i++)
            {
                if (i + 1 == values.Length)
                {
                    Assert.Equal(LuceneVersion.LUCENE_CURRENT, values[i]); //, "Last constant must be LUCENE_CURRENT");
                }
                /*bool dep = typeof(Version).GetField(values[i].Name()).isAnnotationPresent(typeof(Deprecated));
                if (i + 2 != values.Length)
                {
                  Assert.True(values[i].name() + " should be deprecated", dep);
                }
                else
                {
                  Assert.False(values[i].name() + " should not be deprecated", dep);
                }*/
            }
        }

        [Fact]
        public virtual void TestAgainstMainVersionConstant()
        {
            LuceneVersion[] values = Enum.GetValues(typeof(LuceneVersion)).Cast<LuceneVersion>().ToArray();
            Assert.True(values.Length >= 2);
            string mainVersionWithoutAlphaBeta = Constants.MainVersionWithoutAlphaBeta();
            LuceneVersion mainVersionParsed = LuceneVersionHelpers.ParseLeniently(mainVersionWithoutAlphaBeta);
            Assert.Equal(mainVersionParsed, values[values.Length - 2]); //, "Constant one before last must be the same as the parsed LUCENE_MAIN_VERSION (without alpha/beta) constant: " + mainVersionWithoutAlphaBeta);
        }
    }
}