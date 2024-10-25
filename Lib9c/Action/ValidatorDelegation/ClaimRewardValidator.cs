using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimRewardValidator : ActionBase
    {
        public const string TypeIdentifier = "claim_reward_validator";

        public ClaimRewardValidator() { }

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

            if (!(guildRepository.GetJoinedGuild(new AgentAddress(context.Signer))
                is GuildAddress guildAddress))
            {
                throw new InvalidOperationException("Signer does not joined guild.");
            }

            var guild = guildRepository.GetGuild(guildAddress);
            if (context.Signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("Signer is not a guild master.");
            }

            var guildValidatorRepository = new GuildValidatorRepository(world, context);
            var guildValidatorDelegator = guildValidatorRepository.GetGuildValidatorDelegator(context.Signer);
            var guildValidatorDelegatee = guildValidatorRepository.GetGuildValidatorDelegatee(guild.ValidatorAddress);
            guildValidatorDelegator.ClaimReward(guildValidatorDelegatee, context.BlockIndex);

            return guildValidatorRepository.World;
        }
    }
}
