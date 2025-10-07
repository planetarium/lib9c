using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.CustomEquipmentCraft
{
    [Serializable]
    public class NotEnoughRelationshipException : Exception
    {
        public NotEnoughRelationshipException(string s) : base(s)
        {
        }

        protected NotEnoughRelationshipException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
