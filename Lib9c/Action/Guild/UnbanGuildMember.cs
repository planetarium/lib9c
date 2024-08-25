#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;

namespace Nekoyume.Action.Guild
{
    // TODO(GUILD-FEATURE): Enable again when Guild features are enabled.
    // [ActionType(TypeIdentifier)]
    public class UnbanGuildMember : ActionBase
    {
        public const string TypeIdentifier = "unban_guild_member";

        private const string TargetKey = "t";

        public Address Target { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetKey, Target.Bencoded));

        public UnbanGuildMember() {}

        public UnbanGuildMember(Address target)
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

            Target = new Address(rawTarget);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var signer = context.GetAgentAddress();

            if (world.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            return world.Unban(guildAddress, signer, Target);
        }
    }
}
