using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IStats
    {
        IIntStatWithCurrent hp { get; }
        IIntStat atk { get; }
        IIntStat def { get; }
        IDecimalStat cri { get; }
        IDecimalStat hit { get; }
        IDecimalStat spd { get; }
        IIntStat drv { get; }
        IIntStat drr { get; }
        IIntStat cdmg { get; }

        int HP { get; }
        int ATK { get; }
        int DEF { get; }
        int CRI { get; }
        int HIT { get; }
        int SPD { get; }
        int DRV { get; }
        int DRR { get; }
        int CDMG { get; }

        bool HasHP { get; }
        bool HasATK { get; }
        bool HasDEF { get; }
        bool HasCRI { get; }
        bool HasHIT { get; }
        bool HasSPD { get; }
        bool HasDRV { get; }
        bool HasDRR { get; }
        bool HasCDMG { get; }

        IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false);
    }
}
