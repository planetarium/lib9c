using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyRecipeUnlockedException: Exception
    {
        public AlreadyRecipeUnlockedException()
        {
        }

        public AlreadyRecipeUnlockedException(string msg) : base(msg)
        {
        }

        protected AlreadyRecipeUnlockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
