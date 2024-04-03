using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS
{
    /// <summary>
    /// A BeginBlock action for DPoS that updates <see cref="ValidatorSet"/>.
    /// </summary>
    public sealed class DPoSBeginBlockAction : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="DPoSBeginBlockAction"/>.
        /// </summary>
        public DPoSBeginBlockAction()
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

            // Allocate reward
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            states = AllocateReward.Execute(
                states,
                context,
                nativeTokens,
                context.LastCommit?.Votes,
                context.Miner);

            return states;
        }
    }
}
