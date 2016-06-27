using System.Text;
using Lucene.Net.Documents;
using Xunit;

namespace Lucene.Net.Document
{
    using Lucene.Net.Support;
    using System;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using Token = Lucene.Net.Analysis.Token;

    // sanity check some basics of fields
    public class TestField : LuceneTestCase
    {
        [Fact]
        public virtual void TestDoubleField()
        {
            Field[] fields = new Field[] { new DoubleField("foo", 5d, Field.Store.NO), new DoubleField("foo", 5d, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                field.DoubleValue = 6d; // ok
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                assertEquals(6d, (double)field.NumericValue, 0.0d);
            }
        }

        [Fact]
        public virtual void TestDoubleDocValuesField()
        {
            DoubleDocValuesField field = new DoubleDocValuesField("foo", 5d);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            field.DoubleValue = 6d; // ok
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            assertEquals(6d, BitConverter.Int64BitsToDouble((long)field.NumericValue), 0.0d);
        }

        [Fact]
        public virtual void TestFloatDocValuesField()
        {
            FloatDocValuesField field = new FloatDocValuesField("foo", 5f);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            field.FloatValue = 6f; // ok
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            assertEquals(6f, Number.IntBitsToFloat(Convert.ToInt32(field.NumericValue)), 0.0f);
        }

        [Fact]
        public virtual void TestFloatField()
        {
            Field[] fields = new Field[] { new FloatField("foo", 5f, Field.Store.NO), new FloatField("foo", 5f, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                field.FloatValue = 6f; // ok
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                assertEquals(6f, (float)field.NumericValue, 0.0f);
            }
        }

        [Fact]
        public virtual void TestIntField()
        {
            Field[] fields = new Field[] { new IntField("foo", 5, Field.Store.NO), new IntField("foo", 5, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                field.IntValue = 6; // ok
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.Equal(6, (int)field.NumericValue);
            }
        }

        [Fact]
        public virtual void TestNumericDocValuesField()
        {
            NumericDocValuesField field = new NumericDocValuesField("foo", 5L);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            field.LongValue = 6; // ok
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.Equal(6L, (long)field.NumericValue);
        }

        [Fact]
        public virtual void TestLongField()
        {
            Field[] fields = new Field[] { new LongField("foo", 5L, Field.Store.NO), new LongField("foo", 5L, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                field.LongValue = 6; // ok
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.Equal(6L, (long)field.NumericValue);
            }
        }

        [Fact]
        public virtual void TestSortedBytesDocValuesField()
        {
            SortedDocValuesField field = new SortedDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.BytesValue = "fubar".ToBytesRefArray(Encoding.UTF8);
            field.BytesValue = new BytesRef("baz");
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.Equal(new BytesRef("baz"), field.BinaryValue());
        }

        [Fact]
        public virtual void TestBinaryDocValuesField()
        {
            BinaryDocValuesField field = new BinaryDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.BytesValue = "fubar".ToBytesRefArray(Encoding.UTF8);
            field.BytesValue = new BytesRef("baz");
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.Equal(new BytesRef("baz"), field.BinaryValue());
        }

        [Fact]
        public virtual void TestStringField()
        {
            Field[] fields = new Field[] { new StringField("foo", "bar", Field.Store.NO), new StringField("foo", "bar", Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                field.StringValue = "baz";
                TrySetTokenStreamValue(field);

                Assert.Equal("baz", field.StringValue);
            }
        }

        [Fact]
        public virtual void TestTextFieldString()
        {
            Field[] fields = new Field[] { new TextField("foo", "bar", Field.Store.NO), new TextField("foo", "bar", Field.Store.YES) };

            foreach (Field field in fields)
            {
                field.Boost = 5f;
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                field.StringValue = "baz";
                field.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));

                Assert.Equal("baz", field.StringValue);

                assertEquals(5f, field.GetBoost(), 0f);
            }
        }

        [Fact]
        public virtual void TestTextFieldReader()
        {
            Field field = new TextField("foo", new StringReader("bar"));

            field.Boost = 5f;
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            field.ReaderValue = new StringReader("foobar");
            TrySetShortValue(field);
            TrySetStringValue(field);
            field.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));

            Assert.NotNull(field.ReaderValue);

            assertEquals(5f, field.GetBoost(), 0f);
        }

        /* TODO: this is pretty expert and crazy
         * see if we can fix it up later
        public void testTextFieldTokenStream() throws Exception {
        }
        */

        [Fact]
        public virtual void TestStoredFieldBytes()
        {
            Field[] fields = new Field[] { new StoredField("foo", "bar".GetBytes(Encoding.UTF8)), new StoredField("foo", "bar".GetBytes(Encoding.UTF8), 0, 3), new StoredField("foo", new BytesRef("bar")) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                field.BytesValue = "baz".ToBytesRefArray(Encoding.UTF8);
                field.BytesValue = new BytesRef("baz");
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.Equal(new BytesRef("baz"), field.BinaryValue());
            }
        }

        [Fact]
        public virtual void TestStoredFieldString()
        {
            Field field = new StoredField("foo", "bar");
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            field.StringValue = "baz";
            TrySetTokenStreamValue(field);

            Assert.Equal("baz", field.StringValue);
        }

        [Fact]
        public virtual void TestStoredFieldInt()
        {
            Field field = new StoredField("foo", 1);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            field.IntValue = 5;
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.Equal(5, (int)field.NumericValue);
        }

        [Fact]
        public virtual void TestStoredFieldDouble()
        {
            Field field = new StoredField("foo", 1D);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            field.DoubleValue = 5D;
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            assertEquals(5D, (double)field.NumericValue, 0.0d);
        }

        [Fact]
        public virtual void TestStoredFieldFloat()
        {
            Field field = new StoredField("foo", 1F);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            field.FloatValue = 5f;
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            assertEquals(5f, (float)field.NumericValue, 0.0f);
        }

        [Fact]
        public virtual void TestStoredFieldLong()
        {
            Field field = new StoredField("foo", 1L);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            field.LongValue = 5;
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.Equal(5L, (long)field.NumericValue);
        }

        private void TrySetByteValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.ByteValue = (sbyte)10;
            });
        }

        private void TrySetBytesValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.BytesValue = new BytesRef(new byte[] { 5, 5 });
            });
        }

        private void TrySetBytesRefValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.BytesValue = new BytesRef("bogus");
            });
        }

        private void TrySetDoubleValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.DoubleValue = double.MaxValue;
            });
        }

        private void TrySetIntValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.IntValue = int.MaxValue;
            });
        }

        private void TrySetLongValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.LongValue = long.MaxValue;
            });
        }

        private void TrySetFloatValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.FloatValue = float.MaxValue;
            });
        }

        private void TrySetReaderValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.ReaderValue = new StringReader("BOO!");
            });
        }

        private void TrySetShortValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.ShortValue = short.MaxValue;
            });
        }

        private void TrySetStringValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.StringValue = "BOO!";
            });
        }

        private void TrySetTokenStreamValue(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));
            });
        }

        private void TrySetBoost(Field f)
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                f.Boost = 5.0f;
            });
        }
    }
}