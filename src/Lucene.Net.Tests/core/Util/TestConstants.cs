using System;
using System.Text.RegularExpressions;
using Lucene.Net.Support;
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

    public class TestConstants : LuceneTestCase
    {
        private string VersionDetails
        {
            get
            {
                return " (LUCENE_MAIN_VERSION=" + Constants.LUCENE_MAIN_VERSION + ", LUCENE_MAIN_VERSION(without alpha/beta)=" + Constants.MainVersionWithoutAlphaBeta() + ", LUCENE_VERSION=" + Constants.LUCENE_VERSION + ")";
            }
        }

        [Fact]
        public virtual void TestLuceneMainVersionConstant()
        {
            Assert.True(Regex.IsMatch(Constants.LUCENE_MAIN_VERSION, "\\d+\\.\\d+(|\\.0\\.\\d+)", RegexOptions.IgnoreCase), "LUCENE_MAIN_VERSION does not follow pattern: 'x.y' (stable release) or 'x.y.0.z' (alpha/beta version)" + VersionDetails);
            Assert.True(Constants.LUCENE_VERSION.StartsWith(Constants.MainVersionWithoutAlphaBeta()), "LUCENE_VERSION does not start with LUCENE_MAIN_VERSION (without alpha/beta marker)" + VersionDetails);
        }

        [Fact]
        public virtual void TestBuildSetup()
        {
            // common-build.xml sets lucene.version, if not, we skip this test!
            string version = AppSettings.Get("lucene.version", null);
            if (version == null)
            {
                Console.WriteLine("Null lucene.version test property. You should run the tests with the official Lucene build file");
                return;
            }

            // remove anything after a "-" from the version string:
            version = Regex.Replace(version, "-.*$", "");
            string versionConstant = Regex.Replace(Constants.LUCENE_VERSION, "-.*$", "");
            Assert.True(versionConstant.StartsWith(version) || version.StartsWith(versionConstant), "LUCENE_VERSION should share the same prefix with lucene.version test property ('" + version + "')." + VersionDetails);
        }
    }
}