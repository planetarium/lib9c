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
    [ActionType(TypeIdentifier)]
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
            context.UseGas(1);
            var world = context.PreviousState;

            // NOTE: GuildMaster address and GuildAddress are the same with signer address.
            var agentAddress = context.GetAgentAddress();

            if (world.GetJoinedGuild(agentAddress) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != agentAddress)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (!world.IsBanned(guildAddress, Target))
            {
                throw new InvalidOperationException("The target is not banned.");
            }

            return world.Unban(guildAddress, Target);
        }
    }
}
