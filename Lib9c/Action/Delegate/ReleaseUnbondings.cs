using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Module.Delegation;

namespace Nekoyume.Action.Delegate
{
    public class ReleaseUnbondings : ActionBase
    {
        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
            throw new InvalidOperationException("Policy action shouldn't be serialized.");
        }

        public override IWorld Execute(IActionContext context)
        {
            if(!context.IsPolicyAction)
            {
                throw new InvalidOperationException(
                    "This action must be called when it is a policy action.");
            }

            var world = context.PreviousState;

            return world.Release(context);
        }
    }
}
