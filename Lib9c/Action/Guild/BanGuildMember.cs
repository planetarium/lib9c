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
    [ActionType(TypeIdentifier)]
    public class BanGuildMember : ActionBase
    {
        public const string TypeIdentifier = "ban_guild_member";

        private const string TargetKey = "t";

        public AgentAddress Target { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetKey, Target.Bencoded));

        public BanGuildMember() {}

        public BanGuildMember(AgentAddress target)
        {
            Target = target;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)TargetKey, out var rawTarget))
            {
                throw new InvalidCastException();
            }

            Target = new AgentAddress(rawTarget);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);
            var signer = context.GetAgentAddress();

            repository.Ban(signer, Target);

            return repository.World;
        }
    }
}
