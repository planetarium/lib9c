using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
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
