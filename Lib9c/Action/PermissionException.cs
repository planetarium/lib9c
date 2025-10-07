using System;
using System.Runtime.Serialization;
using Lib9c.Model.State;
using Libplanet.Common.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public abstract class AdminPermissionException : Exception
    {
        public AdminState Policy { get; private set; }

        protected AdminPermissionException(AdminState policy)
        {
            Policy = policy;
        }

        protected AdminPermissionException(string message) : base(message)
        {
        }

        protected AdminPermissionException(SerializationInfo info, StreamingContext context)
        {
            Policy = info.GetValue<AdminState>(nameof(Policy));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(Policy), Policy);
        }
    }
}
