using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    // TODO(GUILD-FEATURE): Enable again when Guild features are enabled.
    // [ActionType(TypeIdentifier)]
    public class RejectGuildApplication : ActionBase
    {
        public const string TypeIdentifier = "reject_guild_application";

        private const string TargetKey = "t";

        public RejectGuildApplication() {}

        public RejectGuildApplication(AgentAddress target)
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
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);
            var signer = context.GetAgentAddress();

            repository.RejectGuildApplication(signer, Target);
            return repository.World;
        }
    }
}
