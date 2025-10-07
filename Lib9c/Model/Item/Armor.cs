using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Lib9c.TableData.Item;

namespace Lib9c.Model.Item
{
    /// <summary>
    /// Represents armor equipment items.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    [Serializable]
    public class Armor : Equipment, ITradableItem
    {
        public Guid TradableId => ItemId;

        public Armor(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex,
            bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex,
            madeWithMimisbrunnrRecipe)
        {
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public Armor(IValue serialized) : base(serialized)
        {
        }

        protected Armor(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
