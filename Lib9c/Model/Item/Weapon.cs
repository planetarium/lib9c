using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Lib9c.TableData.Item;

namespace Lib9c.Model.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        public Weapon(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex,
            bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex,
            madeWithMimisbrunnrRecipe)
        {
        }

        public Weapon(Dictionary serialized) : base(serialized)
        {
        }

        protected Weapon(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
