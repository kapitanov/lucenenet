using System;
using System.Collections.Generic;
using Xunit;

namespace Lucene.Net.Util
{
    using Lucene.Net.Analysis.Tokenattributes;

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

    using Token = Lucene.Net.Analysis.Token;

    public class TestAttributeSource : LuceneTestCase
    {
        [Fact]
        public virtual void TestCaptureState()
        {
            // init a first instance
            AttributeSource src = new AttributeSource();
            ICharTermAttribute termAtt = src.AddAttribute<ICharTermAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            termAtt.Append("TestTerm");
            typeAtt.Type = "TestType";
            int hashCode = src.GetHashCode();

            AttributeSource.State state = src.CaptureState();

            // modify the attributes
            termAtt.SetEmpty().Append("AnotherTestTerm");
            typeAtt.Type = "AnotherTestType";
            Assert.True(hashCode != src.GetHashCode(), "Hash code should be different");

            src.RestoreState(state);
            Assert.Equal(termAtt.ToString(), "TestTerm");
            Assert.Equal(typeAtt.Type, "TestType");
            Assert.Equal(hashCode, src.GetHashCode()); //, "Hash code should be equal after restore");

            // restore into an exact configured copy
            AttributeSource copy = new AttributeSource();
            copy.AddAttribute<ICharTermAttribute>();
            copy.AddAttribute<ITypeAttribute>();
            copy.RestoreState(state);
            Assert.Equal(src.GetHashCode(), copy.GetHashCode()); //, "Both AttributeSources should have same hashCode after restore");
            Assert.Equal(src, copy); //, "Both AttributeSources should be equal after restore");

            // init a second instance (with attributes in different order and one additional attribute)
            AttributeSource src2 = new AttributeSource();
            typeAtt = src2.AddAttribute<ITypeAttribute>();
            IFlagsAttribute flagsAtt = src2.AddAttribute<IFlagsAttribute>();
            termAtt = src2.AddAttribute<ICharTermAttribute>();
            flagsAtt.Flags = 12345;

            src2.RestoreState(state);
            Assert.Equal(termAtt.ToString(), "TestTerm");
            Assert.Equal(typeAtt.Type, "TestType");
            Assert.Equal(12345, flagsAtt.Flags); //, "FlagsAttribute should not be touched");

            // init a third instance missing one Attribute
            AttributeSource src3 = new AttributeSource();
            termAtt = src3.AddAttribute<ICharTermAttribute>();
            try
            {
                src3.RestoreState(state);
                Assert.True(false, "The third instance is missing the TypeAttribute, so restoreState() should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
                // pass
            }
        }

        [Fact]
        public virtual void TestCloneAttributes()
        {
            AttributeSource src = new AttributeSource();
            IFlagsAttribute flagsAtt = src.AddAttribute<IFlagsAttribute>();
            ITypeAttribute typeAtt = src.AddAttribute<ITypeAttribute>();
            flagsAtt.Flags = 1234;
            typeAtt.Type = "TestType";

            AttributeSource clone = src.CloneAttributes();
            IEnumerator<Type> it = clone.AttributeClassesIterator;
            it.MoveNext();
            Assert.Equal(typeof(IFlagsAttribute), it.Current); //, "FlagsAttribute must be the first attribute");
            it.MoveNext();
            Assert.Equal(typeof(ITypeAttribute), it.Current); //, "TypeAttribute must be the second attribute");
            Assert.False(it.MoveNext(), "No more attributes");

            IFlagsAttribute flagsAtt2 = clone.GetAttribute<IFlagsAttribute>();
            ITypeAttribute typeAtt2 = clone.GetAttribute<ITypeAttribute>();
            Assert.NotSame(flagsAtt2, flagsAtt); //, "FlagsAttribute of original and clone must be different instances");
            Assert.NotSame(typeAtt2, typeAtt); //, "TypeAttribute of original and clone must be different instances");
            Assert.Equal(flagsAtt2, flagsAtt); //, "FlagsAttribute of original and clone must be equal");
            Assert.Equal(typeAtt2, typeAtt); //, "TypeAttribute of original and clone must be equal");

            // test copy back
            flagsAtt2.Flags = 4711;
            typeAtt2.Type = "OtherType";
            clone.CopyTo(src);
            Assert.Equal(4711, flagsAtt.Flags); //, "FlagsAttribute of original must now contain updated term");
            Assert.Equal(typeAtt.Type, "OtherType"); //, "TypeAttribute of original must now contain updated type");
            // verify again:
            Assert.NotSame(flagsAtt2, flagsAtt); //, "FlagsAttribute of original and clone must be different instances");
            Assert.NotSame(typeAtt2, typeAtt); //, "TypeAttribute of original and clone must be different instances");
            Assert.Equal(flagsAtt2, flagsAtt); //, "FlagsAttribute of original and clone must be equal");
            Assert.Equal(typeAtt2, typeAtt); //, "TypeAttribute of original and clone must be equal");
        }

        [Fact]
        public virtual void TestDefaultAttributeFactory()
        {
            AttributeSource src = new AttributeSource();

            Assert.True(src.AddAttribute<ICharTermAttribute>() is CharTermAttribute, "CharTermAttribute is not implemented by CharTermAttributeImpl");
            Assert.True(src.AddAttribute<IOffsetAttribute>() is OffsetAttribute, "OffsetAttribute is not implemented by OffsetAttributeImpl");
            Assert.True(src.AddAttribute<IFlagsAttribute>() is FlagsAttribute, "FlagsAttribute is not implemented by FlagsAttributeImpl");
            Assert.True(src.AddAttribute<IPayloadAttribute>() is PayloadAttribute, "PayloadAttribute is not implemented by PayloadAttributeImpl");
            Assert.True(src.AddAttribute<IPositionIncrementAttribute>() is PositionIncrementAttribute, "PositionIncrementAttribute is not implemented by PositionIncrementAttributeImpl");
            Assert.True(src.AddAttribute<ITypeAttribute>() is TypeAttribute, "TypeAttribute is not implemented by TypeAttributeImpl");
        }

        [Fact]
        public virtual void TestInvalidArguments()
        {
            try
            {
                AttributeSource src = new AttributeSource();
                src.AddAttribute<Token>();
                Assert.True(false, "Should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
            }

            try
            {
                AttributeSource src = new AttributeSource(Token.TOKEN_ATTRIBUTE_FACTORY);
                src.AddAttribute<Token>();
                Assert.True(false, "Should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
            }

            /*try
            {
              AttributeSource src = new AttributeSource();
              // break this by unsafe cast
              src.AddAttribute<typeof((Type)IEnumerator)>();
              Assert.True(false, "Should throw IllegalArgumentException");
            }
            catch (System.ArgumentException iae)
            {
            }*/
        }

        [Fact]
        public virtual void TestLUCENE_3042()
        {
            AttributeSource src1 = new AttributeSource();
            src1.AddAttribute<ICharTermAttribute>().Append("foo");
            int hash1 = src1.GetHashCode(); // this triggers a cached state
            AttributeSource src2 = new AttributeSource(src1);
            src2.AddAttribute<ITypeAttribute>().Type = "bar";
            Assert.True(hash1 != src1.GetHashCode(), "The hashCode is identical, so the captured state was preserved.");
            Assert.Equal(src2.GetHashCode(), src1.GetHashCode());
        }
    }
}