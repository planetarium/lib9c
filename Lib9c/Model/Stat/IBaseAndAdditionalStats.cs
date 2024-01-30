using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IBaseAndAdditionalStats
    {
        long BaseHP { get; }
        long BaseATK { get; }
        long BaseDEF { get; }
        long BaseCRI { get; }
        long BaseHIT { get; }
        long BaseSPD { get; }
        long BaseDRV { get; }
        long BaseDRR { get; }
        long BaseCDMG { get; }
        long BaseArmorPenetration { get; }
        long BaseThorn { get; }

        long AdditionalHP { get; }
        long AdditionalATK { get; }
        long AdditionalDEF { get; }
        long AdditionalCRI { get; }
        long AdditionalHIT { get; }
        long AdditionalSPD { get; }
        long AdditionalDRV { get; }
        long AdditionalDRR { get; }
        long AdditionalCDMG { get; }
        long AdditionalArmorPenetration { get; }
        long AdditionalThorn { get; }

        IEnumerable<(StatType statType, long baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, long additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, long baseValue, long additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
