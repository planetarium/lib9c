using Libplanet.Crypto;
using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidTransferRecipientException : Exception
    {
        public Address Sender { get; }

        public Address Recipient { get; }

        public InvalidTransferRecipientException(Address sender, Address recipient)
        {
            Sender = sender;
            Recipient = recipient;
        }

        public InvalidTransferRecipientException(string message) : base(message)
        {
        }

        public InvalidTransferRecipientException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
            Sender = (Address)info.GetValue(nameof(Sender), typeof(Address));
            Recipient = (Address)info.GetValue(nameof(Recipient), typeof(Address));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Sender), Sender);
            info.AddValue(nameof(Recipient), Recipient);
        }
    }
}
