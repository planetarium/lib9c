using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimGuildUnbonded : ActionBase
    {
        public const string TypeIdentifier = "claim_guild_unbonded";

        public ClaimGuildUnbonded() { }

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
            var validatorDelegator = repository.GetValidatorDelegator(guild.Address);
            validatorDelegator.ReleaseUnbondings(context.BlockIndex);

            return repository.World;
        }
    }
}
