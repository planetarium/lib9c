namespace Nekoyume.Model.Stat
{
    public interface IIntStatWithCurrent: IIntStat
    {
        public int Current { get; set; }

        public void SetCurrent(int value);
        void EqualizeCurrentWithValue();
    }
}
