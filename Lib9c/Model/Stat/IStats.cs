using System.Collections.Generic;

namespace Lib9c.Model.Stat
{
    public interface IStats
    {
        long HP { get; }
        long ATK { get; }
        long DEF { get; }
        long CRI { get; }
        long HIT { get; }
        long SPD { get; }
        long DRV { get; }
        long DRR { get; }
        long CDMG { get; }
        long ArmorPenetration { get; }
        long Thorn { get; }

        IEnumerable<(StatType statType, long value)> GetStats(bool ignoreZero = false);
    }
}
