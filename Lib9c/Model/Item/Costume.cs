using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase, INonFungibleItem, IEquippableItem, ITradableItem
    {
        // FIXME: Whether the equipment is equipped or not has no asset value and must be removed from the state.
        public bool equipped = false;
        public string SpineResourcePath { get; }

        public Guid ItemId { get; }
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

        public Costume(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "equipped", out var toEquipped))
            {
                equipped = toEquipped.ToBoolean();
            }
            if (serialized.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = (Text) spineResourcePath;
            }

            ItemId = serialized[LegacyCostumeItemIdKey].ToGuid();

            if (serialized.ContainsKey(RequiredBlockIndexKey))
            {
                RequiredBlockIndex = serialized[RequiredBlockIndexKey].ToLong();
            }
        }

        protected Costume(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize()
        {
            var innerDictionary = ((Dictionary)base.Serialize())
                .Add("equipped", equipped.Serialize())
                .Add("spine_resource_path", SpineResourcePath.Serialize())
                .Add(LegacyCostumeItemIdKey, ItemId.Serialize());

            return RequiredBlockIndex > 0
                ? innerDictionary.Add(RequiredBlockIndexKey, RequiredBlockIndex.Serialize())
                : innerDictionary;
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
