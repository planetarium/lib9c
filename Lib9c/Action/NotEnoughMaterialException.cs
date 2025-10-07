using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class NotEnoughMaterialException : InvalidOperationException
    {
        public NotEnoughMaterialException(string s) : base(s)
        {
        }

        protected NotEnoughMaterialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
