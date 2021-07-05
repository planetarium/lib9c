using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using BxDictionary = Bencodex.Types.Dictionary;
using BxText = Bencodex.Types.Text;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : ICloneable, IState, IStats
    {
        protected readonly IntStatWithCurrent hp;
        protected readonly IntStat atk;
        protected readonly IntStat def;
        protected readonly DecimalStat cri;
        protected readonly DecimalStat hit;
        protected readonly DecimalStat spd;

        public int CurrentHP
        {
            get => hp.Current;
            set => hp.SetCurrent(value);
        }

        public Stats()
        {
            hp = new IntStatWithCurrent(StatType.HP);
            atk = new IntStat(StatType.ATK);
            def = new IntStat(StatType.DEF);
            cri = new DecimalStat(StatType.CRI);
            hit = new DecimalStat(StatType.HIT);
            spd = new DecimalStat(StatType.SPD);
        }

        public Stats(Stats value)
        {
            hp = (IntStatWithCurrent) value.hp.Clone();
            atk = (IntStat) value.atk.Clone();
            def = (IntStat) value.def.Clone();
            cri = (DecimalStat) value.cri.Clone();
            hit = (DecimalStat) value.hit.Clone();
            spd = (DecimalStat) value.spd.Clone();
        }

        public Stats(BxDictionary serialized)
        {
            hp = serialized.TryGetValue((BxText) "hp", out var hpValue)
                ? hpValue.ToIntStatWithCurrent()
                : new IntStatWithCurrent(StatType.HP);

            atk = serialized.TryGetValue((BxText) "atk", out var atkValue)
                ? atkValue.ToIntStat()
                : new IntStat(StatType.ATK);

            def = serialized.TryGetValue((BxText) "def", out var defValue)
                ? defValue.ToIntStat()
                : new IntStat(StatType.DEF);

            cri = serialized.TryGetValue((BxText) "cri", out var criValue)
                ? criValue.ToDecimalStat()
                : new DecimalStat(StatType.CRI);

            hit = serialized.TryGetValue((BxText) "hit", out var hitValue)
                ? hitValue.ToDecimalStat()
                : new DecimalStat(StatType.HIT);

            spd = serialized.TryGetValue((BxText) "spd", out var spdValue)
                ? spdValue.ToDecimalStat()
                : new DecimalStat(StatType.SPD);
        }

        #region IState

        public virtual IValue Serialize()
        {
            var result = new BxDictionary();
            if (hp.Value > 0)
            {
                result = result.SetItem("hp", hp.Serialize());
            }

            if (atk.Value > 0)
            {
                result = result.SetItem("atk", atk.Serialize());
            }
            
            if (def.Value > 0)
            {
                result = result.SetItem("def", def.Serialize());
            }
            
            if (cri.Value > 0m)
            {
                result = result.SetItem("cri", cri.Serialize());
            }
            
            if (hit.Value > 0m)
            {
                result = result.SetItem("hit", hit.Serialize());
            }
            
            if (spd.Value > 0m)
            {
                result = result.SetItem("spd", spd.Serialize());
            }

            return result;
        }

        #endregion
        
        #region ICloneable

        public virtual object Clone()
        {
            return new Stats(this);
        }

        #endregion

        #region IStats

        public int HP => hp.Value;
        public decimal HPAsDecimal => hp.Value;
        public int ATK => atk.Value;
        public decimal ATKAsDecimal => atk.Value;
        public int DEF => def.Value;
        public decimal DEFAsDecimal => def.Value;
        public int CRI => cri.ValueAsInt;
        public decimal CRIAsDecimal => cri.Value;
        public int HIT => hit.ValueAsInt;
        public decimal HITAsDecimal => hit.Value;
        public int SPD => spd.ValueAsInt;
        public decimal SPDAsDecimal => spd.Value;

        public bool HasHP => HP > 0;
        public bool HasATK => ATK > 0;
        public bool HasDEF => DEF > 0;
        public bool HasCRI => CRI > 0;
        public bool HasHIT => HIT > 0;
        public bool HasSPD => SPD > 0;

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
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.CRI, CRIAsDecimal);
                yield return (StatType.HIT, HITAsDecimal);
                yield return (StatType.SPD, SPDAsDecimal);
            }
        }

        #endregion

        public void Reset()
        {
            hp.Reset();
            atk.Reset();
            def.Reset();
            cri.Reset();
            hit.Reset();
            spd.Reset();
        }

        /// <summary>
        /// statsArray의 모든 능력치의 합으로 초기화한다. 이때, decimal 값을 그대로 더한다.
        /// </summary>
        /// <param name="statsArray"></param>
        public void Set(params Stats[] statsArray)
        {
            hp.SetValue(statsArray.Sum(stats => stats.hp.Value));
            atk.SetValue(statsArray.Sum(stats => stats.atk.Value));
            def.SetValue(statsArray.Sum(stats => stats.def.Value));
            cri.SetValue(statsArray.Sum(stats => stats.cri.Value));
            hit.SetValue(statsArray.Sum(stats => stats.hit.Value));
            spd.SetValue(statsArray.Sum(stats => stats.spd.Value));
        }

        /// <summary>
        /// baseStatsArray의 모든 능력치의 합을 바탕으로, statModifiers를 통해서 추가되는 부분 만으로 초기화한다. 
        /// </summary>
        /// <param name="statModifiers"></param>
        /// <param name="baseStatsArray"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Set(IEnumerable<StatModifier> statModifiers, params Stats[] baseStatsArray)
        {
            Reset();

            foreach (var statModifier in statModifiers)
            {
                switch (statModifier.StatType)
                {
                    case StatType.HP:
                        hp.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.hp.Value)));
                        break;
                    case StatType.ATK:
                        atk.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.atk.Value)));
                        break;
                    case StatType.DEF:
                        def.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.def.Value)));
                        break;
                    case StatType.CRI:
                        cri.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.cri.Value)));
                        break;
                    case StatType.HIT:
                        hit.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.hit.Value)));
                        break;
                    case StatType.SPD:
                        spd.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.spd.Value)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// value 값 그대로 초기화한다.
        /// </summary>
        /// <param name="value"></param>
        public void Set(StatsMap value)
        {
            hp.SetValue(value.HP);
            atk.SetValue(value.ATK);
            def.SetValue(value.DEF);
            cri.SetValue(value.CRI);
            hit.SetValue(value.HIT);
            spd.SetValue(value.SPD);
        }

        public void EqualizeCurrentHPWithHP()
        {
            hp.EqualizeCurrentWithValue();
        }
    }
}
