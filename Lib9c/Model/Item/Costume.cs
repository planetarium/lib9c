using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Represents costume items that can be equipped by characters.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    [Serializable]
    public class Costume : ItemBase, INonFungibleItem, IEquippableItem, ITradableItem
    {
        // FIXME: Whether the equipment is equipped or not has no asset value and must be removed from the state.
        public bool equipped = false;
        public string SpineResourcePath { get; set; }

        public Guid ItemId { get; private set; }
        public Guid TradableId => ItemId;
        public Guid NonFungibleId => ItemId;

        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than 0, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        public bool Equipped => equipped;

        private long _requiredBlockIndex;

        public Costume(CostumeItemSheet.Row data, Guid itemId) : base(data)
        {
            SpineResourcePath = data.SpineResourcePath;
            ItemId = itemId;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public Costume(IValue serialized) : base(serialized)
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
            if (dict.TryGetValue((Text) "equipped", out var toEquipped))
            {
                equipped = toEquipped.ToBoolean();
            }
            if (dict.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = spineResourcePath.ToDotnetString();
            }
            if (dict.TryGetValue((Text) "item_id", out var itemId))
            {
                ItemId = itemId.ToGuid();
            }

            if (dict.ContainsKey(RequiredBlockIndexKey))
            {
                RequiredBlockIndex = dict[RequiredBlockIndexKey].ToLong();
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [baseData..., equipped, spineResourcePath, itemId, requiredBlockIndex]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for Costume (base 6 + 4 fields = 10)
            if (list.Count < 10)
            {
                throw new ArgumentException($"Invalid list length for Costume: expected at least 10, got {list.Count}");
            }

            // Always read 10 fields
            // base fields (0~5): version, id, itemType, itemSubType, grade, elementalType
            // Costume fields (6~9): equipped, spineResourcePath, itemId, requiredBlockIndex

            // equipped (index 6)
            equipped = list[6].ToBoolean();

            // spineResourcePath (index 7)
            SpineResourcePath = list[7].ToDotnetString();

            // itemId (index 8)
            ItemId = list[8].ToGuid();

            // requiredBlockIndex (index 9)
            RequiredBlockIndex = list[9].ToLong();
        }

        protected Costume(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        /// <summary>
        /// Serializes the costume to List format (new format).
        /// Order: [baseData..., equipped, spineResourcePath, itemId, requiredBlockIndex]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize()
        {
            var list = ((List)base.Serialize())
                .Add(equipped.Serialize())
                .Add((SpineResourcePath ?? "").Serialize())
                .Add(ItemId.Serialize())
                .Add(RequiredBlockIndex.Serialize());
            return list;
        }

        protected bool Equals(Costume other)
        {
            return base.Equals(other) && equipped == other.equipped && ItemId.Equals(other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Costume) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ equipped.GetHashCode();
                hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                return hashCode;
            }
        }

        public void Equip()
        {
            equipped = true;
        }

        public void Unequip()
        {
            equipped = false;
        }

        public void Update(long blockIndex)
        {
            Unequip();
            RequiredBlockIndex = blockIndex;
        }
    }
}
