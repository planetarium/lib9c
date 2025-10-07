using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class EquipmentLevelExceededException : InvalidOperationException
    {
        public EquipmentLevelExceededException(string s) : base(s)
        {
        }

        protected EquipmentLevelExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
