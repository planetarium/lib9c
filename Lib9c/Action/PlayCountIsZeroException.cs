using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class PlayCountIsZeroException : Exception
    {
        public PlayCountIsZeroException(string msg) : base(msg)
        {
        }

        public PlayCountIsZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
