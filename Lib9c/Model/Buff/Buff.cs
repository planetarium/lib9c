using System;
using System.Collections.Generic;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Buff : ICloneable
    {
        public int originalDuration;
        public int remainedDuration;

        public BuffSheet.Row RowData { get; }

        protected Buff(BuffSheet.Row row)
        {
            originalDuration = remainedDuration = row.Duration;
            RowData = row;
        }

        protected Buff(Buff value)
        {
            originalDuration = value.RowData.Duration;
            remainedDuration = value.remainedDuration;
            RowData = value.RowData;
        }

        public int Use(StageCharacter stageCharacter)
        {
            var value = 0;
            switch (RowData.StatModifier.StatType)
            {
                case StatType.HP:
                    value = stageCharacter.HP;
                    break;
                case StatType.ATK:
                    value = stageCharacter.ATK;
                    break;
                case StatType.DEF:
                    value = stageCharacter.DEF;
                    break;
                case StatType.CRI:
                    value = stageCharacter.CRI;
                    break;
                case StatType.HIT:
                    value = stageCharacter.HIT;
                    break;
                case StatType.SPD:
                    value = stageCharacter.SPD;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return RowData.StatModifier.GetModifiedAll(value);
        }

        public IEnumerable<StageCharacter> GetTarget(StageCharacter caster)
        {
            return RowData.TargetType.GetTarget(caster);
        }

        public object Clone()
        {
            return new Buff(this);
        }
    }
}
