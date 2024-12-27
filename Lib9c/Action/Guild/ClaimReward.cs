using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public sealed class ClaimReward : ActionBase
    {
        public const string TypeIdentifier = "claim_reward";

        public ClaimReward() { }

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
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);

            var guildParticipant = repository.GetGuildParticipant(context.Signer);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            var guildDelegatee = repository.GetDelegatee(guild.ValidatorAddress);
            var guildDelegator = repository.GetDelegator(context.Signer);
            guildDelegator.ClaimReward(guildDelegatee, context.BlockIndex);

            return repository.World;
        }
    }
}
