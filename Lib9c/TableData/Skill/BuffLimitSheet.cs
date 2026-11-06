using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    using OperationType = StatModifier.OperationType;

    /// <summary>
    /// Upper limits of stat buffs and lower limits of stat debuffs, per buff group.
    /// </summary>
    public class BuffLimitSheet : Sheet<int, BuffLimitSheet.Row>
    {
        /// <summary>
        /// The limit of one buff group.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => GroupId;

            /// <summary>
            /// The <see cref="StatBuffSheet.Row.GroupId"/> this limit applies to.
            /// </summary>
            public int GroupId { get; set; }

            /// <summary>
            /// How <see cref="Value"/> limits the buff: a percentage of the stat or an absolute amount.
            /// </summary>
            public StatModifier.OperationType StatModifier { get; private set; }

            /// <summary>
            /// The limit. An absolute (<see cref="OperationType.Add"/>) limit follows stat magnitudes,
            /// which pass <see cref="int.MaxValue"/> in hard worlds, so it is a <see cref="long"/>.
            /// </summary>
            public long Value { get; set; }

            /// <inheritdoc />
            public override void Set(IReadOnlyList<string> fields)
            {
                GroupId = ParseInt(fields[0]);
                StatModifier = (OperationType)Enum.Parse(typeof(OperationType), fields[1]);
                Value = ParseLong(fields[2]);
            }

            /// <summary>
            /// The limit as a <see cref="Model.Stat.StatModifier"/> on <paramref name="statType"/>.
            /// </summary>
            /// <param name="statType">The stat the limited buff modifies.</param>
            /// <returns>The modifier that applies the limit.</returns>
            public StatModifier GetModifier(StatType statType)
            {
                return new StatModifier(statType, StatModifier, Value);
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public BuffLimitSheet() : base(nameof(BuffLimitSheet))
        {
        }
    }
}
