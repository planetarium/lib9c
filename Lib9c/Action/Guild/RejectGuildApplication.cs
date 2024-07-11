using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public class RejectGuildApplication : ActionBase
    {
        public const string TypeIdentifier = "reject_guild_application";

        public RejectGuildApplication() {}

        public RejectGuildApplication(AgentAddress target)
        {
            Target = target;
        }

        public AgentAddress Target { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add("target", Target.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)"target", out var rawTargetAddress))
            {
                throw new InvalidCastException();
            }

            Target = new AgentAddress(rawTargetAddress);
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var signer = context.GetAgentAddress();

            return world.RejectGuildApplication(signer, Target);
        }
    }
}
