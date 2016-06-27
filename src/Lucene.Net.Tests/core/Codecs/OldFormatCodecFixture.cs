using System;
using Lucene.Net.Util;

namespace Lucene.Net.Tests.Codecs
{
    public class OldFormatCodecFixture : IDisposable
    {
        public OldFormatCodecFixture()
        {
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
        }

        public void Dispose()
        {
            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
        }
    }
}
