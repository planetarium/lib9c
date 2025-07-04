using System;
using System.Globalization;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Base class for all items in the game.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    [Serializable]
    public abstract class ItemBase : IItem
    {
        public const int SerializationVersion = 2;

        // Field count constants for serialization
        protected const int BaseFieldCount = 6; // version, id, itemType, itemSubType, grade, elementalType

        protected static readonly Codec Codec = new Codec();

        private int _id;
        private int _grade;
        private ItemType _itemType;
        private ItemSubType _itemSubType;
        private ElementalType _elementalType;
        private Text? _serializedId;
        private Text? _serializedGrade;
        private Text? _serializedItemType;
        private Text? _serializedItemSubType;
        private Text? _serializedElementalType;
        private Integer? _serializedVersion;

        public int Id
        {
            get
            {
                if (_serializedId is { })
                {
                    _id = _serializedId.ToInteger();
                    _serializedId = null;
                }

                return _id;
            }
        }

        public int Grade
        {
            get
            {
                if (_serializedGrade is { })
                {
                    _grade = _serializedGrade.ToInteger();
                    _serializedGrade = null;
                }

                return _grade;
            }
        }

        public ItemType ItemType
        {
            get
            {
                if (_serializedItemType is { })
                {
                    _itemType = (ItemType)_serializedItemType.ToInteger();
                    _serializedItemType = null;
                }

                return _itemType;
            }
        }

        public ItemSubType ItemSubType
        {
            get
            {
                if (_serializedItemSubType is { })
                {
                    _itemSubType = (ItemSubType)_serializedItemSubType.ToInteger();
                    _serializedItemSubType = null;
                }

                return _itemSubType;
            }
        }

        public ElementalType ElementalType
        {
            get
            {
                if (_serializedElementalType is { })
                {
                    _elementalType = (ElementalType)_serializedElementalType.ToInteger();
                    _serializedElementalType = null;
                }

                return _elementalType;
            }

            set => _elementalType = value;
        }

        protected ItemBase(ItemSheet.Row data)
        {
            _id = data.Id;
            _grade = data.Grade;
            _itemType = data.ItemType;
            _itemSubType = data.ItemSubType;
            _elementalType = data.ElementalType;
            _serializedVersion = (Integer)SerializationVersion;
        }

        protected ItemBase(ItemBase other)
        {
            _id = other.Id;
            _grade = other.Grade;
            _itemType = other.ItemType;
            _itemSubType = other.ItemSubType;
            _elementalType = other.ElementalType;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        protected ItemBase(IValue serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    // Check if we have enough fields for Item (base 6)
                    if (list.Count < BaseFieldCount)
                    {
                        var fieldNames = string.Join(", ", GetFieldNames());
                        throw new ArgumentException($"Invalid list length for ItemBase: expected at least 6, got {list.Count}. Fields: {fieldNames}");
                    }
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
            if (dict.TryGetValue((Text) "id", out var id))
            {
                _serializedId = (Text) id;
            }
            if (dict.TryGetValue((Text) "grade", out var grade))
            {
                _serializedGrade = (Text) grade;
            }
            if (dict.TryGetValue((Text) "item_type", out var type))
            {
                _serializedItemType = (Text)((int)type.ToEnum<ItemType>()).Serialize();
            }
            if (dict.TryGetValue((Text) "item_sub_type", out var subType))
            {
                _serializedItemSubType = (Text)((int)subType.ToEnum<ItemSubType>()).Serialize();
            }
            if (dict.TryGetValue((Text) "elemental_type", out var elementalType))
            {
                _serializedElementalType = (Text)((int)elementalType.ToEnum<ElementalType>()).Serialize();
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// </summary>
        /// <param name="list">List containing serialized data in order: [version, id, itemType, itemSubType, grade, elementalType]</param>
        private void DeserializeFromList(List list)
        {
            // Always read 6 fields (length check removed)
            _serializedVersion = (Integer) list[0];
            _serializedId = (Text) list[1];
            _serializedItemType = (Text) list[2];
            _serializedItemSubType = (Text) list[3];
            _serializedGrade = (Text) list[4];
            _serializedElementalType = (Text) list[5];
        }

        protected ItemBase(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("serialized", Codec.Encode(Serialize()));
        }

        protected bool Equals(ItemBase other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ItemBase) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        /// <summary>
        /// Serializes the item to List format (new format).
        /// Order: [version, id, itemType, itemSubType, grade, elementalType]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public virtual IValue Serialize()
        {
            return List.Empty
                .Add(SerializationVersion)
                .Add(_serializedId ?? Id.Serialize())
                .Add(_serializedItemType ?? ((int)ItemType).ToString(CultureInfo.InvariantCulture).Serialize())
                .Add(_serializedItemSubType ?? ((int)ItemSubType).ToString(CultureInfo.InvariantCulture).Serialize())
                .Add(_serializedGrade ?? Grade.Serialize())
                .Add(_serializedElementalType ?? ((int)ElementalType).ToString(CultureInfo.InvariantCulture).Serialize());
        }

        public override string ToString()
        {
            return
                $"{nameof(Id)}: {Id}" +
                $", {nameof(Grade)}: {Grade}" +
                $", {nameof(ItemType)}: {ItemType}" +
                $", {nameof(ItemSubType)}: {ItemSubType}" +
                $", {nameof(ElementalType)}: {ElementalType}";
        }

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        protected virtual string[] GetFieldNames()
        {
            return new[]
            {
                "version",
                "id",
                "itemType",
                "itemSubType",
                "grade",
                "elementalType"
            };
        }
    }
}
