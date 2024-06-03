using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// An action for allocate reward to validators and delegators in previous block.
    /// Should be executed at the beginning of the block.
    /// </summary>
    public sealed class AllocateReward : ActionBase
    {
        /// <summary>
        /// Amount of GovernanceToken to be minted as a block reward.
        /// </summary>
        public const int BlockReward = 5;

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
                Asset.GovernanceToken, Currencies.Mead);

            // 5 GovernanceToken is minted to RewardPool.
            states = states.MintAsset(
                context,
                ReservedAddress.RewardPool,
                BlockReward * Asset.GovernanceToken);
            if (states.GetDPoSState(ReservedAddress.ProposerInfo) is { } proposerInfoState)
            {
                states = AllocateRewardCtrl.Execute(
                    states,
                    context,
                    nativeTokens,
                    context.LastCommit?.Votes,
                    new ProposerInfo(proposerInfoState));
            };

            return states;
        }
    }
}
