using Bencodex.Types;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatMap : IStats, IBaseAndAdditionalStats, IState
    {
        public DecimalStat this[StatType type]
        {
            get => _statMap[type];
        }

        public long HP => _statMap[StatType.HP].TotalValueAsLong;
        public long ATK => _statMap[StatType.ATK].TotalValueAsLong;
        public long DEF => _statMap[StatType.DEF].TotalValueAsLong;
        public long CRI => _statMap[StatType.CRI].TotalValueAsLong;
        public long HIT => _statMap[StatType.HIT].TotalValueAsLong;
        public long SPD => _statMap[StatType.SPD].TotalValueAsLong;
        public long DRV => _statMap[StatType.DRV].TotalValueAsLong;
        public long DRR => _statMap[StatType.DRR].TotalValueAsLong;
        public long CDMG => _statMap[StatType.CDMG].TotalValueAsLong;
        public long ArmorPenetration => _statMap[StatType.ArmorPenetration].TotalValueAsLong;
        public long Thorn => _statMap[StatType.Thorn].TotalValueAsLong;

        public long BaseHP => _statMap[StatType.HP].BaseValueAsLong;
        public long BaseATK => _statMap[StatType.ATK].BaseValueAsLong;
        public long BaseDEF => _statMap[StatType.DEF].BaseValueAsLong;
        public long BaseCRI => _statMap[StatType.CRI].BaseValueAsLong;
        public long BaseHIT => _statMap[StatType.HIT].BaseValueAsLong;
        public long BaseSPD => _statMap[StatType.SPD].BaseValueAsLong;
        public long BaseDRV => _statMap[StatType.DRV].BaseValueAsLong;
        public long BaseDRR => _statMap[StatType.DRR].BaseValueAsLong;
        public long BaseCDMG => _statMap[StatType.CDMG].BaseValueAsLong;
        public long BaseArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValueAsLong;
        public long BaseThorn => _statMap[StatType.Thorn].BaseValueAsLong;

        public long AdditionalHP => _statMap[StatType.HP].AdditionalValueAsLong;
        public long AdditionalATK => _statMap[StatType.ATK].AdditionalValueAsLong;
        public long AdditionalDEF => _statMap[StatType.DEF].AdditionalValueAsLong;
        public long AdditionalCRI => _statMap[StatType.CRI].AdditionalValueAsLong;
        public long AdditionalHIT => _statMap[StatType.HIT].AdditionalValueAsLong;
        public long AdditionalSPD => _statMap[StatType.SPD].AdditionalValueAsLong;
        public long AdditionalDRV => _statMap[StatType.DRV].AdditionalValueAsLong;
        public long AdditionalDRR => _statMap[StatType.DRR].AdditionalValueAsLong;
        public long AdditionalCDMG => _statMap[StatType.CDMG].AdditionalValueAsLong;
        public long AdditionalArmorPenetration => _statMap[StatType.ArmorPenetration].AdditionalValueAsLong;
        public long AdditionalThorn => _statMap[StatType.Thorn].AdditionalValueAsLong;

        private readonly Dictionary<StatType, DecimalStat> _statMap =
            new Dictionary<StatType, DecimalStat>(StatTypeComparer.Instance)
            {
                { StatType.HP, new DecimalStat(StatType.HP) },
                { StatType.ATK, new DecimalStat(StatType.ATK) },
                { StatType.DEF, new DecimalStat(StatType.DEF) },
                { StatType.CRI, new DecimalStat(StatType.CRI) },
                { StatType.HIT, new DecimalStat(StatType.HIT) },
                { StatType.SPD, new DecimalStat(StatType.SPD) },
                { StatType.DRV, new DecimalStat(StatType.DRV) },
                { StatType.DRR, new DecimalStat(StatType.DRR) },
                { StatType.CDMG, new DecimalStat(StatType.CDMG) },
                { StatType.ArmorPenetration, new DecimalStat(StatType.ArmorPenetration) },
                { StatType.Thorn, new DecimalStat(StatType.Thorn) },
            };

        public StatMap()
        {
        }

        public StatMap(StatMap statMap)
        {
            foreach (var stat in statMap.GetDecimalStats(false))
            {
                _statMap[stat.StatType] = (DecimalStat)stat.Clone();
            }
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public long GetStatAsLong(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValueAsLong;
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValue;
        }

        public long GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.BaseValueAsLong;
        }

        public long GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValueAsLong;
        }

        public IEnumerable<(StatType statType, long value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasTotalValueAsLong)
                    {
                        yield return (statType, stat.TotalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.TotalValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValueAsLong)
                    {
                        yield return (statType, stat.BaseValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasAdditionalValueAsLong)
                    {
                        yield return (statType, stat.AdditionalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.AdditionalValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long baseValue, long additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValueAsLong || stat.HasAdditionalValueAsLong)
                    {
                        yield return (statType, stat.BaseValueAsLong, stat.AdditionalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsLong, stat.AdditionalValueAsLong);
                }
            }
        }

        public IEnumerable<DecimalStat> GetDecimalStats(bool ignoreZero)
        {
            var values = _statMap.OrderBy(x => x.Key).Select(x => x.Value);
            return ignoreZero ?
                values.Where(x => x.HasBaseValueAsLong || x.HasAdditionalValueAsLong) :
                values;
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(
                _statMap
                    .Where(x => x.Value.HasBaseValue || x.Value.HasAdditionalValue)
                    .Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                )
            );
#pragma warning restore LAA1002

        public void Deserialize(Dictionary serialized)
        {
#pragma warning disable LAA1002
            foreach (KeyValuePair<IKey, IValue> kv in serialized)
            {
                _statMap[StatTypeExtension.Deserialize((Binary)kv.Key)]
                    .Deserialize((Dictionary)kv.Value);
            }
#pragma warning restore LAA1002
        }
    }
}
