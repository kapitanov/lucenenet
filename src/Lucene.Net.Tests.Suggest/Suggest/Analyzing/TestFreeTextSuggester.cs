﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

//@SuppressCodecs("Lucene3x")
namespace Lucene.Net.Search.Suggest.Analyzing
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

    public class TestFreeTextSuggester : LuceneTestCase
    {
        [Test]
        public void TestBasic()
        {
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo bar baz blah", 50),
                new Input("boo foo bar foo bee", 20)
            );

            Analyzer a = new MockAnalyzer(Random());
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 2, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            assertEquals(2, sug.Count);

            for (int i = 0; i < 2; i++)
            {

                // Uses bigram model and unigram backoff:
                assertEquals("foo bar/0.67 foo bee/0.33 baz/0.04 blah/0.04 boo/0.04",
                             ToString(sug.DoLookup("foo b", 10)));

                // Uses only bigram model:
                assertEquals("foo bar/0.67 foo bee/0.33",
                             ToString(sug.DoLookup("foo ", 10)));

                // Uses only unigram model:
                assertEquals("foo/0.33",
                             ToString(sug.DoLookup("foo", 10)));

                // Uses only unigram model:
                assertEquals("bar/0.22 baz/0.11 bee/0.11 blah/0.11 boo/0.11",
                             ToString(sug.DoLookup("b", 10)));

                // Try again after save/load:
                DirectoryInfo tmpDir = CreateTempDir("FreeTextSuggesterTest");
                tmpDir.Create();

                FileInfo path = new FileInfo(Path.Combine(tmpDir.FullName, "suggester"));

                Stream os = new FileStream(path.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                sug.Store(os);
                os.Dispose();

                Stream @is = new FileStream(path.FullName, FileMode.Open);
                sug = new FreeTextSuggester(a, a, 2, (byte)0x20);
                sug.Load(@is);
                @is.Dispose();
                assertEquals(2, sug.Count);
            }
        }

        [Test]
        public void TestIllegalByteDuringBuild()
        {
            // Default separator is INFORMATION SEPARATOR TWO
            // (0x1e), so no input token is allowed to contain it
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo\u001ebar baz", 50)
            );
            FreeTextSuggester sug = new FreeTextSuggester(new MockAnalyzer(Random()));
            try
            {
                sug.Build(new InputArrayIterator(keys));
                fail("did not hit expected exception");
            }
            catch (ArgumentException /*iae*/)
            {
                // expected
            }
        }

        [Test]
        public void TestIllegalByteDuringQuery()
        {
            // Default separator is INFORMATION SEPARATOR TWO
            // (0x1e), so no input token is allowed to contain it
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo bar baz", 50)
            );
            FreeTextSuggester sug = new FreeTextSuggester(new MockAnalyzer(Random()));
            sug.Build(new InputArrayIterator(keys));

            try
            {
                sug.DoLookup("foo\u001eb", 10);
                fail("did not hit expected exception");
            }
            catch (ArgumentException /*iae*/)
            {
                // expected
            }
        }

        internal class TestWikiInputIterator : IInputIterator
        {
            private readonly LineFileDocs lfd;
            private readonly TestFreeTextSuggester outerInstance;
            private int count;

            public TestWikiInputIterator(TestFreeTextSuggester outerInstance, LineFileDocs lfd)
            {
                this.outerInstance = outerInstance;
                this.lfd = lfd;
            }

            public long Weight
            {
                get
                {
                    return 1;
                }
            }

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public BytesRef Next()
            {
                Document doc;
                try
                {
                    doc = lfd.NextDoc();
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.Message, ioe);
                }
                if (doc == null)
                {
                    return null;
                }
                if (count++ == 10000)
                {
                    return null;
                }
                return new BytesRef(doc.Get("body"));
            }

            public BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public bool HasPayloads
            {
                get
                {
                    return false;
                }
            }

            public IEnumerable<BytesRef> Contexts
            {
                get
                {
                    return null;
                }
            }

            public bool HasContexts
            {
                get
                {
                    return false;
                }
            }
        }

        [Ignore("Ignore reason required.")]
        public void TestWiki()
        {
            LineFileDocs lfd = new LineFileDocs(null, "/lucenedata/enwiki/enwiki-20120502-lines-1k.txt", false);
            // Skip header:
            lfd.NextDoc();
            FreeTextSuggester sug = new FreeTextSuggester(new MockAnalyzer(Random()));
            sug.Build(new TestWikiInputIterator(this, lfd));
            if (VERBOSE)
            {
                Console.WriteLine(sug.GetSizeInBytes() + " bytes");

                IList<Lookup.LookupResult> results = sug.DoLookup("general r", 10);
                Console.WriteLine("results:");
                foreach (Lookup.LookupResult result in results)
                {
                    Console.WriteLine("  " + result);
                }
            }
        }

        // Make sure you can suggest based only on unigram model:
        [Test]
        public void TestUnigrams()
        {
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo bar baz blah boo foo bar foo bee", 50)
            );

            Analyzer a = new MockAnalyzer(Random());
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 1, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            // Sorts first by count, descending, second by term, ascending
            assertEquals("bar/0.22 baz/0.11 bee/0.11 blah/0.11 boo/0.11",
                         ToString(sug.DoLookup("b", 10)));
        }

        // Make sure the last token is not duplicated
        [Test]
        public void TestNoDupsAcrossGrams()
        {
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo bar bar bar bar", 50)
            );
            Analyzer a = new MockAnalyzer(Random());
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 2, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            assertEquals("foo bar/1.00",
                         ToString(sug.DoLookup("foo b", 10)));
        }

        // Lookup of just empty string produces unicode only matches:
        [Test]
        public void TestEmptyString()
        {
            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("foo bar bar bar bar", 50)
            );
            Analyzer a = new MockAnalyzer(Random());
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 2, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            try
            {
                sug.DoLookup("", 10);
                fail("did not hit exception");
            }
            catch (ArgumentException /*iae*/)
            {
                // expected
            }
        }

        internal class TestEndingHoleAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                CharArraySet stopSet = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "of");
                return new TokenStreamComponents(tokenizer, new StopFilter(TEST_VERSION_CURRENT, tokenizer, stopSet));
            }
        }

        // With one ending hole, ShingleFilter produces "of _" and
        // we should properly predict from that:
        [Test]
        public void TestEndingHole()
        {
            // Just deletes "of"
            Analyzer a = new TestEndingHoleAnalyzer();

            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("wizard of oz", 50)
            );
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 3, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            assertEquals("wizard _ oz/1.00",
                         ToString(sug.DoLookup("wizard of", 10)));

            // Falls back to unigram model, with backoff 0.4 times
            // prop 0.5:
            assertEquals("oz/0.20",
                         ToString(sug.DoLookup("wizard o", 10)));
        }



        // If the number of ending holes exceeds the ngrams window
        // then there are no predictions, because ShingleFilter
        // does not produce e.g. a hole only "_ _" token:
        [Test]
        public void TestTwoEndingHoles()
        {
            // Just deletes "of"
            Analyzer a = new TestEndingHoleAnalyzer();

            IEnumerable<Input> keys = AnalyzingSuggesterTest.Shuffle(
                new Input("wizard of of oz", 50)
            );
            FreeTextSuggester sug = new FreeTextSuggester(a, a, 3, (byte)0x20);
            sug.Build(new InputArrayIterator(keys));
            assertEquals("",
                         ToString(sug.DoLookup("wizard of of", 10)));
        }

        internal class ByScoreThenKeyComparator : IComparer<Lookup.LookupResult>
        {
            public int Compare(Lookup.LookupResult a, Lookup.LookupResult b)
            {
                if (a.value > b.value)
                {
                    return -1;
                }
                else if (a.value < b.value)
                {
                    return 1;
                }
                else
                {
                    // Tie break by UTF16 sort order:
                    return ((string)a.key).CompareToOrdinal((string)b.key);
                }
            }
        }

        private static IComparer<Lookup.LookupResult> byScoreThenKey = new ByScoreThenKeyComparator();

        internal class TestRandomInputIterator : IInputIterator
        {
            internal int upto;
            private readonly TestFreeTextSuggester outerInstance;
            private readonly string[][] docs;

            public TestRandomInputIterator(TestFreeTextSuggester outerInstance, string[][] docs)
            {
                this.outerInstance = outerInstance;
                this.docs = docs;
            }

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public BytesRef Next()
            {
                if (upto == docs.Length)
                {
                    return null;
                }
                else
                {
                    StringBuilder b = new StringBuilder();
                    foreach (string token in docs[upto])
                    {
                        b.append(' ');
                        b.append(token);
                    }
                    upto++;
                    return new BytesRef(b.toString());
                }
            }

            public long Weight
            {
                get
                {
                    return Random().Next();
                }
            }

            public BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public bool HasPayloads
            {
                get
                {
                    return false;
                }
            }


            public IEnumerable<BytesRef> Contexts
            {
                get
                {
                    return null;
                }
            }

            public bool HasContexts
            {
                get
                {
                    return false;
                }
            }
        }

        [Test]
        public void TestRandom()
        {
            string[] terms = new string[TestUtil.NextInt(Random(), 2, 10)];
            ISet<string> seen = new HashSet<string>();
            while (seen.size() < terms.Length)
            {
                string token = TestUtil.RandomSimpleString(Random(), 1, 5);
                if (!seen.contains(token))
                {
                    terms[seen.size()] = token;
                    seen.add(token);
                }
            }

            Analyzer a = new MockAnalyzer(Random());

            int numDocs = AtLeast(10);
            long totTokens = 0;
            string[][] docs = new string[numDocs][];
            for (int i = 0; i < numDocs; i++)
            {
                docs[i] = new string[AtLeast(100)];
                if (VERBOSE)
                {
                    Console.Write("  doc " + i + ":");
                }
                for (int j = 0; j < docs[i].Length; j++)
                {
                    docs[i][j] = GetZipfToken(terms);
                    if (VERBOSE)
                    {
                        Console.Write(" " + docs[i][j]);
                    }
                }
                if (VERBOSE)
                {
                    Console.WriteLine();
                }
                totTokens += docs[i].Length;
            }

            int grams = TestUtil.NextInt(Random(), 1, 4);

            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + terms.Length + " terms; " + numDocs + " docs; " + grams + " grams");
            }

            // Build suggester model:
            FreeTextSuggester sug = new FreeTextSuggester(a, a, grams, (byte)0x20);
            sug.Build(new TestRandomInputIterator(this, docs));

            // Build inefficient but hopefully correct model:
            List<IDictionary<string, int?>> gramCounts = new List<IDictionary<string, int?>>(grams);
            for (int gram = 0; gram < grams; gram++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: build model for gram=" + gram);
                }
                IDictionary<string, int?> model = new HashMap<string, int?>();
                gramCounts.Add(model);
                foreach (string[] doc in docs)
                {
                    for (int i = 0; i < doc.Length - gram; i++)
                    {
                        StringBuilder b = new StringBuilder();
                        for (int j = i; j <= i + gram; j++)
                        {
                            if (j > i)
                            {
                                b.append(' ');
                            }
                            b.append(doc[j]);
                        }
                        string token = b.toString();
                        int? curCount = model.ContainsKey(token) ? model[token] : null;
                        if (curCount == null)
                        {
                            model.Put(token, 1);
                        }
                        else
                        {
                            model.Put(token, 1 + curCount);
                        }
                        if (VERBOSE)
                        {
                            Console.WriteLine("  add '" + token + "' -> count=" + (model.ContainsKey(token) ? model[token].ToString() : ""));
                        }
                    }
                }
            }

            int lookups = AtLeast(100);
            for (int iter = 0; iter < lookups; iter++)
            {
                string[] tokens = new string[TestUtil.NextInt(Random(), 1, 5)];
                for (int i = 0; i < tokens.Length; i++)
                {
                    tokens[i] = GetZipfToken(terms);
                }

                // Maybe trim last token; be sure not to create the
                // empty string:
                int trimStart;
                if (tokens.Length == 1)
                {
                    trimStart = 1;
                }
                else
                {
                    trimStart = 0;
                }
                int trimAt = TestUtil.NextInt(Random(), trimStart, tokens[tokens.Length - 1].Length);
                tokens[tokens.Length - 1] = tokens[tokens.Length - 1].Substring(0, trimAt - 0);

                int num = TestUtil.NextInt(Random(), 1, 100);
                StringBuilder b = new StringBuilder();
                foreach (string token in tokens)
                {
                    b.append(' ');
                    b.append(token);
                }
                string query = b.toString();
                query = query.Substring(1);

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " query='" + query + "' num=" + num);
                }

                // Expected:
                List<Lookup.LookupResult> expected = new List<Lookup.LookupResult>();
                double backoff = 1.0;
                seen = new HashSet<string>();

                if (VERBOSE)
                {
                    Console.WriteLine("  compute expected");
                }
                for (int i = grams - 1; i >= 0; i--)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("    grams=" + i);
                    }

                    if (tokens.Length < i + 1)
                    {
                        // Don't have enough tokens to use this model
                        if (VERBOSE)
                        {
                            Console.WriteLine("      skip");
                        }
                        continue;
                    }

                    if (i == 0 && tokens[tokens.Length - 1].Length == 0)
                    {
                        // Never suggest unigrams from empty string:
                        if (VERBOSE)
                        {
                            Console.WriteLine("      skip unigram priors only");
                        }
                        continue;
                    }

                    // Build up "context" ngram:
                    b = new StringBuilder();
                    for (int j = tokens.Length - i - 1; j < tokens.Length - 1; j++)
                    {
                        b.append(' ');
                        b.append(tokens[j]);
                    }
                    string context = b.toString();
                    if (context.Length > 0)
                    {
                        context = context.Substring(1);
                    }
                    if (VERBOSE)
                    {
                        Console.WriteLine("      context='" + context + "'");
                    }
                    long contextCount;
                    if (context.Length == 0)
                    {
                        contextCount = totTokens;
                    }
                    else
                    {
                        //int? count = gramCounts.get(i - 1).get(context);
                        var gramCount = gramCounts[i - 1];
                        int? count = gramCount.ContainsKey(context) ? gramCount[context] : null;
                        if (count == null)
                        {
                            // We never saw this context:
                            backoff *= FreeTextSuggester.ALPHA;
                            if (VERBOSE)
                            {
                                Console.WriteLine("      skip: never saw context");
                            }
                            continue;
                        }
                        contextCount = count.GetValueOrDefault();
                    }
                    if (VERBOSE)
                    {
                        Console.WriteLine("      contextCount=" + contextCount);
                    }
                    IDictionary<string, int?> model = gramCounts[i];

                    // First pass, gather all predictions for this model:
                    if (VERBOSE)
                    {
                        Console.WriteLine("      find terms w/ prefix=" + tokens[tokens.Length - 1]);
                    }
                    List<Lookup.LookupResult> tmp = new List<Lookup.LookupResult>();
                    foreach (string term in terms)
                    {
                        if (term.StartsWith(tokens[tokens.Length - 1]))
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("        term=" + term);
                            }
                            if (seen.contains(term))
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("          skip seen");
                                }
                                continue;
                            }
                            string ngram = (context + " " + term).Trim();
                            //Integer count = model.get(ngram);
                            int? count = model.ContainsKey(ngram) ? model[ngram] : null;
                            if (count != null)
                            {
                                // LUCENENET NOTE: We need to calculate this as decimal because when using double it can sometimes 
                                // return numbers that are greater than long.MaxValue, which results in a negative long number.
                                // This is also the way it is being done in the FreeTextSuggester to work around the issue.
                                Lookup.LookupResult lr = new Lookup.LookupResult(ngram, (long)(long.MaxValue * ((decimal)backoff * (decimal)count / contextCount)));
                                tmp.Add(lr);
                                if (VERBOSE)
                                {
                                    Console.WriteLine("      add tmp key='" + lr.key + "' score=" + lr.value);
                                }
                            }
                        }
                    }

                    // Second pass, trim to only top N, and fold those
                    // into overall suggestions:
                    tmp.Sort(byScoreThenKey);
                    if (tmp.size() > num)
                    {
                        //tmp.subList(num, tmp.size()).clear();
                        tmp.RemoveRange(num, tmp.size() - num);
                    }
                    foreach (Lookup.LookupResult result in tmp)
                    {
                        string key = result.key.toString();
                        int idx = key.LastIndexOf(' ');
                        string lastToken;
                        if (idx != -1)
                        {
                            lastToken = key.Substring(idx + 1);
                        }
                        else
                        {
                            lastToken = key;
                        }
                        if (!seen.contains(lastToken))
                        {
                            seen.add(lastToken);
                            expected.Add(result);
                            if (VERBOSE)
                            {
                                Console.WriteLine("      keep key='" + result.key + "' score=" + result.value);
                            }
                        }
                    }

                    backoff *= FreeTextSuggester.ALPHA;
                }

                expected.Sort(byScoreThenKey);

                if (expected.size() > num)
                {
                    expected.RemoveRange(num, expected.size() - num);
                }

                // Actual:
                IList<Lookup.LookupResult> actual = sug.DoLookup(query, num);

                if (VERBOSE)
                {
                    Console.WriteLine("  expected: " + expected);
                    Console.WriteLine("    actual: " + actual);
                }

                assertEquals(expected.ToString(), actual.ToString());
            }
        }

        private static string GetZipfToken(string[] tokens)
        {
            // Zipf-like distribution:
            for (int k = 0; k < tokens.Length; k++)
            {
                if (Random().nextBoolean() || k == tokens.Length - 1)
                {
                    return tokens[k];
                }
            }
            Debug.Assert(false);
            return null;
        }

        private static string ToString(IEnumerable<Lookup.LookupResult> results)
        {
            StringBuilder b = new StringBuilder();
            foreach (Lookup.LookupResult result in results)
            {
                b.Append(' ');
                b.Append(result.key);
                b.Append('/');
                b.AppendFormat(CultureInfo.InvariantCulture, "{0:0.00}", ((double)result.value) / long.MaxValue);
            }
            return b.toString().Trim();
        }
    }
}
