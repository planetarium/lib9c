using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatsMap : IStats, IBaseAndAdditionalStats, IState
    {
        public long HP => GetStat(StatType.HP);
        public long ATK => GetStat(StatType.ATK);
        public long DEF => GetStat(StatType.DEF);
        public long CRI => GetStat(StatType.CRI);
        public long HIT => GetStat(StatType.HIT);
        public long SPD => GetStat(StatType.SPD);
        public long DRV => GetStat(StatType.DRV);
        public long DRR => GetStat(StatType.DRR);
        public long CDMG => GetStat(StatType.CDMG);
        public long ArmorPenetration => GetStat(StatType.ArmorPenetration);
        public long Thorn => GetStat(StatType.Thorn);

        public long BaseHP => GetBaseStat(StatType.HP);
        public long BaseATK => GetBaseStat(StatType.ATK);
        public long BaseDEF => GetBaseStat(StatType.DEF);
        public long BaseCRI => GetBaseStat(StatType.CRI);
        public long BaseHIT => GetBaseStat(StatType.HIT);
        public long BaseSPD => GetBaseStat(StatType.SPD);
        public long BaseDRV => GetBaseStat(StatType.DRV);
        public long BaseDRR => GetBaseStat(StatType.DRR);
        public long BaseCDMG => GetBaseStat(StatType.CDMG);
        public long BaseArmorPenetration => GetBaseStat(StatType.ArmorPenetration);
        public long BaseThorn => GetBaseStat(StatType.Thorn);

        public long AdditionalHP => GetAdditionalStat(StatType.HP);
        public long AdditionalATK => GetAdditionalStat(StatType.ATK);
        public long AdditionalDEF => GetAdditionalStat(StatType.DEF);
        public long AdditionalCRI => GetAdditionalStat(StatType.CRI);
        public long AdditionalHIT => GetAdditionalStat(StatType.HIT);
        public long AdditionalSPD => GetAdditionalStat(StatType.SPD);
        public long AdditionalDRV => GetAdditionalStat(StatType.DRV);
        public long AdditionalDRR => GetAdditionalStat(StatType.DRR);
        public long AdditionalCDMG => GetAdditionalStat(StatType.CDMG);
        public long AdditionalArmorPenetration => GetAdditionalStat(StatType.ArmorPenetration);
        public long AdditionalThorn => GetAdditionalStat(StatType.Thorn);

        private readonly StatMap _statMap = new StatMap();

        protected bool Equals(StatsMap other)
        {
            return Equals(_statMap, other._statMap);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatsMap)obj);
        }

        public override int GetHashCode()
        {
            return _statMap != null ? _statMap.GetHashCode() : 0;
        }

        public long GetStat(StatType statType)
        {
            return _statMap.GetStatAsLong(statType);
        }

        public long GetBaseStat(StatType statType)
        {
            return _statMap.GetBaseStat(statType);
        }

        public long GetAdditionalStat(StatType statType)
        {
            return _statMap.GetAdditionalStat(statType);
        }

        public void AddStatValue(StatType key, decimal value)
        {
            _statMap[key].AddBaseValue(value);
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            _statMap[key].AddAdditionalValue(additionalValue);
        }

        public void AddStatAdditionalValue(StatModifier statModifier)
        {
            AddStatAdditionalValue(statModifier.StatType, statModifier.Value);
        }

        public void SetStatAdditionalValue(StatType key, decimal additionalValue)
        {
            _statMap[key].SetAdditionalValue(additionalValue);
        }

        public IValue Serialize() => _statMap.Serialize();

        public void Deserialize(Dictionary serialized) => _statMap.Deserialize(serialized);

        public IEnumerable<(StatType statType, long value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, long baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            return _statMap.GetBaseStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, long additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            return _statMap.GetAdditionalStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, long baseValue, long additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            return _statMap.GetBaseAndAdditionalStats(ignoreZero);
        }

        public IEnumerable<DecimalStat> GetDecimalStats(bool ignoreZero)
        {
            return _statMap.GetDecimalStats(ignoreZero);
        }

        public IEnumerable<DecimalStat> GetAdditionalStats()
        {
            return _statMap.GetDecimalStats(true)
                .Where(x => x.HasAdditionalValue);
        }
    }
}
