using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Lib9c.DPoS.Util;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;

namespace Lib9c.DPoS.Action.Sys
{
    /// <summary>
    /// A system action for DPoS that withdraws reward tokens from given <see cref="Validator"/>.
    /// </summary>
    public sealed class WithdrawDelegator : IAction
    {
        /// <summary>
        /// Creates a new instance of <see cref="WithdrawDelegator"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="Address"/> of the validator
        /// from which to withdraw the tokens.</param>
        public WithdrawDelegator(Address validator)
        {
            Validator = validator;
        }

        internal WithdrawDelegator()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator to withdraw.
        /// </summary>
        public Address Validator { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public IValue PlainValue => Validator.Serialize();

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public void LoadPlainValue(IValue plainValue)
        {
            Validator = plainValue.ToAddress();
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
            states = DelegateCtrl.Distribute(
                states,
                ctx,
                nativeTokens,
                Delegation.DeriveAddress(ctx.Signer, Validator));

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
