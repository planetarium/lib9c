using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }
        
        public Armor(
            int serializedVersion,
            EquipmentItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(serializedVersion, data, id, requiredBlockIndex, requiredCharacterLevel)
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
