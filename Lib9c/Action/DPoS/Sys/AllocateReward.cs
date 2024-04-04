using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.DPoS.Misc;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// An action for allocate reward to validators and delegators in previous block.
    /// Should be executed at the beginning of the block.
    /// </summary>
    public sealed class AllocateReward : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="AllocateReward"/>.
        /// </summary>
        public AllocateReward()
        {
        }

       /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => new Bencodex.Types.Boolean(true);

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            var states = context.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            states = Control.AllocateReward.Execute(
                states,
                context,
                nativeTokens,
                context.LastCommit?.Votes,
                context.Miner);

            return states;
        }
    }
}
