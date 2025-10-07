using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.TableData.Item;
using Libplanet.Common;

namespace Lib9c.Model.Item
{
    /// <summary>
    /// Represents a material item that can be used for crafting and other purposes.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    ///
    /// <para>
    /// Field Order (List Format):
    /// Base fields (0~5): version, id, itemType, itemSubType, grade, elementalType
    /// Material fields (6): itemId
    /// </para>
    ///
    /// <para>
    /// Material Properties:
    /// - ItemId: Unique identifier for the material item
    /// </para>
    /// </summary>
    /// <remarks>
    /// Material items are fungible items used primarily for crafting other items.
    /// They support both Binary and Text formats for ItemId during deserialization
    /// to maintain compatibility with different serialization formats.
    ///
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create material
    /// var material = new Material(materialRow);
    ///
    /// // Use in crafting
    /// var requiredMaterials = new Dictionary&lt;HashDigest&lt;SHA256&gt;, int&gt;
    /// {
    ///     { material.ItemId, 5 } // 5 of this material
    /// };
    ///
    /// // Check material properties
    /// var grade = material.Grade;
    /// var itemType = material.ItemType;
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class Material : ItemBase, ISerializable, IFungibleItem
    {
        // Field count constants for serialization
        private const int MATERIAL_FIELD_COUNT = BaseFieldCount + 1; // base + itemId

        public HashDigest<SHA256> ItemId { get; private set; }

        public HashDigest<SHA256> FungibleId => ItemId;

        public Material(MaterialItemSheet.Row data) : base(data)
        {
            ItemId = data.ItemId;
        }

        public Material(Material other) : base(other)
        {
            ItemId = other.ItemId;
        }

        /// <summary>
        /// Sets the ItemId from various serialized formats.
        /// </summary>
        /// <param name="itemIdValue">Serialized ItemId value</param>
        /// <exception cref="ArgumentException">Thrown when ItemId format is not supported</exception>
        private void SetItemId(IValue itemIdValue)
        {
            if (itemIdValue is Binary binary)
            {
                ItemId = binary.ToItemId();
            }
            else if (itemIdValue is Text text)
            {
                // Convert Text format to Binary for ItemId
                ItemId = text.ToItemId();
            }
            else
            {
                throw new ArgumentException(
                    $"Unsupported ItemId format: {itemIdValue.GetType().Name}. " +
                    $"Expected Binary or Text, got {itemIdValue.GetType().Name}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public Material(IValue serialized) : base(serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    if (dict.TryGetValue((Text) "item_id", out var itemId))
                    {
                        SetItemId(itemId);
                    }
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
            if (dict.TryGetValue((Text) "item_id", out var itemId))
            {
                // Note: ItemId is a read-only property and should be set in the constructor.
                // For deserialization, we need to handle this differently.
                // For now, we'll skip this as it should be set from the base constructor.
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [baseData..., itemId]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for Material
            if (list.Count < MATERIAL_FIELD_COUNT)
            {
                var fieldNames = string.Join(", ", GetFieldNames());
                throw new ArgumentException(
                    $"Invalid list length for {GetType().Name}: expected at least {MATERIAL_FIELD_COUNT}, got {list.Count}. " +
                    $"Required fields: {fieldNames}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // Always read MATERIAL_FIELD_COUNT fields
            // base fields (0~5): version, id, itemType, itemSubType, grade, elementalType
            // Material fields (6): itemId

            // itemId (index 6)
            SetItemId(list[6]);
        }

        protected Material(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(Material other)
        {
            return base.Equals(other) &&
                   ItemId.Equals(other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Material) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Serializes the material to List format (new format).
        /// Order: [baseData..., itemId]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize() => ((List)base.Serialize())
            .Add(ItemId.Serialize());

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        protected override string[] GetFieldNames()
        {
            return base.GetFieldNames().Concat(new[]
            {
                "itemId"
            }).ToArray();
        }

        public override string ToString()
        {
            return base.ToString() +
                   $", {nameof(ItemId)}: {ItemId}";
        }
    }
}
