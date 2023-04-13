using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly IIntStatWithCurrent Hp = new IntStatWithCurrent(StatType.HP);
        protected readonly IIntStat Atk = new IntStat(StatType.ATK);
        protected readonly IIntStat Def = new IntStat(StatType.DEF);
        protected readonly IDecimalStat Cri = new DecimalStat(StatType.CRI);
        protected readonly IDecimalStat Hit = new DecimalStat(StatType.HIT);
        protected readonly IDecimalStat Spd = new DecimalStat(StatType.SPD);
        protected readonly IIntStat Drv = new IntStat(StatType.DRV);
        protected readonly IIntStat Drr = new IntStat(StatType.DRR);
        protected readonly IntStat Cdmg = new IntStat(StatType.CDMG);

        public IIntStatWithCurrent hp => Hp;
        public IIntStat atk => Atk;
        public IIntStat def => Def;
        public IDecimalStat cri => Cri;
        public IDecimalStat hit => Hit;
        public IDecimalStat spd => Spd;
        public IIntStat drv => Drv;
        public IIntStat drr => Drr;
        public IIntStat cdmg => Cdmg;
        public int HP => Hp.Value;
        public int ATK => Atk.Value;
        public int DEF => Def.Value;
        public int CRI => Cri.ValueAsInt;
        public int HIT => Hit.ValueAsInt;
        public int SPD => Spd.ValueAsInt;
        public int DRV => Drv.Value;
        public int DRR => Drr.Value;
        public int CDMG => Cdmg.Value;

        public bool HasHP => HP > 0;
        public bool HasATK => ATK > 0;
        public bool HasDEF => DEF > 0;
        public bool HasCRI => CRI > 0;
        public bool HasHIT => HIT > 0;
        public bool HasSPD => SPD > 0;
        public bool HasDRV => DRV > 0;
        public bool HasDRR => DRR > 0;
        public bool HasCDMG => CDMG > 0;

        public int CurrentHP
        {
            get => Hp.Current;
            set => Hp.SetCurrent(value);
        }

        public Stats()
        {
        }

        public Stats(IStats value)
        {
            Hp = (IntStatWithCurrent) value.hp.Clone();
            Atk = (IntStat)value.atk.Clone();
            Def = (IntStat)value.def.Clone();
            Cri = (DecimalStat)value.cri.Clone();
            Hit = (DecimalStat)value.hit.Clone();
            Spd = (DecimalStat)value.spd.Clone();
            Drv = (IntStat)value.drv.Clone();
            Drr = (IntStat)value.drr.Clone();
            Cdmg = (IntStat)value.cdmg.Clone();
        }

        public void Reset()
        {
            Hp.Reset();
            Atk.Reset();
            Def.Reset();
            Cri.Reset();
            Hit.Reset();
            Spd.Reset();
            Drv.Reset();
            Drr.Reset();
            Cdmg.Reset();
        }

        /// <summary>
        /// statsArray의 모든 능력치의 합으로 초기화한다. 이때, decimal 값을 그대로 더한다.
        /// </summary>
        /// <param name="statsArray"></param>
        public void Set(params Stats[] statsArray)
        {
            Hp.SetValue(statsArray.Sum(stats => stats.Hp.Value));
            Atk.SetValue(statsArray.Sum(stats => stats.Atk.Value));
            Def.SetValue(statsArray.Sum(stats => stats.Def.Value));
            Cri.SetValue(statsArray.Sum(stats => stats.Cri.Value));
            Hit.SetValue(statsArray.Sum(stats => stats.Hit.Value));
            Spd.SetValue(statsArray.Sum(stats => stats.Spd.Value));
            Drv.SetValue(statsArray.Sum(stats => stats.Drv.Value));
            Drr.SetValue(statsArray.Sum(stats => stats.Drr.Value));
            Cdmg.SetValue(statsArray.Sum(stats => stats.Cdmg.Value));
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
                        Hp.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Hp.Value)));
                        break;
                    case StatType.ATK:
                        Atk.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Atk.Value)));
                        break;
                    case StatType.DEF:
                        Def.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Def.Value)));
                        break;
                    case StatType.CRI:
                        Cri.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Cri.Value)));
                        break;
                    case StatType.HIT:
                        Hit.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Hit.Value)));
                        break;
                    case StatType.SPD:
                        Spd.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Spd.Value)));
                        break;
                    case StatType.DRV:
                        Drv.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Drv.Value)));
                        break;
                    case StatType.DRR:
                        Drr.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Drr.Value)));
                        break;
                    case StatType.CDMG:
                        Cdmg.AddValue(statModifier.GetModifiedPart(baseStatsArray.Sum(stats => stats.Cdmg.Value)));
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
            Hp.SetValue(value.HP);
            Atk.SetValue(value.ATK);
            Def.SetValue(value.DEF);
            Cri.SetValue(value.CRI);
            Hit.SetValue(value.HIT);
            Spd.SetValue(value.SPD);
            Drv.SetValue(value.DRV);
            Drr.SetValue(value.DRR);
            Cdmg.SetValue(value.CDMG);
        }

        /// <summary>
        /// Use this only for testing.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetStatForTest(StatType statType, int value)
        {
            switch (statType)
            {
                case StatType.HP:
                    Hp.SetValue(value);
                    break;
                case StatType.ATK:
                    Atk.SetValue(value);
                    break;
                case StatType.DEF:
                    Def.SetValue(value);
                    break;
                case StatType.CRI:
                    Cri.SetValue(value);
                    break;
                case StatType.HIT:
                    Hit.SetValue(value);
                    break;
                case StatType.SPD:
                    Spd.SetValue(value);
                    break;
                case StatType.DRV:
                    Drv.SetValue(value);
                    break;
                case StatType.DRR:
                    Drr.SetValue(value);
                    break;
                case StatType.CDMG:
                    Cdmg.SetValue(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            if (ignoreZero)
            {
                if (HasHP)
                    yield return (StatType.HP, HP);
                if (HasATK)
                    yield return (StatType.ATK, ATK);
                if (HasDEF)
                    yield return (StatType.DEF, DEF);
                if (HasCRI)
                    yield return (StatType.CRI, CRI);
                if (HasHIT)
                    yield return (StatType.HIT, HIT);
                if (HasSPD)
                    yield return (StatType.SPD, SPD);
                if (HasDRV)
                    yield return (StatType.DRV, DRV);
                if (HasDRR)
                    yield return (StatType.DRR, DRR);
                if (HasCDMG)
                    yield return (StatType.CDMG, CDMG);
            }
            else
            {
                yield return (StatType.HP, HP);
                yield return (StatType.ATK, ATK);
                yield return (StatType.DEF, DEF);
                yield return (StatType.CRI, CRI);
                yield return (StatType.HIT, HIT);
                yield return (StatType.SPD, SPD);
                yield return (StatType.DRV, DRV);
                yield return (StatType.DRR, DRR);
                yield return (StatType.CDMG, CDMG);
            }
        }

        public void EqualizeCurrentHPWithHP()
        {
            Hp.EqualizeCurrentWithValue();
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
