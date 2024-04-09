using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS
{
    /// <summary>
    /// A system action for DPoS that cancels <see cref="Delegate"/> specified
    /// <see cref="ShareAmount"/> of shared tokens to a given <see cref="Validator"/>.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class Undelegate : ActionBase
    {
        private const string ActionTypeValue = "undelegate";

        /// <summary>
        /// Creates a new instance of <see cref="Undelegate"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="amount"/> of the validator
        /// to undelegate tokens.</param>
        /// <param name="amount">The amount of the asset to be undelegated.</param>
        public Undelegate(Address validator, FungibleAssetValue amount)
        {
            Validator = validator;
            ShareAmount = amount;
        }

        public Undelegate()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator to cancel the <see cref="Delegate"/>.
        /// </summary>
        public Address Validator { get; set; }

        /// <summary>
        /// The amount of the asset to be undelegated.
        /// </summary>
        public FungibleAssetValue ShareAmount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue))
            .Add("validator", Validator.Serialize())
            .Add("amount", ShareAmount.Serialize());

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            Validator = dict["validator"].ToAddress();
            ShareAmount = dict["amount"].ToFungibleAssetValue();
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            // if (ctx.Rehearsal)
            // Rehearsal mode is not implemented
            states = UndelegateCtrl.Execute(
                states,
                ctx,
                ctx.Signer,
                Validator,
                ShareAmount,
                nativeTokens);

            return states;
        }
    }
}
