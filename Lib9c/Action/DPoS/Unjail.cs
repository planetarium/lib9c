using System;
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
    /// A system action for DPoS that <see cref="Unjail"/> given <see cref="Validator"/>.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class Unjail : ActionBase
    {
        private const string ActionTypeValue = "unjail";

        /// <summary>
        /// Creates a new instance of <see cref="Unjail"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="Address"/> of the validator
        /// to unjail.</param>
        public Unjail(Address validator)
        {
            Validator = validator;
        }

        public Unjail()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator to <see cref="Unjail"/>.
        /// </summary>
        public Address Validator { get; set; }

        public FungibleAssetValue Amount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue))
            .Add("validator", Validator.Serialize());

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            Validator = dict["validator"].ToAddress();
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var validatorAddress = Model.Validator.DeriveAddress(ctx.Signer);
            if (!Validator.Equals(validatorAddress))
            {
                throw new InvalidOperationException("Signer is not the validator.");
            }

            var states = ctx.PreviousState;
            states = ValidatorCtrl.Unjail(
                states,
                validatorAddress: Validator);

            return states;
        }
    }
}
