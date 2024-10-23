using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimRewardGuild : ActionBase
    {
        public const string TypeIdentifier = "claim_reward_guild";

        public ClaimRewardGuild() { }

        public ClaimRewardGuild(Address guildAddress)
        {
            GuildAddress = guildAddress;
        }

        public Address GuildAddress { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(GuildAddress.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not List values)
            {
                throw new InvalidCastException();
            }

            GuildAddress = new Address(values[0]);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var guildRepository = new GuildRepository(world, context);

            var guildParticipant = guildRepository.GetGuildParticipant(context.Signer);
            if (!(guildRepository.GetJoinedGuild(new AgentAddress(context.Signer))
                is GuildAddress guildAddress))
            {
                throw new InvalidOperationException("Signer does not joind guild.");
            }

            var guild = guildRepository.GetGuild(guildAddress);

            guildParticipant.ClaimReward(guild, context.BlockIndex);

            return guildRepository.World;
        }
    }
}
