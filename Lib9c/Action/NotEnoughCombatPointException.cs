using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    public class NotEnoughCombatPointException : InvalidOperationException
    {
        public NotEnoughCombatPointException(string s) : base(s)
        {
        }

        protected NotEnoughCombatPointException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
