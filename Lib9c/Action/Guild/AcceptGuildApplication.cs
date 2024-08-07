using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    // TODO(GUILD-FEATURE): Enable again when Guild features are enabled.
    // [ActionType(TypeIdentifier)]
    public class AcceptGuildApplication : ActionBase
    {
        public const string TypeIdentifier = "accept_guild_application";

        private const string TargetKey = "t";

        public AcceptGuildApplication() {}

        public AcceptGuildApplication(AgentAddress target)
        {
            Target = target;
        }

        public AgentAddress Target { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetKey, Target.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)TargetKey, out var rawTargetAddress))
            {
                throw new InvalidCastException();
            }

            Target = new AgentAddress(rawTargetAddress);
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);

            var world = context.PreviousState;
            var signer = context.GetAgentAddress();

            return world.AcceptGuildApplication(signer, Target);
        }
    }
}
