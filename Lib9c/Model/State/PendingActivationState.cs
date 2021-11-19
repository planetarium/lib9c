using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using MessagePack;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    [Serializable]
    [MessagePackObject]
    public class PendingActivationState : State, ISerializable
    {
        private static Address BaseAddress = Addresses.PendingActivation;

        [Key(1)]
        public byte[] Nonce { get; }

#pragma warning disable MsgPack003
        [Key(2)]
        public PublicKey PublicKey { get; }
#pragma warning restore MsgPack003

        public PendingActivationState(byte[] nonce, PublicKey publicKey)
            : base (DeriveAddress(nonce, publicKey))
        {
            Nonce = nonce;
            PublicKey = publicKey;
        }

        [SerializationConstructor]
        public PendingActivationState(Address address, byte[] nonce, PublicKey publicKey) : base(address)
        {
            Nonce = nonce;
            PublicKey = publicKey;
        }

        private static Address DeriveAddress(byte[] nonce, PublicKey publicKey)
        {
            return BaseAddress.Derive(nonce.Concat(publicKey.Format(true)).ToArray());
        }

        public PendingActivationState(Dictionary serialized)
            : base(serialized)
        {
            Nonce = (Binary)serialized["nonce"];
            PublicKey = serialized["public_key"].ToPublicKey();
        }

        protected PendingActivationState(SerializationInfo info, StreamingContext context)
            : this((Dictionary) new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "nonce"] = (Binary) Nonce,
                [(Text) "public_key"] = PublicKey.Serialize(),
            };

#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }

        public bool Verify(ActivateAccount action)
        {
            return Verify(action.Signature);
        }

        public bool Verify(byte[] signature)
        {
            return PublicKey.Verify(Nonce, signature);
        }
    }
}
