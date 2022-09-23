using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class AvatarIndexAlreadyUsedException : InvalidOperationException
    {
        public AvatarIndexAlreadyUsedException(string s) : base(s)
        {
        }

        protected AvatarIndexAlreadyUsedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
