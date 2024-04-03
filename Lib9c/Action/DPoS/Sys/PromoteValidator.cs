using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// A system action for DPoS that promotes non-validator node to a validator.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class PromoteValidator : ActionBase
    {
        private const string ActionTypeValue = "promote_validator";

        /// <summary>
        /// Create a new instance of <see cref="PromoteValidator"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="PublicKey"/> of the target
        /// to promote validator.</param>
        /// <param name="amount">The amount of the asset to be initialize delegation.</param>
        public PromoteValidator(PublicKey validator, FungibleAssetValue amount)
        {
            Validator = validator;
            Amount = amount;
        }

        public PromoteValidator()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
            // FIXME: do not fill ambiguous validator field.
            // Suggestion: https://gist.github.com/riemannulus/7405e0d361364c6afa0ab433905ae81c
            Validator = new PrivateKey().PublicKey;
        }

        /// <summary>
        /// The <see cref="PublicKey"/> of the target promoting to a validator.
        /// </summary>
        public PublicKey Validator { get; set; }

        /// <summary>
        /// The amount of the asset to be initially delegated.
        /// </summary>
        public FungibleAssetValue Amount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue))
            .Add("validator", Validator.Serialize())
            .Add("amount", Amount.Serialize());

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            Validator = dict["validator"].ToPublicKey();
            Amount = dict["amount"].ToFungibleAssetValue();
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            if (!ctx.Signer.Equals(Validator.Address))
            {
                throw new PublicKeyAddressMatchingException(ctx.Signer, Validator);
            }

            var states = ctx.PreviousState;
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            states = ValidatorCtrl.Create(
                states,
                ctx,
                ctx.Signer,
                Validator,
                Amount,
                nativeTokens);

            return states;
        }
    }
}
