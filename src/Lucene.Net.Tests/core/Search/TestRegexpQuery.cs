using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using Xunit;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using AutomatonProvider = Lucene.Net.Util.Automaton.AutomatonProvider;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
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
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Some simple regex tests, mostly converted from contrib's TestRegexQuery.
    /// </summary>
    public class TestRegexpQuery : LuceneTestCase
    {
        private IndexSearcher Searcher;
        private IndexReader Reader;
        private Directory Directory;
        private readonly string FN = "field";

        [SetUp]
        public override void SetUp()
        {
            
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory);
            Document doc = new Document();
            doc.Add(NewTextField(FN, "the quick brown fox jumps over the lazy ??? dog 493432 49344", Field.Store.NO));
            writer.AddDocument(doc);
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Directory.Dispose();
            base.Dispose();
        }

        private Term NewTerm(string value)
        {
            return new Term(FN, value);
        }

        private int RegexQueryNrHits(string regex)
        {
            RegexpQuery query = new RegexpQuery(NewTerm(regex));
            return Searcher.Search(query, 5).TotalHits;
        }

        [Fact]
        public virtual void TestRegex1()
        {
            Assert.Equal(1, RegexQueryNrHits("q.[aeiou]c.*"));
        }

        [Fact]
        public virtual void TestRegex2()
        {
            Assert.Equal(0, RegexQueryNrHits(".[aeiou]c.*"));
        }

        [Fact]
        public virtual void TestRegex3()
        {
            Assert.Equal(0, RegexQueryNrHits("q.[aeiou]c"));
        }

        [Fact]
        public virtual void TestNumericRange()
        {
            Assert.Equal(1, RegexQueryNrHits("<420000-600000>"));
            Assert.Equal(0, RegexQueryNrHits("<493433-600000>"));
        }

        [Fact]
        public virtual void TestRegexComplement()
        {
            Assert.Equal(1, RegexQueryNrHits("4934~[3]"));
            // not the empty lang, i.e. match all docs
            Assert.Equal(1, RegexQueryNrHits("~#"));
        }

        [Fact]
        public virtual void TestCustomProvider()
        {
            AutomatonProvider myProvider = new AutomatonProviderAnonymousInnerClassHelper(this);
            RegexpQuery query = new RegexpQuery(NewTerm("<quickBrown>"), RegExp.ALL, myProvider);
            Assert.Equal(1, Searcher.Search(query, 5).TotalHits);
        }

        private class AutomatonProviderAnonymousInnerClassHelper : AutomatonProvider
        {
            private readonly TestRegexpQuery OuterInstance;

            public AutomatonProviderAnonymousInnerClassHelper(TestRegexpQuery outerInstance)
            {
                this.OuterInstance = outerInstance;
                quickBrownAutomaton = BasicOperations.Union(Arrays.AsList(BasicAutomata.MakeString("quick"), BasicAutomata.MakeString("brown"), BasicAutomata.MakeString("bob")));
            }

            // automaton that matches quick or brown
            private Automaton quickBrownAutomaton;

            public Automaton GetAutomaton(string name)
            {
                if (name.Equals("quickBrown"))
                {
                    return quickBrownAutomaton;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Test a corner case for backtracking: In this case the term dictionary has
        /// 493432 followed by 49344. When backtracking from 49343... to 4934, its
        /// necessary to test that 4934 itself is ok before trying to append more
        /// characters.
        /// </summary>
        [Fact]
        public virtual void TestBacktracking()
        {
            Assert.Equal(1, RegexQueryNrHits("4934[314]"));
        }
    }
}