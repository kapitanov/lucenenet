
namespace Lucene.Net.Analysis.Util
{
    public interface ICharacterIterator
    {
        char First();
        char Last();
        char Current();
        char Next();
        char Previous();
        char SetIndex(int position);
        int GetBeginIndex();
        int GetEndIndex();
        int GetIndex();
    }
}