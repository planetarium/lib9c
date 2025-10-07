using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Model.State;

namespace Lib9c.Model.Stat
{
    /// <summary>
    /// Represents a single character statistic with base and additional values.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    ///
    /// <para>
    /// Serialization Format:
    /// - Dictionary (Legacy): Uses key-value pairs for backward compatibility
    /// - List (New): Uses ordered list for better performance and smaller size
    /// </para>
    ///
    /// <para>
    /// Field Order (List Format):
    /// 1. version - Serialization version number
    /// 2. statType - The type of statistic
    /// 3. baseValue - Base value of the statistic
    /// 4. additionalValue - Additional value of the statistic
    /// </para>
    /// </summary>
    /// <remarks>
    /// This class implements dual serialization support to ensure smooth migration
    /// from the legacy Dictionary format to the new List format. The List format
    /// provides better performance and smaller serialized data size.
    ///
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create decimal stat
    /// var stat = new DecimalStat(StatType.HP, 100, 50);
    ///
    /// // Serialize to List format (new)
    /// var serialized = stat.Serialize(); // Returns List
    ///
    /// // Deserialize from any format
    /// var deserialized = new DecimalStat(serialized); // Supports both Dictionary and List
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class DecimalStat : ICloneable, IState
    {
        // Serialization version for backward compatibility
        private const int SerializationVersion = 1;

        // Field count constants for serialization
        private const int DecimalStatFieldCount = 4; // version + statType + baseValue + additionalValue

        public decimal BaseValue { get; private set; }

        public decimal AdditionalValue { get; private set; }

        public bool HasTotalValueAsLong => HasBaseValueAsLong || HasAdditionalValueAsLong;

        public bool HasBaseValueAsLong => BaseValue > 0;

        public bool HasAdditionalValueAsLong => AdditionalValue > 0;

        public bool HasBaseValue => BaseValue > 0m;

        public bool HasAdditionalValue => AdditionalValue > 0m;

        public StatType StatType;

        public long BaseValueAsLong => (long)BaseValue;

        public long AdditionalValueAsLong => (long)AdditionalValue;

        [Obsolete("For legacy equipments. (Before world 7 patch)")]
        public long TotalValueAsLong => BaseValueAsLong + AdditionalValueAsLong;

        public decimal TotalValue => BaseValue + AdditionalValue;

        public DecimalStat(StatType type, decimal value = 0m, decimal additionalValue = 0m)
        {
            StatType = type;
            BaseValue = value;
            AdditionalValue = additionalValue;
        }

        public virtual void Reset()
        {
            BaseValue = 0;
            AdditionalValue = 0;
        }

        protected DecimalStat(DecimalStat value)
        {
            StatType = value.StatType;
            BaseValue = value.BaseValue;
            AdditionalValue = value.AdditionalValue;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        /// <exception cref="ArgumentNullException">Thrown when serialized is null</exception>
        /// <exception cref="ArgumentException">Thrown when serialized format is not supported</exception>
        public DecimalStat(IValue serialized)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized), "Serialized data cannot be null");
            }

            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported serialization format: {serialized.GetType().Name}. " +
                        $"Expected Dictionary or List, got {serialized.GetType().Name}. " +
                        $"This may indicate corrupted data or an unsupported serialization format.");
            }
        }

        /// <summary>
        /// Constructor for backward compatibility with Dictionary format.
        /// </summary>
        /// <param name="serialized">Dictionary containing serialized data</param>
        public DecimalStat(Dictionary serialized)
        {
            DeserializeFromDictionary(serialized);
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
            StatType = StatTypeExtension.Deserialize((Binary)dict["statType"]);
            BaseValue = dict["value"].ToDecimal();

            // This field is added later.
            if (dict.TryGetValue((Text)"additionalValue", out var additionalValue))
            {
                AdditionalValue = additionalValue.ToDecimal();
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [version, statType, baseValue, additionalValue]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for DecimalStat
            if (list.Count < DecimalStatFieldCount)
            {
                var fieldNames = string.Join(", ", GetFieldNames());
                throw new ArgumentException(
                    $"Invalid list length for {GetType().Name}: expected at least {DecimalStatFieldCount}, got {list.Count}. " +
                    $"Required fields: {fieldNames}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // Always read DecimalStatFieldCount fields
            // version (index 0)
            var version = ((Integer)list[0]).Value;
            if (version != SerializationVersion)
            {
                throw new ArgumentException(
                    $"Unsupported serialization version: {version}. " +
                    $"Expected {SerializationVersion}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // statType (index 1)
            StatType = StatTypeExtension.Deserialize((Binary)list[1]);

            // baseValue (index 2)
            BaseValue = list[2].ToDecimal();

            // additionalValue (index 3)
            AdditionalValue = list[3].ToDecimal();
        }

        public void SetBaseValue(decimal value)
        {
            BaseValue = value;
        }

        public void AddBaseValue(decimal value)
        {
            SetBaseValue(BaseValue + value);
        }

        public void SetAdditionalValue(decimal value)
        {
            AdditionalValue = value;
        }

        public void AddAdditionalValue(decimal value)
        {
            SetAdditionalValue(AdditionalValue + value);
        }

        public virtual object Clone()
        {
            return new DecimalStat(this);
        }

        protected bool Equals(DecimalStat other)
        {
            return BaseValue == other.BaseValue &&
                AdditionalValue == other.AdditionalValue &&
                StatType == other.StatType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecimalStat)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BaseValue, AdditionalValue, StatType);
        }

        /// <summary>
        /// Serializes the DecimalStat to List format (new format).
        /// Order: [version, statType, baseValue, additionalValue]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public virtual IValue Serialize()
        {
            return List.Empty
                .Add(SerializationVersion)
                .Add(StatType.Serialize())
                .Add(BaseValue.Serialize())
                .Add(AdditionalValue.Serialize());
        }

        public IValue SerializeWithoutAdditional()
        {
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"statType"] = StatType.Serialize(),
                [(Text)"value"] = TotalValue.Serialize(),
            });
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="serialized">Dictionary containing serialized data</param>
        public virtual void Deserialize(Dictionary serialized)
        {
            DeserializeFromDictionary(serialized);
        }

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        private static string[] GetFieldNames()
        {
            return new[] { "version", "statType", "baseValue", "additionalValue" };
        }
    }
}
