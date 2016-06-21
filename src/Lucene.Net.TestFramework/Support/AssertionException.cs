using System;

namespace Lucene.Net.TestFramework.Support
{
    class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
