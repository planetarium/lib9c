using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    // todo: `Stats`나 `StatModifier`로 대체되어야 함.
    [Serializable]
    public class StatsMap : IBaseAndAdditionalStats, IState
    {
        public int HP => HasHP ? _statMaps[StatType.HP].TotalValueAsInt : 0;

        public decimal HPAsDecimal => HasHP
            ? _statMaps[StatType.HP].Value + _statMaps[StatType.HP].AdditionalValue
            : 0m;
        
        public int ATK => HasATK ? _statMaps[StatType.ATK].TotalValueAsInt : 0;
        
        public decimal ATKAsDecimal => HasATK
            ? _statMaps[StatType.ATK].Value + _statMaps[StatType.ATK].AdditionalValue
            : 0m;
        
        public int DEF => HasDEF ? _statMaps[StatType.DEF].TotalValueAsInt : 0;
        
        public decimal DEFAsDecimal => HasDEF
            ? _statMaps[StatType.DEF].Value + _statMaps[StatType.DEF].AdditionalValue
            : 0m;
        
        public int CRI => HasCRI ? _statMaps[StatType.CRI].TotalValueAsInt : 0;
        
        public decimal CRIAsDecimal => HasCRI
            ? _statMaps[StatType.CRI].TotalValue
            : 0m;
        
        public int HIT => HasHIT ? _statMaps[StatType.HIT].TotalValueAsInt : 0;
        
        public decimal HITAsDecimal => HasHIT
            ? _statMaps[StatType.HIT].Value + _statMaps[StatType.HIT].AdditionalValue
            : 0m;
        
        public int SPD => HasSPD ? _statMaps[StatType.SPD].TotalValueAsInt : 0;
        
        public decimal SPDAsDecimal => HasSPD
            ? _statMaps[StatType.SPD].Value + _statMaps[StatType.SPD].AdditionalValue
            : 0m;
        

        public bool HasHP => _statMaps.ContainsKey(StatType.HP) &&
                             (_statMaps[StatType.HP].HasValue || _statMaps[StatType.HP].HasAdditionalValue);

        public bool HasATK => _statMaps.ContainsKey(StatType.ATK) &&
                              (_statMaps[StatType.ATK].HasValue || _statMaps[StatType.ATK].HasAdditionalValue);

        public bool HasDEF => _statMaps.ContainsKey(StatType.DEF) &&
                              (_statMaps[StatType.DEF].HasValue || _statMaps[StatType.DEF].HasAdditionalValue);

        public bool HasCRI => _statMaps.ContainsKey(StatType.CRI) &&
                              (_statMaps[StatType.CRI].HasValue || _statMaps[StatType.CRI].HasAdditionalValue);

        public bool HasHIT => _statMaps.ContainsKey(StatType.HIT) &&
                              (_statMaps[StatType.HIT].HasValue || _statMaps[StatType.HIT].HasAdditionalValue);

        public bool HasSPD => _statMaps.ContainsKey(StatType.SPD) &&
                              (_statMaps[StatType.SPD].HasValue || _statMaps[StatType.SPD].HasAdditionalValue);

        public int BaseHP => HasBaseHP ? _statMaps[StatType.HP].ValueAsInt : 0;
        public decimal BaseHPAsDecimal => HasBaseHP ? _statMaps[StatType.HP].Value : 0m;
        public int BaseATK => HasBaseATK ? _statMaps[StatType.ATK].ValueAsInt : 0;
        public decimal BaseATKAsDecimal => HasBaseATK ? _statMaps[StatType.ATK].Value : 0m;
        public int BaseDEF => HasBaseDEF ? _statMaps[StatType.DEF].ValueAsInt : 0;
        public decimal BaseDEFAsDecimal => HasBaseDEF ? _statMaps[StatType.DEF].Value : 0m;
        public int BaseCRI => HasBaseCRI ? _statMaps[StatType.CRI].ValueAsInt : 0;
        public decimal BaseCRIAsDecimal => HasBaseCRI ? _statMaps[StatType.CRI].Value : 0m;
        public int BaseHIT => HasBaseHIT ? _statMaps[StatType.HIT].ValueAsInt : 0;
        public decimal BaseHITAsDecimal => HasBaseHIT ? _statMaps[StatType.HIT].Value : 0m;
        public int BaseSPD => HasBaseSPD ? _statMaps[StatType.SPD].ValueAsInt : 0;
        public decimal BaseSPDAsDecimal => HasBaseSPD ? _statMaps[StatType.SPD].Value : 0m;

        public bool HasBaseHP => _statMaps.ContainsKey(StatType.HP) && _statMaps[StatType.HP].HasValue;
        public bool HasBaseATK => _statMaps.ContainsKey(StatType.ATK) && _statMaps[StatType.ATK].HasValue;
        public bool HasBaseDEF => _statMaps.ContainsKey(StatType.DEF) && _statMaps[StatType.DEF].HasValue;
        public bool HasBaseCRI => _statMaps.ContainsKey(StatType.CRI) && _statMaps[StatType.CRI].HasValue;
        public bool HasBaseHIT => _statMaps.ContainsKey(StatType.HIT) && _statMaps[StatType.HIT].HasValue;
        public bool HasBaseSPD => _statMaps.ContainsKey(StatType.SPD) && _statMaps[StatType.SPD].HasValue;

        public int AdditionalHP => HasAdditionalHP ? _statMaps[StatType.HP].AdditionalValueAsInt : 0;
        public decimal AdditionalHPAsDecimal => HasAdditionalHP ? _statMaps[StatType.HP].AdditionalValue : 0m;
        public int AdditionalATK => HasAdditionalATK ? _statMaps[StatType.ATK].AdditionalValueAsInt : 0;
        public decimal AdditionalATKAsDecimal => HasAdditionalATK ? _statMaps[StatType.ATK].AdditionalValue : 0m;
        public int AdditionalDEF => HasAdditionalDEF ? _statMaps[StatType.DEF].AdditionalValueAsInt : 0;
        public decimal AdditionalDEFAsDecimal => HasAdditionalDEF ? _statMaps[StatType.DEF].AdditionalValue : 0m;
        public int AdditionalCRI => HasAdditionalCRI ? _statMaps[StatType.CRI].AdditionalValueAsInt : 0;
        public decimal AdditionalCRIAsDecimal => HasAdditionalCRI ? _statMaps[StatType.CRI].AdditionalValue : 0m;
        public int AdditionalHIT => HasAdditionalHIT ? _statMaps[StatType.HIT].AdditionalValueAsInt : 0;
        public decimal AdditionalHITAsDecimal => HasAdditionalHIT ? _statMaps[StatType.HIT].AdditionalValue : 0m;
        public int AdditionalSPD => HasAdditionalSPD ? _statMaps[StatType.SPD].AdditionalValueAsInt : 0;
        public decimal AdditionalSPDAsDecimal => HasAdditionalSPD ? _statMaps[StatType.SPD].AdditionalValue : 0m;

        public bool HasAdditionalHP => _statMaps.ContainsKey(StatType.HP) && _statMaps[StatType.HP].HasAdditionalValue;

        public bool HasAdditionalATK =>
            _statMaps.ContainsKey(StatType.ATK) && _statMaps[StatType.ATK].HasAdditionalValue;

        public bool HasAdditionalDEF =>
            _statMaps.ContainsKey(StatType.DEF) && _statMaps[StatType.DEF].HasAdditionalValue;

        public bool HasAdditionalCRI =>
            _statMaps.ContainsKey(StatType.CRI) && _statMaps[StatType.CRI].HasAdditionalValue;

        public bool HasAdditionalHIT =>
            _statMaps.ContainsKey(StatType.HIT) && _statMaps[StatType.HIT].HasAdditionalValue;

        public bool HasAdditionalSPD =>
            _statMaps.ContainsKey(StatType.SPD) && _statMaps[StatType.SPD].HasAdditionalValue;

        public bool HasAdditionalStats => HasAdditionalHP ||
                                          HasAdditionalATK ||
                                          HasAdditionalDEF ||
                                          HasAdditionalCRI ||
                                          HasAdditionalHIT ||
                                          HasAdditionalSPD;

        private readonly Dictionary<StatType, StatMapEx> _statMaps =
            new Dictionary<StatType, StatMapEx>(StatTypeComparer.Instance);

        public void Clear()
        {
            _statMaps.Clear();
        }

        protected bool Equals(StatsMap other)
        {
            if (_statMaps is null || other._statMaps is null)
            {
                return false;
            }
 
            var orderedStatMaps = _statMaps.OrderBy(pair => pair.Key, StatTypeComparer.Instance);
            var otherOrderedStatMaps = other._statMaps.OrderBy(pair => pair.Key, StatTypeComparer.Instance);
            return _statMaps.Count == other._statMaps.Count && !orderedStatMaps.Except(otherOrderedStatMaps).Any();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatsMap) obj);
        }

        public override int GetHashCode()
        {
            return _statMaps != null
                ? _statMaps.GetHashCode()
                : 0;
        }

        public void AddStatValue(StatType key, decimal value)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].Value += value;
            PostStatValueChanged(key);
        }

        public void AddStatValue(StatOption statOption) => AddStatValue(statOption.StatType, statOption.statValue);

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].AdditionalValue += additionalValue;
            PostStatValueChanged(key);
        }

        public void AddStatAdditionalValue(StatModifier statModifier)
        {
            AddStatAdditionalValue(statModifier.StatType, statModifier.Value);
        }
        
        public void AddStatAdditionalValue(StatOption statOption)
        {
            AddStatAdditionalValue(statOption.StatType, statOption.statValue);
        }

        public void SetStatValue(StatType key, decimal value)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].Value = value;
            PostStatValueChanged(key);
        }

        public void SetStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMaps.ContainsKey(key))
            {
                _statMaps.Add(key, new StatMapEx(key));
            }

            _statMaps[key].AdditionalValue = additionalValue;
            PostStatValueChanged(key);
        }

        private void PostStatValueChanged(StatType key)
        {
            if (!_statMaps.ContainsKey(key))
            {
                return;
            }

            var statMap = _statMaps[key];
            if (statMap.HasValue || statMap.HasAdditionalValue)
            {
                return;
            }

            _statMaps.Remove(key);
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(
                _statMaps.Select(kv =>
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
#pragma warning restore LAA1002
            {
                _statMaps[StatTypeExtension.Deserialize((Binary) kv.Key)] =
                    new StatMapEx((Dictionary) kv.Value);
            }
        }

        public int GetStat(StatType statType, bool ignoreAdditional = false)
        {
            switch (statType)
            {
                case StatType.HP:
                    return ignoreAdditional ? BaseHP : HP;
                case StatType.ATK:
                    return ignoreAdditional ? BaseATK : ATK;
                case StatType.DEF:
                    return ignoreAdditional ? BaseDEF : DEF;
                case StatType.CRI:
                    return ignoreAdditional ? BaseCRI : CRI;
                case StatType.HIT:
                    return ignoreAdditional ? BaseHIT : HIT;
                case StatType.SPD:
                    return ignoreAdditional ? BaseSPD : SPD;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }
        
        public decimal GetRawStat(StatType statType, bool ignoreAdditional = false)
        {
            switch (statType)
            {
                case StatType.HP:
                    return ignoreAdditional ? BaseHPAsDecimal : HPAsDecimal;
                case StatType.ATK:
                    return ignoreAdditional ? BaseATKAsDecimal : ATKAsDecimal;
                case StatType.DEF:
                    return ignoreAdditional ? BaseDEFAsDecimal : DEFAsDecimal;
                case StatType.CRI:
                    return ignoreAdditional ? BaseCRIAsDecimal : CRIAsDecimal;
                case StatType.HIT:
                    return ignoreAdditional ? BaseHITAsDecimal : HITAsDecimal;
                case StatType.SPD:
                    return ignoreAdditional ? BaseSPDAsDecimal : SPDAsDecimal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }
        
        public decimal GetAdditionalRawStat(StatType statType)
        {
            switch (statType)
            {
                case StatType.HP:
                    return HP - BaseHPAsDecimal;
                case StatType.ATK:
                    return ATK - BaseATKAsDecimal;
                case StatType.DEF:
                    return DEF - BaseDEFAsDecimal;
                case StatType.CRI:
                    return CRIAsDecimal - BaseCRIAsDecimal;
                case StatType.HIT:
                    return HITAsDecimal - BaseHITAsDecimal;
                case StatType.SPD:
                    return SPDAsDecimal - BaseSPDAsDecimal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = default)
        {
            if (ignoreZero)
            {
                if (HasHP)
                {
                    yield return (StatType.HP, HP);
                }

                if (HasATK)
                {
                    yield return (StatType.ATK, ATK);
                }

                if (HasDEF)
                {
                    yield return (StatType.DEF, DEF);
                }

                if (HasCRI)
                {
                    yield return (StatType.CRI, CRI);
                }

                if (HasHIT)
                {
                    yield return (StatType.HIT, HIT);
                }

                if (HasSPD)
                {
                    yield return (StatType.SPD, SPD);
                }
            }
            else
            {
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.CRI, CRI);
                yield return (StatType.HIT, HIT);
                yield return (StatType.SPD, SPD);
            }
        }

        public IEnumerable<(StatType statType, decimal value)> GetRawStats(bool ignoreZero = default)
        {
            if (ignoreZero)
            {
                if (HasHP)
                {
                    yield return (StatType.HP, HPAsDecimal);
                }

                if (HasATK)
                {
                    yield return (StatType.ATK, ATKAsDecimal);
                }

                if (HasDEF)
                {
                    yield return (StatType.DEF, DEFAsDecimal);
                }

                if (HasCRI)
                {
                    yield return (StatType.CRI, CRIAsDecimal);
                }

                if (HasHIT)
                {
                    yield return (StatType.HIT, HITAsDecimal);
                }

                if (HasSPD)
                {
                    yield return (StatType.SPD, SPDAsDecimal);
                }
            }
            else
            {
                yield return (StatType.HP, HPAsDecimal);
                yield return (StatType.ATK, ATKAsDecimal);
                yield return (StatType.DEF, DEFAsDecimal);
                yield return (StatType.CRI, CRIAsDecimal);
                yield return (StatType.HIT, HITAsDecimal);
                yield return (StatType.SPD, SPDAsDecimal);
            }
        }

        public IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = default)
        {
            if (ignoreZero)
            {
                if (HasBaseHP || HasAdditionalHP)
                {
                    yield return (StatType.HP, BaseHP, AdditionalHP);
                }

                if (HasBaseATK || HasAdditionalATK)
                {
                    yield return (StatType.ATK, BaseATK, AdditionalATK);
                }

                if (HasBaseDEF || HasAdditionalDEF)
                {
                    yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                }

                if (HasBaseCRI || HasAdditionalCRI)
                {
                    yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                }

                if (HasBaseHIT || HasAdditionalHIT)
                {
                    yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                }

                if (HasBaseSPD || HasAdditionalSPD)
                {
                    yield return (StatType.SPD, BaseSPD, AdditionalSPD);
                }
            }
            else
            {
                yield return (StatType.HP, BaseHP, AdditionalHP);
                yield return (StatType.ATK, BaseATK, AdditionalATK);
                yield return (StatType.DEF, BaseDEF, AdditionalDEF);
                yield return (StatType.CRI, BaseCRI, AdditionalCRI);
                yield return (StatType.HIT, BaseHIT, AdditionalHIT);
                yield return (StatType.SPD, BaseSPD, AdditionalSPD);
            }
        }
        
        public IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalRawStats(
            bool ignoreZero = default)
        {
            if (ignoreZero)
            {
                if (HasBaseHP || HasAdditionalHP)
                {
                    yield return (StatType.HP, BaseHPAsDecimal, AdditionalHPAsDecimal);
                }

                if (HasBaseATK || HasAdditionalATK)
                {
                    yield return (StatType.ATK, BaseATKAsDecimal, AdditionalATKAsDecimal);
                }

                if (HasBaseDEF || HasAdditionalDEF)
                {
                    yield return (StatType.DEF, BaseDEFAsDecimal, AdditionalDEFAsDecimal);
                }

                if (HasBaseCRI || HasAdditionalCRI)
                {
                    yield return (StatType.CRI, BaseCRIAsDecimal, AdditionalCRIAsDecimal);
                }

                if (HasBaseHIT || HasAdditionalHIT)
                {
                    yield return (StatType.HIT, BaseHITAsDecimal, AdditionalHITAsDecimal);
                }

                if (HasBaseSPD || HasAdditionalSPD)
                {
                    yield return (StatType.SPD, BaseSPDAsDecimal, AdditionalSPDAsDecimal);
                }
            }
            else
            {
                yield return (StatType.HP, BaseHPAsDecimal, AdditionalHPAsDecimal);
                yield return (StatType.ATK, BaseATKAsDecimal, AdditionalATKAsDecimal);
                yield return (StatType.DEF, BaseDEFAsDecimal, AdditionalDEFAsDecimal);
                yield return (StatType.CRI, BaseCRIAsDecimal, AdditionalCRIAsDecimal);
                yield return (StatType.HIT, BaseHITAsDecimal, AdditionalHITAsDecimal);
                yield return (StatType.SPD, BaseSPDAsDecimal, AdditionalSPDAsDecimal);
            }
        }

        public IEnumerable<StatMapEx> GetStats()
        {
            if (HasHP)
            {
                yield return _statMaps[StatType.HP];
            }

            if (HasATK)
            {
                yield return _statMaps[StatType.ATK];
            }

            if (HasDEF)
            {
                yield return _statMaps[StatType.DEF];
            }

            if (HasCRI)
            {
                yield return _statMaps[StatType.CRI];
            }

            if (HasHIT)
            {
                yield return _statMaps[StatType.HIT];
            }

            if (HasSPD)
            {
                yield return _statMaps[StatType.SPD];
            }
        }

        /// <summery>
        /// 추가 스탯이 붙어 있는 스탯맵을 열거형으로 반환합니다.
        /// 이 스탯맵에는 기본 스탯이 포함되어 있기 때문에 구분해서 사용해야 합니다.
        /// </summery>
        public IEnumerable<StatMapEx> GetAdditionalStats()
        {
            if (HasAdditionalHP)
            {
                yield return _statMaps[StatType.HP];
            }

            if (HasAdditionalATK)
            {
                yield return _statMaps[StatType.ATK];
            }

            if (HasAdditionalDEF)
            {
                yield return _statMaps[StatType.DEF];
            }

            if (HasAdditionalCRI)
            {
                yield return _statMaps[StatType.CRI];
            }

            if (HasAdditionalHIT)
            {
                yield return _statMaps[StatType.HIT];
            }

            if (HasAdditionalSPD)
            {
                yield return _statMaps[StatType.SPD];
            }
        }
    }
}
