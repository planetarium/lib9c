using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class DuplicateMaterialException : InvalidOperationException
    {
        public DuplicateMaterialException(string s) : base(s)
        {
        }

        protected DuplicateMaterialException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
