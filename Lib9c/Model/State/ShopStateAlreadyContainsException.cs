using System;
using System.Runtime.Serialization;

namespace Lib9c.Model.State
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
