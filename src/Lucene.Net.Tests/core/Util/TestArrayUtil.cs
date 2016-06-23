using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using System;
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

    public class TestArrayUtil : LuceneTestCase
    {
        // Ensure ArrayUtil.getNextSize gives linear amortized cost of realloc/copy
        [Fact]
        public virtual void TestGrowth()
        {
            int currentSize = 0;
            long copyCost = 0;

            // Make sure ArrayUtil hits Integer.MAX_VALUE, if we insist:
            while (currentSize != int.MaxValue)
            {
                int nextSize = ArrayUtil.Oversize(1 + currentSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                Assert.True(nextSize > currentSize);
                if (currentSize > 0)
                {
                    copyCost += currentSize;
                    double copyCostPerElement = ((double)copyCost) / currentSize;
                    Assert.True(copyCostPerElement < 10.0, "cost " + copyCostPerElement);
                }
                currentSize = nextSize;
            }
        }

        [Fact]
        public virtual void TestMaxSize()
        {
            // intentionally pass invalid elemSizes:
            for (int elemSize = 0; elemSize < 10; elemSize++)
            {
                Assert.Equal(int.MaxValue, ArrayUtil.Oversize(int.MaxValue, elemSize));
                Assert.Equal(int.MaxValue, ArrayUtil.Oversize(int.MaxValue - 1, elemSize));
            }
        }

        [Fact]
        public virtual void TestInvalidElementSizes()
        {
            Random rnd = Random();
            int num = AtLeast(10000);
            for (int iter = 0; iter < num; iter++)
            {
                int minTargetSize = rnd.Next(int.MaxValue);
                int elemSize = rnd.Next(11);
                int v = ArrayUtil.Oversize(minTargetSize, elemSize);
                Assert.True(v >= minTargetSize);
            }
        }

        [Fact]
        public virtual void TestParseInt()
        {
            int test;
            try
            {
                test = ArrayUtil.ParseInt("".ToCharArray());
                Assert.True(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt("foo".ToCharArray());
                Assert.True(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt(Convert.ToString(long.MaxValue).ToCharArray());
                Assert.True(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt("0.34".ToCharArray());
                Assert.True(false);
            }
            catch (FormatException e)
            {
                //expected
            }

            try
            {
                test = ArrayUtil.ParseInt("1".ToCharArray());
                Assert.True(test == 1, test + " does not equal: " + 1);
                test = ArrayUtil.ParseInt("-10000".ToCharArray());
                Assert.True(test == -10000, test + " does not equal: " + -10000);
                test = ArrayUtil.ParseInt("1923".ToCharArray());
                Assert.True(test == 1923, test + " does not equal: " + 1923);
                test = ArrayUtil.ParseInt("-1".ToCharArray());
                Assert.True(test == -1, test + " does not equal: " + -1);
                test = ArrayUtil.ParseInt("foo 1923 bar".ToCharArray(), 4, 4);
                Assert.True(test == 1923, test + " does not equal: " + 1923);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.True(false);
            }
        }

        [Fact]
        public virtual void TestSliceEquals()
        {
            string left = "this is equal";
            string right = left;
            char[] leftChars = left.ToCharArray();
            char[] rightChars = right.ToCharArray();
            Assert.True(ArrayUtil.Equals(leftChars, 0, rightChars, 0, left.Length), left + " does not equal: " + right);

            Assert.False(ArrayUtil.Equals(leftChars, 1, rightChars, 0, left.Length), left + " does not equal: " + right);
            Assert.False(ArrayUtil.Equals(leftChars, 1, rightChars, 2, left.Length), left + " does not equal: " + right);

            Assert.False(ArrayUtil.Equals(leftChars, 25, rightChars, 0, left.Length), left + " does not equal: " + right);
            Assert.False(ArrayUtil.Equals(leftChars, 12, rightChars, 0, left.Length), left + " does not equal: " + right);
        }

        private int[] CreateRandomArray(int maxSize)
        {
            Random rnd = Random();
            int[] a = new int[rnd.Next(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = Convert.ToInt32(rnd.Next(a.Length));
            }
            return a;
        }

        private CollectionsHelper.ReverseComparer<int> ReverseOrder = new CollectionsHelper.ReverseComparer<int>();

        [Fact]
        public virtual void TestIntroSort()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                int[] a1 = CreateRandomArray(2000);
                int[] a2 = (int[])a1.Clone();
                ArrayUtil.IntroSort(a1);
                Array.Sort(a2);
                Assert.Equal(a2, a1);

                a1 = CreateRandomArray(2000);
                a2 = (int[])a1.Clone();
                ArrayUtil.IntroSort(a1, ReverseOrder);
                Array.Sort(a2, ReverseOrder);
                Assert.Equal(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.IntroSort(a1);
                Array.Sort(a2);
                Assert.Equal(a2, a1);
            }
        }

        private int[] CreateSparseRandomArray(int maxSize)
        {
            Random rnd = Random();
            int[] a = new int[rnd.Next(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = Convert.ToInt32(rnd.Next(2));
            }
            return a;
        }

        // this is a test for LUCENE-3054 (which fails without the merge sort fall back with stack overflow in most cases)
        [Fact]
        public virtual void TestQuickToHeapSortFallback()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                int[] a1 = CreateSparseRandomArray(40000);
                int[] a2 = (int[])a1.Clone();
                ArrayUtil.IntroSort(a1);
                Array.Sort(a2);
                Assert.Equal(a2, a1);
            }
        }

        [Fact]
        public virtual void TestTimSort()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                int[] a1 = CreateRandomArray(2000), a2 = (int[])a1.Clone();
                ArrayUtil.TimSort(a1);
                Array.Sort(a2);
                Assert.Equal(a2, a1);

                a1 = CreateRandomArray(2000);
                a2 = (int[])a1.Clone();
                ArrayUtil.TimSort(a1, ReverseOrder);
                Array.Sort(a2, ReverseOrder);
                Assert.Equal(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.TimSort(a1);
                Array.Sort(a2);
                Assert.Equal(a2, a1);
            }
        }

        internal class Item : IComparable<Item>
        {
            internal readonly int Val, Order;

            internal Item(int val, int order)
            {
                this.Val = val;
                this.Order = order;
            }

            public virtual int CompareTo(Item other)
            {
                return this.Order - other.Order;
            }

            public override string ToString()
            {
                return Convert.ToString(Val);
            }
        }

        [Fact]
        public virtual void TestMergeSortStability()
        {
            Random rnd = Random();
            Item[] items = new Item[100];
            for (int i = 0; i < items.Length; i++)
            {
                // half of the items have value but same order. The value of this items is sorted,
                // so they should always be in order after sorting.
                // The other half has defined order, but no (-1) value (they should appear after
                // all above, when sorted).
                bool equal = rnd.NextBoolean();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
            }

            if (VERBOSE)
            {
                Console.WriteLine("Before: " + Arrays.ToString(items));
            }
            // if you replace this with ArrayUtil.quickSort(), test should fail:
            ArrayUtil.TimSort(items);
            if (VERBOSE)
            {
                Console.WriteLine("Sorted: " + Arrays.ToString(items));
            }

            Item last = items[0];
            for (int i = 1; i < items.Length; i++)
            {
                Item act = items[i];
                if (act.Order == 0)
                {
                    // order of "equal" items should be not mixed up
                    Assert.True(act.Val > last.Val);
                }
                Assert.True(act.Order >= last.Order);
                last = act;
            }
        }

        [Fact]
        public virtual void TestTimSortStability()
        {
            Random rnd = Random();
            Item[] items = new Item[100];
            for (int i = 0; i < items.Length; i++)
            {
                // half of the items have value but same order. The value of this items is sorted,
                // so they should always be in order after sorting.
                // The other half has defined order, but no (-1) value (they should appear after
                // all above, when sorted).
                bool equal = rnd.NextBoolean();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
            }

            if (VERBOSE)
            {
                Console.WriteLine("Before: " + Arrays.ToString(items));
            }
            // if you replace this with ArrayUtil.quickSort(), test should fail:
            ArrayUtil.TimSort(items);
            if (VERBOSE)
            {
                Console.WriteLine("Sorted: " + Arrays.ToString(items));
            }

            Item last = items[0];
            for (int i = 1; i < items.Length; i++)
            {
                Item act = items[i];
                if (act.Order == 0)
                {
                    // order of "equal" items should be not mixed up
                    Assert.True(act.Val > last.Val);
                }
                Assert.True(act.Order >= last.Order);
                last = act;
            }
        }

        // should produce no exceptions
        [Fact]
        public virtual void TestEmptyArraySort()
        {
            int[] a = new int[0];
            ArrayUtil.IntroSort(a);
            ArrayUtil.TimSort(a);
            ArrayUtil.IntroSort(a, ReverseOrder);
            ArrayUtil.TimSort(a, ReverseOrder);
        }
    }
}