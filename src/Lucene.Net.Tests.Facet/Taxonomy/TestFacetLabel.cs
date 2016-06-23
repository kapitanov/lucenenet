using System;
using Lucene.Net.Support;
using Xunit;

namespace Lucene.Net.Facet.Taxonomy
{

    using SortedSetDocValuesFacetField = Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using TestUtil = Lucene.Net.Util.TestUtil;
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
    public class TestFacetLabel : FacetTestCase
    {

        [Fact]
        public virtual void TestBasic()
        {
            Assert.Equal(0, (new FacetLabel()).Length);
            Assert.Equal(1, (new FacetLabel("hello")).Length);
            Assert.Equal(2, (new FacetLabel("hello", "world")).Length);
        }

        [Fact]
        public virtual void TestToString()
        {
            // When the category is empty, we expect an empty string
            Assert.Equal("FacetLabel: []", (new FacetLabel()).ToString());
            // one category
            Assert.Equal("FacetLabel: [hello]", (new FacetLabel("hello")).ToString());
            // more than one category
            Assert.Equal("FacetLabel: [hello, world]", (new FacetLabel("hello", "world")).ToString());
        }

        [Fact]
        public virtual void TestGetComponent()
        {
            string[] components = new string[AtLeast(10)];
            for (int i = 0; i < components.Length; i++)
            {
                components[i] = Convert.ToString(i);
            }
            FacetLabel cp = new FacetLabel(components);
            for (int i = 0; i < components.Length; i++)
            {
                Assert.Equal(i, Convert.ToInt32(cp.Components[i]));
            }
        }

        [Fact]
        public virtual void TestDefaultConstructor()
        {
            // test that the default constructor (no parameters) currently
            // defaults to creating an object with a 0 initial capacity.
            // If we change this default later, we also need to change this
            // test.
            FacetLabel p = new FacetLabel();
            Assert.Equal(0, p.Length);
            Assert.Equal("FacetLabel: []", p.ToString());
        }

        [Fact]
        public virtual void TestSubPath()
        {
            FacetLabel p = new FacetLabel("hi", "there", "man");
            Assert.Equal(p.Length, 3);

            FacetLabel p1 = p.Subpath(2);
            Assert.Equal(2, p1.Length);
            Assert.Equal("FacetLabel: [hi, there]", p1.ToString());

            p1 = p.Subpath(1);
            Assert.Equal(1, p1.Length);
            Assert.Equal("FacetLabel: [hi]", p1.ToString());

            p1 = p.Subpath(0);
            Assert.Equal(0, p1.Length);
            Assert.Equal("FacetLabel: []", p1.ToString());

            // with all the following lengths, the prefix should be the whole path 
            int[] lengths = new int[] { 3, -1, 4 };
            for (int i = 0; i < lengths.Length; i++)
            {
                p1 = p.Subpath(lengths[i]);
                Assert.Equal(3, p1.Length);
                Assert.Equal("FacetLabel: [hi, there, man]", p1.ToString());
                Assert.Equal(p, p1);
            }
        }

        [Fact]
        public virtual void TestEquals()
        {
            Assert.Equal(new FacetLabel(), new FacetLabel());
            Assert.False((new FacetLabel()).Equals(new FacetLabel("hi")));
            Assert.False((new FacetLabel()).Equals(Convert.ToInt32(3)));
            Assert.Equal(new FacetLabel("hello", "world"), new FacetLabel("hello", "world"));
        }

        [Fact]
        public virtual void TestHashCode()
        {
            Assert.Equal((new FacetLabel()).GetHashCode(), (new FacetLabel()).GetHashCode());
            Assert.False((new FacetLabel()).GetHashCode() == (new FacetLabel("hi")).GetHashCode());
            Assert.Equal((new FacetLabel("hello", "world")).GetHashCode(), (new FacetLabel("hello", "world")).GetHashCode());
        }

        [Fact]
        public virtual void TestLongHashCode()
        {
            Assert.Equal((new FacetLabel()).LongHashCode(), (new FacetLabel()).LongHashCode());
            Assert.False((new FacetLabel()).LongHashCode() == (new FacetLabel("hi")).LongHashCode());
            Assert.Equal((new FacetLabel("hello", "world")).LongHashCode(), (new FacetLabel("hello", "world")).LongHashCode());
        }

        [Fact]
        public virtual void TestArrayConstructor()
        {
            FacetLabel p = new FacetLabel("hello", "world", "yo");
            Assert.Equal(3, p.Length);
            Assert.Equal("FacetLabel: [hello, world, yo]", p.ToString());
        }

        [Fact]
        public virtual void TestCompareTo()
        {
            FacetLabel p = new FacetLabel("a", "b", "c", "d");
            FacetLabel pother = new FacetLabel("a", "b", "c", "d");
            Assert.Equal(0, pother.CompareTo(p));
            Assert.Equal(0, p.CompareTo(pother));
            pother = new FacetLabel();
            Assert.True(pother.CompareTo(p) < 0);
            Assert.True(p.CompareTo(pother) > 0);
            pother = new FacetLabel("a", "b_", "c", "d");
            Assert.True(pother.CompareTo(p) > 0);
            Assert.True(p.CompareTo(pother) < 0);
            pother = new FacetLabel("a", "b", "c");
            Assert.True(pother.CompareTo(p) < 0);
            Assert.True(p.CompareTo(pother) > 0);
            pother = new FacetLabel("a", "b", "c", "e");
            Assert.True(pother.CompareTo(p) > 0);
            Assert.True(p.CompareTo(pother) < 0);
        }

        [Fact]
        public virtual void TestEmptyNullComponents()
        {
            // LUCENE-4724: CategoryPath should not allow empty or null components
            string[][] components_tests = new string[][]
		{
			new string[] {"", "test"},
			new string[] {"test", ""},
			new string[] {"test", "", "foo"},
			new string[] {null, "test"},
			new string[] {"test", null},
			new string[] {"test", null, "foo"}
		};

            foreach (string[] components in components_tests)
            {
                try
                {
                    Assert.NotNull(new FacetLabel(components));
                    True(false, "empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (System.ArgumentException)
                {
                    // expected
                }
                try
                {
                    new FacetField("dim", components);
                    True(false, "empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (System.ArgumentException)
                {
                    // expected
                }
                try
                {
                    new AssociationFacetField(new BytesRef(), "dim", components);
                    True(false, "empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (System.ArgumentException)
                {
                    // expected
                }
                try
                {
                    new IntAssociationFacetField(17, "dim", components);
                    True(false, "empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (System.ArgumentException)
                {
                    // expected
                }
                try
                {
                    new FloatAssociationFacetField(17.0f, "dim", components);
                    True(false, "empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (System.ArgumentException)
                {
                    // expected
                }
            }
            try
            {
                new FacetField(null, new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new FacetField("", new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new IntAssociationFacetField(17, null, new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new IntAssociationFacetField(17, "", new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new FloatAssociationFacetField(17.0f, null, new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new FloatAssociationFacetField(17.0f, "", new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new AssociationFacetField(new BytesRef(), null, new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new AssociationFacetField(new BytesRef(), "", new string[] { "abc" });
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new SortedSetDocValuesFacetField(null, "abc");
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new SortedSetDocValuesFacetField("", "abc");
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new SortedSetDocValuesFacetField("dim", null);
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
            try
            {
                new SortedSetDocValuesFacetField("dim", "");
                True(false, "empty or null components should not be allowed");
            }
            catch (System.ArgumentException)
            {
                // expected
            }
        }

        [Fact]
        public virtual void TestLongPath()
        {
            string bigComp = null;
            while (true)
            {
                int len = FacetLabel.MAX_CATEGORY_PATH_LENGTH;
                bigComp = TestUtil.RandomSimpleString(Random(), len, len);
                if (bigComp.IndexOf('\u001f') != -1)
                {
                    continue;
                }
                break;
            }

            try
            {
                Assert.NotNull(new FacetLabel("dim", bigComp));
                True(false, "long paths should not be allowed; len=" + bigComp.Length);
            }
            catch (System.ArgumentException)
            {
                // expected
            }
        }
    }

}