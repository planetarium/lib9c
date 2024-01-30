using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly StatMap _statMap;

        public long HP => _statMap[StatType.HP].BaseValueAsLong;
        public long ATK => _statMap[StatType.ATK].BaseValueAsLong;
        public long DEF => _statMap[StatType.DEF].BaseValueAsLong;
        public long CRI => _statMap[StatType.CRI].BaseValueAsLong;
        public long HIT => _statMap[StatType.HIT].BaseValueAsLong;
        public long SPD => _statMap[StatType.SPD].BaseValueAsLong;
        public long DRV => _statMap[StatType.DRV].BaseValueAsLong;
        public long DRR => _statMap[StatType.DRR].BaseValueAsLong;
        public long CDMG => _statMap[StatType.CDMG].BaseValueAsLong;
        public long ArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValueAsLong;
        public long Thorn => _statMap[StatType.Thorn].BaseValueAsLong;

        protected readonly HashSet<StatType> LegacyDecimalStatTypes =
            new HashSet<StatType>{ StatType.CRI, StatType.HIT, StatType.SPD };

        public Stats()
        {
            _statMap = new StatMap();
        }

        public Stats(Stats value)
        {
            _statMap = new StatMap(value._statMap);
        }

        public void Reset()
        {
            _statMap.Reset();
        }

        public void Set(StatMap statMap, params Stats[] statsArray)
        {
            foreach (var stat in statMap.GetDecimalStats(false))
            {
                if (!LegacyDecimalStatTypes.Contains(stat.StatType))
                {
                    long sum = 0;
                    foreach (var s in statsArray)
                    {
                        sum += s.GetStatAsLong(stat.StatType);
                    }
                    stat.SetBaseValue(sum);
                }
                else
                {
                    decimal sum = 0;
                    foreach (var s in statsArray)
                    {
                        sum += s.GetStat(stat.StatType);
                    }
                    stat.SetBaseValue(sum);
                }
            }
        }

        public void Set(StatsMap value)
        {
            foreach (var stat in value.GetDecimalStats(true))
            {
                var statType = stat.StatType;
                var sum = value.GetStat(statType);
                _statMap[statType].SetBaseValue(sum);
            }
        }

        public void Modify(IEnumerable<StatModifier> statModifiers)
        {
            foreach (var statModifier in statModifiers)
            {
                var statType = statModifier.StatType;
                if (!LegacyDecimalStatTypes.Contains(statType))
                {
                    var originalStatValue = GetStatAsLong(statType);
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
                else
                {
                    var originalStatValue = GetStat(statType);
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
            }
        }

        public void Set(IEnumerable<StatModifier> statModifiers, params Stats[] baseStats)
        {
            Reset();

            foreach (var statModifier in statModifiers)
            {
                var statType = statModifier.StatType;
                if (!LegacyDecimalStatTypes.Contains(statType))
                {
                    long originalStatValue = 0;
                    foreach (var stats in baseStats)
                    {
                        originalStatValue += stats.GetStatAsLong(statType);
                    }

                    long result = (long)statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
                else
                {
                    decimal originalStatValue = 0;
                    foreach (var stats in baseStats)
                    {
                        originalStatValue += stats.GetStat(statType);
                    }

                    decimal result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
            }
        }

        public long GetStatAsLong(StatType statType)
        {
            return _statMap.GetStatAsLong(statType);
        }

        public decimal GetStat(StatType statType)
        {
            return _statMap.GetStat(statType);
        }

        /// <summary>
        /// Use this only for testing.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetStatForTest(StatType statType, decimal value)
        {
            _statMap[statType].SetBaseValue(value);
        }

        public IEnumerable<(StatType statType, long value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
