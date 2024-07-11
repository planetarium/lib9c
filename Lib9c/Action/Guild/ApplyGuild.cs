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
    public class ApplyGuild : ActionBase
    {
        public const string TypeIdentifier = "apply_guild";

        public ApplyGuild() {}

        public ApplyGuild(GuildAddress guildAddress)
        {
            GuildAddress = guildAddress;
        }

        public GuildAddress GuildAddress { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add("guild_address", GuildAddress.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)"guild_address", out var rawGuildAddress))
            {
                throw new InvalidCastException();
            }

            GuildAddress = new GuildAddress(rawGuildAddress);
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var signer = context.GetAgentAddress();

            if (world.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer is already joined in a guild.");
            }

            // NOTE: Check there is such guild.
            _ = world.GetGuild(GuildAddress);

            if (world.IsBanned(GuildAddress, signer))
            {
                throw new InvalidOperationException("The signer is banned from the guild.");
            }

            // TODO: Do something related with ConsensusPower delegation.

            return world.ApplyGuild(signer, GuildAddress);
        }
    }
}
