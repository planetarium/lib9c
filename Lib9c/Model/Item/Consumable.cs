using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Represents consumable items that can be used by characters.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    [Serializable]
    public class Consumable : ItemUsable, ITradableItem
    {
        public Guid TradableId => ItemId;

        public StatType MainStat => Stats.Any() ? Stats[0].StatType : StatType.NONE;

        public List<DecimalStat> Stats { get; set; }

        public Consumable(ConsumableItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
            Stats = data.Stats;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public Consumable(IValue serialized) : base(serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
            }
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
            if (dict.TryGetValue((Text) "stats", out var stats))
            {
                Stats = stats.ToList(i => new DecimalStat((Dictionary) i));
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [baseData..., stats]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for Consumable (base 11 + stats 1 = 12)
            if (list.Count < 12)
            {
                throw new ArgumentException($"Invalid list length for Consumable: expected at least 12, got {list.Count}");
            }

            // base fields (0~10): 11 fields from ItemUsable
            // Consumable fields (11): stats
            Stats = list[11].ToList(i => new DecimalStat((Dictionary) i));
        }

        protected Consumable(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        /// <summary>
        /// Serializes the consumable to List format (new format).
        /// Order: [baseData..., stats]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize() => ((List)base.Serialize())
            .Add(new List((Stats ?? new List<DecimalStat>())
                .OrderBy(i => i.StatType)
                .ThenByDescending(i => i.BaseValue)
                .Select(s => s.SerializeWithoutAdditional())));
    }
}
