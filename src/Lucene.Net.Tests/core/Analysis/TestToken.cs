using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lucene.Net.Analysis
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

    using Lucene.Net.Analysis.Tokenattributes;
    using Attribute = Lucene.Net.Util.Attribute;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestToken : LuceneTestCase
    {
        [Fact]
        public virtual void TestCtor()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, content.Length);
            Assert.NotSame(t.Buffer(), content);
            Assert.Equal(0, t.StartOffset());
            Assert.Equal(0, t.EndOffset());
            Assert.Equal("hello", t.ToString());
            Assert.Equal("word", t.Type);
            Assert.Equal(0, t.Flags);

            t = new Token(6, 22);
            t.CopyBuffer(content, 0, content.Length);
            Assert.Equal("hello", t.ToString());
            Assert.Equal("hello", t.ToString());
            Assert.Equal(6, t.StartOffset());
            Assert.Equal(22, t.EndOffset());
            Assert.Equal("word", t.Type);
            Assert.Equal(0, t.Flags);

            t = new Token(6, 22, 7);
            t.CopyBuffer(content, 0, content.Length);
            Assert.Equal("hello", t.ToString());
            Assert.Equal("hello", t.ToString());
            Assert.Equal(6, t.StartOffset());
            Assert.Equal(22, t.EndOffset());
            Assert.Equal("word", t.Type);
            Assert.Equal(7, t.Flags);

            t = new Token(6, 22, "junk");
            t.CopyBuffer(content, 0, content.Length);
            Assert.Equal("hello", t.ToString());
            Assert.Equal("hello", t.ToString());
            Assert.Equal(6, t.StartOffset());
            Assert.Equal(22, t.EndOffset());
            Assert.Equal("junk", t.Type);
            Assert.Equal(0, t.Flags);
        }

        [Fact]
        public virtual void TestResize()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, content.Length);
            for (int i = 0; i < 2000; i++)
            {
                t.ResizeBuffer(i);
                Assert.True(i <= t.Buffer().Length);
                Assert.Equal("hello", t.ToString());
            }
        }

        [Fact]
        public virtual void TestGrow()
        {
            Token t = new Token();
            StringBuilder buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                char[] content = buf.ToString().ToCharArray();
                t.CopyBuffer(content, 0, content.Length);
                Assert.Equal(buf.Length, t.Length);
                Assert.Equal(buf.ToString(), t.ToString());
                buf.Append(buf.ToString());
            }
            Assert.Equal(1048576, t.Length);

            // now as a string, second variant
            t = new Token();
            buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.Equal(content.Length, t.Length);
                Assert.Equal(content, t.ToString());
                buf.Append(content);
            }
            Assert.Equal(1048576, t.Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.Equal(content.Length, t.Length);
                Assert.Equal(content, t.ToString());
                buf.Append("a");
            }
            Assert.Equal(20000, t.Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.Equal(content.Length, t.Length);
                Assert.Equal(content, t.ToString());
                buf.Append("a");
            }
            Assert.Equal(20000, t.Length);
        }

        [Fact]
        public virtual void TestToString()
        {
            char[] b = new char[] { 'a', 'l', 'o', 'h', 'a' };
            Token t = new Token("", 0, 5);
            t.CopyBuffer(b, 0, 5);
            Assert.Equal("aloha", t.ToString());

            t.SetEmpty().Append("hi there");
            Assert.Equal("hi there", t.ToString());
        }

        [Fact]
        public virtual void TestTermBufferEquals()
        {
            Token t1a = new Token();
            char[] content1a = "hello".ToCharArray();
            t1a.CopyBuffer(content1a, 0, 5);
            Token t1b = new Token();
            char[] content1b = "hello".ToCharArray();
            t1b.CopyBuffer(content1b, 0, 5);
            Token t2 = new Token();
            char[] content2 = "hello2".ToCharArray();
            t2.CopyBuffer(content2, 0, 6);
            Assert.True(t1a.Equals(t1b));
            Assert.False(t1a.Equals(t2));
            Assert.False(t2.Equals(t1b));
        }

        [Fact]
        public virtual void TestMixedStringArray()
        {
            Token t = new Token("hello", 0, 5);
            Assert.Equal(t.Length, 5);
            Assert.Equal(t.ToString(), "hello");
            t.SetEmpty().Append("hello2");
            Assert.Equal(t.Length, 6);
            Assert.Equal(t.ToString(), "hello2");
            t.CopyBuffer("hello3".ToCharArray(), 0, 6);
            Assert.Equal(t.ToString(), "hello3");

            char[] buffer = t.Buffer();
            buffer[1] = 'o';
            Assert.Equal(t.ToString(), "hollo3");
        }

        [Fact]
        public virtual void TestClone()
        {
            Token t = new Token(0, 5);
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer();
            Token copy = AssertCloneIsEqual(t);
            Assert.Equal(t.ToString(), copy.ToString());
            Assert.NotSame(buf, copy.Buffer());

            BytesRef pl = new BytesRef(new byte[] { 1, 2, 3, 4 });
            t.Payload = pl;
            copy = AssertCloneIsEqual(t);
            Assert.Equal(pl, copy.Payload);
            Assert.NotSame(pl, copy.Payload);
        }

        [Fact]
        public virtual void TestCopyTo()
        {
            Token t = new Token();
            Token copy = AssertCopyIsEqual(t);
            Assert.Equal("", t.ToString());
            Assert.Equal("", copy.ToString());

            t = new Token(0, 5);
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer();
            copy = AssertCopyIsEqual(t);
            Assert.Equal(t.ToString(), copy.ToString());
            Assert.NotSame(buf, copy.Buffer());

            BytesRef pl = new BytesRef(new byte[] { 1, 2, 3, 4 });
            t.Payload = pl;
            copy = AssertCopyIsEqual(t);
            Assert.Equal(pl, copy.Payload);
            Assert.NotSame(pl, copy.Payload);
        }

        public interface ISenselessAttribute : Lucene.Net.Util.IAttribute
        {
        }

        public sealed class SenselessAttribute : Attribute, ISenselessAttribute
        {
            public override void CopyTo(Attribute target)
            {
            }

            public override void Clear()
            {
            }

            public override bool Equals(object o)
            {
                return (o is SenselessAttribute);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Fact]
        public virtual void TestTokenAttributeFactory()
        {
            TokenStream ts = new MockTokenizer(Token.TOKEN_ATTRIBUTE_FACTORY, new System.IO.StringReader("foo bar"), MockTokenizer.WHITESPACE, false, MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH);

            Assert.True(ts.AddAttribute<ISenselessAttribute>() is SenselessAttribute, "SenselessAttribute is not implemented by SenselessAttributeImpl");

            Assert.True(ts.AddAttribute<ICharTermAttribute>() is Token, "CharTermAttribute is not implemented by Token");
            Assert.True(ts.AddAttribute<IOffsetAttribute>() is Token, "OffsetAttribute is not implemented by Token");
            Assert.True(ts.AddAttribute<IFlagsAttribute>() is Token, "FlagsAttribute is not implemented by Token");
            Assert.True(ts.AddAttribute<IPayloadAttribute>() is Token, "PayloadAttribute is not implemented by Token");
            Assert.True(ts.AddAttribute<IPositionIncrementAttribute>() is Token, "PositionIncrementAttribute is not implemented by Token");
            Assert.True(ts.AddAttribute<ITypeAttribute>() is Token, "TypeAttribute is not implemented by Token");
        }

        [Fact]
        public virtual void TestAttributeReflection()
        {
            Token t = new Token("foobar", 6, 22, 8);
            TestUtil.AssertAttributeReflection(t, new Dictionary<string, object>()
            {
                { typeof(ICharTermAttribute).Name + "#term", "foobar" },
                { typeof(ITermToBytesRefAttribute).Name + "#bytes", new BytesRef("foobar") },
                { typeof(IOffsetAttribute).Name + "#startOffset", 6 },
                { typeof(IOffsetAttribute).Name + "#endOffset", 22 },
                { typeof(IPositionIncrementAttribute).Name + "#positionIncrement", 1 },
                { typeof(IPayloadAttribute).Name + "#payload", null },
                { typeof(ITypeAttribute).Name + "#type", TypeAttribute_Fields.DEFAULT_TYPE },
                { typeof(IFlagsAttribute).Name + "#flags", 8 }
            });
        }

        public static T AssertCloneIsEqual<T>(T att) where T : Attribute
        {
            T clone = (T)att.Clone();
            Assert.Equal(att, clone); //, "Clone must be equal");
            Assert.Equal(att.GetHashCode(), clone.GetHashCode()); //, "Clone's hashcode must be equal");
            return clone;
        }

        public static T AssertCopyIsEqual<T>(T att) where T : Attribute
        {
            T copy = (T)System.Activator.CreateInstance(att.GetType());
            att.CopyTo(copy);
            Assert.Equal(att, copy); //, "Copied instance must be equal");
            Assert.Equal(att.GetHashCode(), copy.GetHashCode()); //, "Copied instance's hashcode must be equal");
            return copy;
        }
    }
}