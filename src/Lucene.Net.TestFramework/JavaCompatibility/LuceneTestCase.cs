using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Lucene.Net.Util
{
    public abstract partial class LuceneTestCase
    {
        public static void assertTrue(bool condition)
        {
            Assert.True(condition);
        }

        public static void assertTrue(string message, bool condition)
        {
            Assert.True(condition, message);
        }

        public static void assertFalse(bool condition)
        {
            Assert.False(condition);
        }

        public static void assertFalse(string message, bool condition)
        {
            Assert.False(condition, message);
        }

        public static void assertEquals(object expected, object actual)
        {
            Assert.Equal(expected, actual);
        }

        public static void assertEquals(string message, object expected, object actual)
        {
            Assert.Equal(expected, actual);//, message);
        }

        public static void assertEquals(long expected, long actual)
        {
            Assert.Equal(expected, actual);
        }

        public static void assertEquals(string message, long expected, long actual)
        {
            Assert.Equal(expected, actual); //, message);
        }

        public static void assertEquals<T>(ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual));
        }

        public static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual), message);
        }

        public static void assertEquals<T, S>(IDictionary<T, S> expected, IDictionary<T, S> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.Equal(expected[key], actual[key]);
            }
        }

        public static void assertNotSame(object unexpected, object actual)
        {
            Assert.NotSame(unexpected, actual);
        }

        public static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.NotSame(unexpected, actual); //, message);
        }

        protected static void assertEquals(double expected, double actual, double delta)
        {
            double min = expected - delta;
            double max = expected + delta;
            Assert.InRange(actual, min, max);
        }

        protected static void assertEquals(float expected, float actual, float delta)
        {
            float min = expected - delta;
            float max = expected + delta;
            Assert.InRange(actual, min, max);
        }

        protected static void assertEquals(string msg, double expected, double actual, double delta)
        {
            double min = expected - delta;
            double max = expected + delta;
            Assert.InRange(actual, min, max);//, msg);
        }

        protected static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        protected static void assertNotNull(string msg, object o)
        {
            Assert.True(o != null, msg);
        }

        protected static void assertNull(object o)
        {
            Assert.Null(o);
        }

        protected static void assertNull(string msg, object o)
        {
            Assert.True(o == null, msg);
        }

        protected static void assertArrayEquals(IEnumerable a1, IEnumerable a2)
        {
            Assert.Equal(a1, a2);
        }

        protected static void fail()
        {
            Assert.True(false);
        }

        protected static void fail(string message)
        {
            Assert.True(false, message);
        }
    }
}
