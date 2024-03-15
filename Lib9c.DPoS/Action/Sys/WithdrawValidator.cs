using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Module;

namespace Lib9c.DPoS.Action.Sys
{
    /// <summary>
    /// A system action for DPoS that withdraws commission tokens from <see cref="Validator"/>.
    /// </summary>
    public sealed class WithdrawValidator : IAction
    {
        /// <summary>
        /// Creates a new instance of <see cref="WithdrawValidator"/> action.
        /// </summary>
        public WithdrawValidator()
        {
        }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public IValue PlainValue => Bencodex.Types.Dictionary.Empty;

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            // if (ctx.Rehearsal)
            // Rehearsal mode is not implemented
            foreach (Currency nativeToken in nativeTokens)
            {
                FungibleAssetValue reward = states.GetBalance(
                    AllocateReward.RewardAddress(ctx.Signer), nativeToken);
                if (reward.Sign > 0)
                {
                    states = states.TransferAsset(
                        ctx,
                        AllocateReward.RewardAddress(ctx.Signer),
                        ctx.Signer,
                        reward);
                }
            }

            return states;
        }
    }
}
