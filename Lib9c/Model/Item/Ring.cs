using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }
        
        public Ring(
            int serializedVersion,
            EquipmentItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(serializedVersion, data, id, requiredBlockIndex, requiredCharacterLevel)
        {
        }

        public Ring(Dictionary serialized) : base(serialized)
        {
        }
        
        protected Ring(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
