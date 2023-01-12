using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Lib9c.TableData.Item;

namespace Lib9c.Model.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex,
            bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex, madeWithMimisbrunnrRecipe)
        {
        }

        public Armor(Dictionary serialized) : base(serialized)
        {
        }

        protected Armor(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
