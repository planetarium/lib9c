#nullable enable

using System;
using System.Runtime.Serialization;
using Libplanet.Common.Serialization;
using Libplanet.Crypto;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidMinterException : Exception
    {
        private Address _signer;

        public InvalidMinterException()
        {
        }

        public InvalidMinterException(Address signer)
        {
            _signer = signer;
        }

        public InvalidMinterException(string message) : base(message)
        {
        }

        protected InvalidMinterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _signer = new Address(info.GetValue<byte[]>(nameof(_signer)));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(_signer), _signer.ToByteArray());
        }
    }
}
