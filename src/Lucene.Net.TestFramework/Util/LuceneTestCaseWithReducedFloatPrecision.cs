using Lucene.Net.Support;

namespace Lucene.Net.Util
{
    public class LuceneTestCaseWithReducedFloatPrecision :  LuceneTestCase
    {
        public LuceneTestCaseWithReducedFloatPrecision()
            : base()
        {
            // set precision
            FloatUtils.SetPrecision();
        }
    }
}
