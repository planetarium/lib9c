using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidMaterialException : InvalidOperationException
    {
        public InvalidMaterialException(string s) : base(s)
        {
        }

        protected InvalidMaterialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
