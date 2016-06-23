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

    public class TestVirtualMethod : LuceneTestCase
    {
        private static readonly VirtualMethod<TestVirtualMethod> PublicTestMethod;
        private static readonly VirtualMethod<TestVirtualMethod> ProtectedTestMethod;

        static TestVirtualMethod()
        {
            PublicTestMethod = new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "PublicTest", typeof(string));
            ProtectedTestMethod = new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "ProtectedTest", typeof(int));
        }

        public virtual void PublicTest(string test)
        {
        }

        protected virtual void ProtectedTest(int test)
        {
        }

        [Trait("Category", "Ignore")]
        internal class TestClass1 : TestVirtualMethod
        {
            public override void PublicTest(string test)
            {
            }

            protected override void ProtectedTest(int test)
            {
            }
        }

        [Trait("Category", "Ignore")]
        internal class TestClass2 : TestClass1
        {
            protected override void ProtectedTest(int test) // make it public here
            {
            }
        }

        [Trait("Category", "Ignore")]
        internal class TestClass3 : TestClass2
        {
            public override void PublicTest(string test)
            {
            }
        }

        [Trait("Category", "Ignore")]
        internal class TestClass4 : TestVirtualMethod
        {
        }

        [Trait("Category", "Ignore")]
        internal class TestClass5 : TestClass4
        {
        }

        [Fact]
        public virtual void TestGeneral()
        {
            Assert.Equal(0, PublicTestMethod.GetImplementationDistance(this.GetType()));
            Assert.Equal(1, PublicTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.Equal(1, PublicTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.Equal(3, PublicTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.False(PublicTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.False(PublicTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            Assert.Equal(0, ProtectedTestMethod.GetImplementationDistance(this.GetType()));
            Assert.Equal(1, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass1)));
            Assert.Equal(2, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass2)));
            Assert.Equal(2, ProtectedTestMethod.GetImplementationDistance(typeof(TestClass3)));
            Assert.False(ProtectedTestMethod.IsOverriddenAsOf(typeof(TestClass4)));
            Assert.False(ProtectedTestMethod.IsOverriddenAsOf(typeof(TestClass5)));

            Assert.True(VirtualMethod<TestVirtualMethod>.compareImplementationDistance(typeof(TestClass3), PublicTestMethod, ProtectedTestMethod) > 0);
            Assert.Equal(0, VirtualMethod<TestVirtualMethod>.compareImplementationDistance(typeof(TestClass5), PublicTestMethod, ProtectedTestMethod));
        }

        [Fact]
        public virtual void TestExceptions()
        {
            try
            {
                // cast to Class to remove generics:
                PublicTestMethod.GetImplementationDistance(typeof(LuceneTestCase));
                Assert.True(false, "LuceneTestCase is not a subclass and can never override publicTest(String)");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }

            try
            {
                new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "bogus");
                Assert.True(false, "Method bogus() does not exist, so IAE should be thrown");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }

            try
            {
                new VirtualMethod<TestClass2>(typeof(TestClass2), "PublicTest", typeof(string));
            }
            catch (System.ArgumentException arg)
            {
                Assert.True(false, "Method publicTest(String) is declared in TestClass2, so IAE should not be thrown");
            }

            try
            {
                // try to create a second instance of the same baseClass / method combination
                new VirtualMethod<TestVirtualMethod>(typeof(TestVirtualMethod), "PublicTest", typeof(string));
                Assert.True(false, "Violating singleton status succeeded");
            }
            catch (System.ArgumentException arg)
            {
                // pass
            }
        }
    }
}