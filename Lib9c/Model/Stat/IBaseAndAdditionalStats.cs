using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IBaseAndAdditionalStats : IStats
    {
        int BaseHP { get; }
        decimal BaseHPAsDecimal { get; }
        int BaseATK { get; }
        decimal BaseATKAsDecimal { get; }
        int BaseDEF { get; }
        decimal BaseDEFAsDecimal { get; }
        int BaseCRI { get; }
        decimal BaseCRIAsDecimal { get; }
        int BaseHIT { get; }
        decimal BaseHITAsDecimal { get; }
        int BaseSPD { get; }
        decimal BaseSPDAsDecimal { get; }

        bool HasBaseHP { get; }
        bool HasBaseATK { get; }
        bool HasBaseDEF { get; }
        bool HasBaseCRI { get; }
        bool HasBaseHIT { get; }
        bool HasBaseSPD { get; }

        int AdditionalHP { get; }
        decimal AdditionalHPAsDecimal { get; }
        int AdditionalATK { get; }
        decimal AdditionalATKAsDecimal { get; }
        int AdditionalDEF { get; }
        decimal AdditionalDEFAsDecimal { get; }
        int AdditionalCRI { get; }
        decimal AdditionalCRIAsDecimal { get; }
        int AdditionalHIT { get; }
        decimal AdditionalHITAsDecimal { get; }
        int AdditionalSPD { get; }
        decimal AdditionalSPDAsDecimal { get; }

        bool HasAdditionalHP { get; }
        bool HasAdditionalATK { get; }
        bool HasAdditionalDEF { get; }
        bool HasAdditionalCRI { get; }
        bool HasAdditionalHIT { get; }
        bool HasAdditionalSPD { get; }
        bool HasAdditionalStats { get; }

        IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = default);
        IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalRawStats(bool ignoreZero = default);
    }
}
