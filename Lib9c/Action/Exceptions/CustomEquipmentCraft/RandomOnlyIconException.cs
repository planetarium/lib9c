using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.CustomEquipmentCraft
{
    public class RandomOnlyIconException : Exception
    {
        public RandomOnlyIconException(string s) : base(s)
        {
        }

        public RandomOnlyIconException(int iconId)
            : base($"{iconId} only can be made with random selection.")
        {
        }

        protected RandomOnlyIconException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
