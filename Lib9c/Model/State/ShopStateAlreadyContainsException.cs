using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Model.State
{
    [Serializable]
    public class ShopStateAlreadyContainsException : Exception
    {
        public ShopStateAlreadyContainsException(string message) : base(message)
        {
        }

        protected ShopStateAlreadyContainsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
