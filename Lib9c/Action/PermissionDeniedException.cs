using System;
using System.Runtime.Serialization;
using Lib9c.Model.State;
using Libplanet.Common.Serialization;
using Libplanet.Crypto;

namespace Lib9c.Action
{
    [Serializable]
    public class PermissionDeniedException : AdminPermissionException
    {
        public Address Signer { get; }

        public PermissionDeniedException(AdminState policy, Address signer) : base(policy)
        {
            Signer = signer;
        }

        public PermissionDeniedException(string message) : base(message)
        {
        }

        public PermissionDeniedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Signer = info.GetValue<Address>(nameof(Signer));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(Signer), Signer);
        }
    }
}
