using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// A system action for DPoS that withdraws commission tokens from <see cref="Validator"/>.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class WithdrawValidator : ActionBase
    {
        private const string ActionTypeValue = "withdraw_validator";

        /// <summary>
        /// Creates a new instance of <see cref="WithdrawValidator"/> action.
        /// </summary>
        public WithdrawValidator()
        {
        }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue));

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            // if (ctx.Rehearsal)
            // Rehearsal mode is not implemented
#pragma warning disable LAA1002
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
#pragma warning restore LAA1002

            return states;
        }
    }
}
