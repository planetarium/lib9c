using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughRankException : InvalidOperationException
    {
        public NotEnoughRankException()
        {
        }

        public NotEnoughRankException(SerializationInfo info, StreamingContext context) : base(
            info, context)
        {
        }
    }
}
