using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidTransferUnactivatedRecipientException : Exception
    {
        public InvalidTransferUnactivatedRecipientException(
            Address sender,
            Address recipient)
        {
            Sender = sender;
            Recipient = recipient;
        }

        public InvalidTransferUnactivatedRecipientException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
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

        public Address Sender { get; }

        public Address Recipient { get; }
    }
}
