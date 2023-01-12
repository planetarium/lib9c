using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class DuplicateEquipmentException: InvalidOperationException
    {
        public DuplicateEquipmentException(string s) : base(s)
        {
        }

        protected DuplicateEquipmentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
