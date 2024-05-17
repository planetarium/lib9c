using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Charm : Equipment
    {
        public Charm(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex, bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex, madeWithMimisbrunnrRecipe)
        {
        }

        public Charm(Dictionary serialized) : base(serialized)
        {
        }

        protected Charm(SerializationInfo info, StreamingContext _) : base(info, _)
        {
        }
    }
}
