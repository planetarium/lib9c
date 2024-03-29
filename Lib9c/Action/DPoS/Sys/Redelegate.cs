using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// A system action for DPoS that <see cref="Redelegate"/> specified <see cref="ShareAmount"/>
    /// of shared tokens to <see cref="DstValidator"/> from <see cref="SrcValidator"/>.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class Redelegate : IAction
    {
        private const string ActionTypeValue = "redelegate";

        /// <summary>
        /// Creates a new instance of <see cref="Redelegate"/> action.
        /// </summary>
        /// <param name="src">The <see cref="dst"/> of the validator that
        /// delegated previously.</param>
        /// <param name="dst">The <see cref="amount"/> of the validator
        /// to be newly delegated.</param>
        /// <param name="amount">The amount of the shared asset to be re-delegated.</param>
        public Redelegate(Address src, Address dst, FungibleAssetValue amount)
        {
            SrcValidator = src;
            DstValidator = dst;
            ShareAmount = amount;
        }

        internal Redelegate()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator that was previously delegated to.
        /// </summary>
        public Address SrcValidator { get; set; }

        /// <summary>
        /// The <see cref="Address"/> of the validator as a destination of moved voting power.
        /// </summary>
        public Address DstValidator { get; set; }

        /// <summary>
        /// The amount of the shared token to move delegation.
        /// </summary>
        public FungibleAssetValue ShareAmount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue))
            .Add("src", SrcValidator.Serialize())
            .Add("dst", DstValidator.Serialize())
            .Add("amount", ShareAmount.Serialize());

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            SrcValidator = dict["src"].ToAddress();
            DstValidator = dict["dst"].ToAddress();
            ShareAmount = dict["amount"].ToFungibleAssetValue();
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            states = RedelegateCtrl.Execute(
                states,
                ctx,
                ctx.Signer,
                SrcValidator,
                DstValidator,
                ShareAmount,
                nativeTokens);

            return states;
        }
    }
}
