using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using BxDictionary = Bencodex.Types.Dictionary;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class SkillOption : IItemOption
    {
        public readonly Skill.Skill Skill;

        public int Grade { get; }

        public ItemOptionType Type => ItemOptionType.Skill;

        public SkillOption(int grade, Skill.Skill skill)
        {
            Skill = skill;
            Grade = grade;
            // SkillRow = skillRow;
            // this.power = power;
            // this.chance = chance;
        }

        public SkillOption(IValue serialized)
        {
            try
            {
                var dict = (BxDictionary) serialized;
                Grade = dict["grade"].ToInteger();
                Skill = SkillFactory.Deserialize((BxDictionary) dict["skill"]);
                // SkillRow = SkillSheet.Row.Deserialize((BxDictionary) dict["skill-row"]);
                // power = dict["power"].ToInteger();
                // chance = dict["chance"].ToInteger();
            }
            catch (Exception e) when (e is InvalidCastException || e is KeyNotFoundException)
            {
                Log.Error("{Exception}", e.ToString());
                throw;
            }
        }

        public static SkillOption Deserialize(IValue serialized) => new SkillOption(serialized);

        public IValue Serialize() => BxDictionary.Empty
            .SetItem("grade", Grade.Serialize())
            .SetItem("skill", Skill.Serialize());

        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="OverflowException"></exception>
        public void Enhance(decimal ratio)
        {
            if (ratio < 0 || ratio > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ratio),
                    ratio,
                    $"{nameof(ratio)} greater than or equal to 0 and less than or equal to 1");
            }

            try
            {
                Skill.Update(
                    decimal.ToInt32(Skill.Chance * (1 + ratio)),
                    decimal.ToInt32(Skill.Power * (1 + ratio)));
            }
            catch (OverflowException e)
            {
                Log.Error("{Exception}", e.ToString());
                throw;
            }
        }
    }
}
