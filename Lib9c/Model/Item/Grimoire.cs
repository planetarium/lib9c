using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Grimoire : Equipment
    {
        public Grimoire(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex, bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex, madeWithMimisbrunnrRecipe)
        {
        }

        public Grimoire(Dictionary serialized) : base(serialized)
        {
        }

        protected Grimoire(SerializationInfo info, StreamingContext _) : base(info, _)
        {
        }
    }
}
