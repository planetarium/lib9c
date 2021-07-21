using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Serilog;
using BxBinary = Bencodex.Types.Binary;
using BxDictionary = Bencodex.Types.Dictionary;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class StatOption : IItemOption
    {
        public const string StatTypeKey = "t";
        public readonly StatType StatType;
        
        public const string statValueKey = "v";
        public decimal statValue;

        public const string GradeKey = "g";
        public int Grade { get; }

        public ItemOptionType Type => ItemOptionType.Stat;

        public StatOption(int grade, StatType statType, decimal statValue = default)
        {
            Grade = grade;
            StatType = statType;
            this.statValue = statValue;
        }

        public StatOption(int grade, DecimalStat decimalStat) : this(grade, decimalStat.Type, decimalStat.Value)
        {
        }

        public StatOption(int grade, StatMap statMap) : this(grade, statMap.StatType, statMap.Value)
        {
        }

        public StatOption(IValue serialized)
        {
            try
            {
                var dict = (BxDictionary) serialized;
                StatType = StatTypeExtension.Deserialize((BxBinary) dict[StatTypeKey]);
                statValue = dict[statValueKey].ToDecimal();
                Grade = dict[GradeKey].ToInteger();
            }
            catch (Exception e) when (e is InvalidCastException || e is KeyNotFoundException)
            {
                Log.Error("{Exception}", e.ToString());
                throw;
            }
        }

        public static StatOption Deserialize(IValue serialized) => new StatOption(serialized);

        public static bool TryDeserialize(IValue serialized, out StatOption statOption)
        {
            statOption = serialized is BxDictionary
                ? Deserialize(serialized)
                : null;

            return (statOption is null);
        }

        public IValue Serialize() => BxDictionary.Empty
            .SetItem(GradeKey, Grade.Serialize())
            .SetItem(statValueKey, statValue.Serialize())
            .SetItem(StatTypeKey, StatType.Serialize());

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
                statValue *= 1m + ratio;
            }
            catch (OverflowException e)
            {
                Log.Error("{Exception}", e.ToString());
                throw;
            }
        }
    }
}
