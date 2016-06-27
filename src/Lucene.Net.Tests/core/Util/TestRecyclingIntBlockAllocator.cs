using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Testcase for <seealso cref="RecyclingIntBlockAllocator"/>
    /// </summary>
    public class TestRecyclingIntBlockAllocator : LuceneTestCase
    {
        private RecyclingIntBlockAllocator NewAllocator()
        {
            return new RecyclingIntBlockAllocator(1 << (2 + Random().Next(15)), Random().Next(97), Util.Counter.NewCounter());
        }

        [Fact]
        public virtual void TestAllocate()
        {
            RecyclingIntBlockAllocator allocator = NewAllocator();
            HashSet<int[]> set = new HashSet<int[]>();
            int[] block = allocator.IntBlock;
            set.Add(block);
            Assert.NotNull(block);
            int size = block.Length;

            int num = AtLeast(97);
            for (int i = 0; i < num; i++)
            {
                block = allocator.IntBlock;
                Assert.NotNull(block);
                Assert.Equal(size, block.Length);
                Assert.True(set.Add(block), "block is returned twice");
                Assert.Equal(4 * size * (i + 2), allocator.BytesUsed()); // zero based + 1
                Assert.Equal(0, allocator.NumBufferedBlocks());
            }
        }

        [Fact]
        public virtual void TestAllocateAndRecycle()
        {
            RecyclingIntBlockAllocator allocator = NewAllocator();
            HashSet<int[]> allocated = new HashSet<int[]>();

            int[] block = allocator.IntBlock;
            allocated.Add(block);
            Assert.NotNull(block);
            int size = block.Length;

            int numIters = AtLeast(97);
            for (int i = 0; i < numIters; i++)
            {
                int num = 1 + Random().Next(39);
                for (int j = 0; j < num; j++)
                {
                    block = allocator.IntBlock;
                    Assert.NotNull(block);
                    Assert.Equal(size, block.Length);
                    Assert.True(allocated.Add(block), "block is returned twice");
                    Assert.Equal(4 * size * (allocated.Count + allocator.NumBufferedBlocks()), allocator.BytesUsed());
                }
                int[][] array = allocated.ToArray(/*new int[0][]*/);
                int begin = Random().Next(array.Length);
                int end = begin + Random().Next(array.Length - begin);
                IList<int[]> selected = new List<int[]>();
                for (int j = begin; j < end; j++)
                {
                    selected.Add(array[j]);
                }
                allocator.RecycleIntBlocks(array, begin, end);
                for (int j = begin; j < end; j++)
                {
                    Assert.Null(array[j]);
                    int[] b = selected[0];
                    selected.RemoveAt(0);
                    Assert.True(allocated.Remove(b));
                }
            }
        }

        [Fact]
        public virtual void TestAllocateAndFree()
        {
            RecyclingIntBlockAllocator allocator = NewAllocator();
            HashSet<int[]> allocated = new HashSet<int[]>();
            int freeButAllocated = 0;
            int[] block = allocator.IntBlock;
            allocated.Add(block);
            Assert.NotNull(block);
            int size = block.Length;

            int numIters = AtLeast(97);
            for (int i = 0; i < numIters; i++)
            {
                int num = 1 + Random().Next(39);
                for (int j = 0; j < num; j++)
                {
                    block = allocator.IntBlock;
                    freeButAllocated = Math.Max(0, freeButAllocated - 1);
                    Assert.NotNull(block);
                    Assert.Equal(size, block.Length);
                    Assert.True(allocated.Add(block), "block is returned twice");
                    Assert.Equal(4 * size * (allocated.Count + allocator.NumBufferedBlocks()), allocator.BytesUsed()); //, "" + (4 * size * (allocated.Count + allocator.NumBufferedBlocks()) - allocator.BytesUsed()));
                }

                int[][] array = allocated.ToArray(/*new int[0][]*/);
                int begin = Random().Next(array.Length);
                int end = begin + Random().Next(array.Length - begin);
                for (int j = begin; j < end; j++)
                {
                    int[] b = array[j];
                    Assert.True(allocated.Remove(b));
                }
                allocator.RecycleIntBlocks(array, begin, end);
                for (int j = begin; j < end; j++)
                {
                    Assert.Null(array[j]);
                }
                // randomly free blocks
                int numFreeBlocks = allocator.NumBufferedBlocks();
                int freeBlocks = allocator.FreeBlocks(Random().Next(7 + allocator.MaxBufferedBlocks()));
                Assert.Equal(allocator.NumBufferedBlocks(), numFreeBlocks - freeBlocks);
            }
        }
    }
}