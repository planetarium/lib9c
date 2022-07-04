namespace Nekoyume.TableData
{
    public interface ISheet
    {
        void Set(string csv, bool isReversed = false);
        int Count { get; }
        void Validate();
    }
}
