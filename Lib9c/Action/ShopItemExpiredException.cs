using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ShopItemExpiredException : Exception
    {
        public ShopItemExpiredException()
        {
        }

        public ShopItemExpiredException(string msg) : base(msg)
        {
        }

        protected ShopItemExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
