﻿using System.IO;
using NUnit.Framework;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Gl
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
     [TestFixture]
    public class TestGalicianAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// This test fails with NPE when the 
        /// stopwords file is missing in classpath 
        /// </summary>
        public virtual void TestResourcesAvailable()
        {
            new GalicianAnalyzer(TEST_VERSION_CURRENT);
        }

        /// <summary>
        /// test stopwords and stemming </summary>
        [Test]
        public virtual void TestBasics()
        {
            Analyzer a = new GalicianAnalyzer(TEST_VERSION_CURRENT);
            // stemming
            CheckOneTerm(a, "correspondente", "correspond");
            CheckOneTerm(a, "corresponderá", "correspond");
            // stopword
            AssertAnalyzesTo(a, "e", new string[] { });
        }

        /// <summary>
        /// test use of exclusion set </summary>
        [Test]
        public virtual void TestExclude()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("correspondente"), false);
            Analyzer a = new GalicianAnalyzer(TEST_VERSION_CURRENT, GalicianAnalyzer.DefaultStopSet, exclusionSet);
            CheckOneTerm(a, "correspondente", "correspondente");
            CheckOneTerm(a, "corresponderá", "correspond");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random(), new GalicianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
        }
    }
}