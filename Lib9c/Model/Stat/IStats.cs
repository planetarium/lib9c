using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IStats
    {
        int HP { get; }
        decimal HPAsDecimal { get; }
        int ATK { get; }
        decimal ATKAsDecimal { get; }
        int DEF { get; }
        decimal DEFAsDecimal { get; }
        int CRI { get; }
        decimal CRIAsDecimal { get; }
        int HIT { get; }
        decimal HITAsDecimal { get; }
        int SPD { get; }
        decimal SPDAsDecimal { get; }

        bool HasHP { get; }
        bool HasATK { get; }
        bool HasDEF { get; }
        bool HasCRI { get; }
        bool HasHIT { get; }
        bool HasSPD { get; }

        IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = default);
        
        IEnumerable<(StatType statType, decimal value)> GetRawStats(bool ignoreZero = default);
    }
}
