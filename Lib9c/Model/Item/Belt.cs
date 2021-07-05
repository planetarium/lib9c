using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        public Belt(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }
        
        public Belt(
            int serializedVersion,
            EquipmentItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(serializedVersion, data, id, requiredBlockIndex, requiredCharacterLevel)
        {
        }

        public Belt(Dictionary serialized) : base(serialized)
        {
        }
        
        protected Belt(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
