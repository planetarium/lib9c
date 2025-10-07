using System;
using Bencodex.Types;
using Lib9c.Model.Guild;
using Lib9c.ValidatorDelegation;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Lib9c.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimGuildReward : ActionBase
    {
        public const string TypeIdentifier = "claim_guild_reward";

        public ClaimGuildReward() { }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Null.Value);

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Null)
            {
                throw new InvalidCastException();
            };
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var guildRepository = new GuildRepository(world, context);

            var guildParticipant = guildRepository.GetGuildParticipant(context.Signer);
            var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
            if (context.Signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("Signer is not a guild master.");
            }

            var repository = new ValidatorRepository(guildRepository);
            var validatorDelegatee = repository.GetDelegatee(guild.ValidatorAddress);
            var validatorDelegator = repository.GetDelegator(guild.Address);
            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repository.World;
        }
    }
}
