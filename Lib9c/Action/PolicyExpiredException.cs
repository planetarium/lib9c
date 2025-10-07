using System;
using System.Runtime.Serialization;
using Lib9c.Model.State;
using Libplanet.Common.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class PolicyExpiredException : AdminPermissionException
    {
        public long BlockIndex { get; }

        public PolicyExpiredException(AdminState policy, long blockIndex) : base(policy)
        {
            BlockIndex = blockIndex;
        }

        public PolicyExpiredException(string message) : base(message)
        {
        }

        public PolicyExpiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            BlockIndex = info.GetValue<long>(nameof(BlockIndex));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(BlockIndex), BlockIndex);
        }
    }
}
