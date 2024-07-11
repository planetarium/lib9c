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
            context.UseGas(1);
            var world = context.PreviousState;

            // NOTE: GuildMaster address and GuildAddress are the same with signer address.
            var signer = context.GetAgentAddress();

            if (world.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not have a guild.");
            }

            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (guild.GuildMasterAddress == Target)
            {
                throw new InvalidOperationException("The guild master cannot be banned.");
            }

            world = world.Ban(guildAddress, Target);

            if (world.GetJoinedGuild(Target) == guildAddress)
            {
                world = world.LeaveGuild(Target);
            }

            if (world.TryGetGuildApplication(Target, out var guildApplication) && guildApplication.GuildAddress == guildAddress)
            {
                world = world.RejectGuildApplication(signer, Target);
            }

            return world;
        }
    }
}
