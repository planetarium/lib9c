using System;
using Bencodex.Types;
using Lib9c.Model.Guild;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Lib9c.Action.Guild
{
    /// <summary>
    /// An action to claim unbonded assets.
    /// This action can be executed only when the unbonding period is over.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public sealed class ClaimUnbonded : ActionBase
    {
        public const string TypeIdentifier = "claim_unbonded";

        public ClaimUnbonded() { }

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
            var guildDelegator = repository.GetDelegator(context.Signer);
            guildDelegator.ReleaseUnbondings(context.BlockIndex);

            return repository.World;
        }
    }
}
