using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Model.State
{
    [Serializable]
    public class FailedToUnregisterInShopStateException : Exception
    {
        public FailedToUnregisterInShopStateException(string message) : base(message)
        {
        }

        protected FailedToUnregisterInShopStateException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
