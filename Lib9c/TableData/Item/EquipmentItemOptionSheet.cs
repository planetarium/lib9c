using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Stat and skill options that crafting can add to equipment.
    /// </summary>
    public class EquipmentItemOptionSheet : Sheet<int, EquipmentItemOptionSheet.Row>
    {
        /// <summary>
        /// One option and the ranges its values are drawn from.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => Id;
            /// <summary>
            /// The option id.
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// The stat this option adds, or <see cref="StatType.NONE"/> for a skill option.
            /// </summary>
            public StatType StatType { get; private set; }
            /// <summary>
            /// The inclusive lower bound of the stat value.
            /// </summary>
            public long StatMin { get; private set; }
            /// <summary>
            /// The inclusive upper bound of the stat value.
            /// </summary>
            public long StatMax { get; private set; }
            /// <summary>
            /// The skill this option grants, or 0 for a stat option.
            /// </summary>
            public int SkillId { get; private set; }
            public long SkillDamageMin { get; private set; }
            /// <summary>
            /// The inclusive upper bound of the skill power.
            /// </summary>
            public long SkillDamageMax { get; private set; }
            /// <summary>
            /// The inclusive lower bound of the skill chance.
            /// </summary>
            public int SkillChanceMin { get; private set; }
            /// <summary>
            /// The inclusive upper bound of the skill chance.
            /// </summary>
            public int SkillChanceMax { get; private set; }
            /// <summary>
            /// The inclusive lower bound of the stat damage ratio.
            /// </summary>
            public int StatDamageRatioMin { get; private set; }
            /// <summary>
            /// The inclusive upper bound of the stat damage ratio.
            /// </summary>
            public int StatDamageRatioMax { get; private set; }
            /// <summary>
            /// The stat the stat damage ratio refers to.
            /// </summary>
            public StatType ReferencedStatType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StatType = string.IsNullOrEmpty(fields[1])
                    ? StatType.NONE
                    : (StatType) Enum.Parse(typeof(StatType), fields[1]);
                StatMin = ParseLong(fields[2], 0);
                StatMax = ParseLong(fields[3], 0);
                SkillId = ParseInt(fields[4], 0);
                SkillDamageMin = ParseLong(fields[5], 0);
                SkillDamageMax = ParseLong(fields[6], 0);
                SkillChanceMin = ParseInt(fields[7], 0);
                SkillChanceMax = ParseInt(fields[8], 0);

                if (fields.Count > 9)
                {
                    StatDamageRatioMin = ParseInt(fields[9], 0);
                    StatDamageRatioMax = ParseInt(fields[10], 0);
                    ReferencedStatType = string.IsNullOrEmpty(fields[11])
                        ? StatType.NONE
                        : (StatType)Enum.Parse(typeof(StatType), fields[11]);
                }
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public EquipmentItemOptionSheet() : base(nameof(EquipmentItemOptionSheet))
        {
        }
    }
}
