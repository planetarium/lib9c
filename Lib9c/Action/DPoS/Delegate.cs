using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS
{
    /// <summary>
    /// A system action for DPoS that <see cref="Delegate"/> specified <see cref="Amount"/>
    /// of tokens to a given <see cref="Validator"/>.
    /// </summary>
    [ActionType(ActionTypeValue)]
    public sealed class Delegate : ActionBase
    {
        private const string ActionTypeValue = "delegate";

        /// <summary>
        /// Creates a new instance of <see cref="Delegate"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="Address"/> of the validator
        /// to delegate tokens.</param>
        /// <param name="amount">The amount of the asset to be delegated.</param>
        public Delegate(Address validator, long amount)
        {
            Validator = validator;
            Amount = amount;
        }

        public Delegate()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator to <see cref="Delegate"/>.
        /// </summary>
        public Address Validator { get; set; }

        public long Amount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("type_id", new Text(ActionTypeValue))
            .Add("validator", Validator.Serialize())
            .Add("amount", Amount);

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            Validator = dict["validator"].ToAddress();
            Amount = (Integer)dict["amount"];
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var governanceToken = states.GetGoldCurrency();
            var nativeTokens = states.GetNativeTokens();
            states = DelegateCtrl.Execute(
                states,
                context,
                context.Signer,
                Validator,
                governanceToken * Amount,
                nativeTokens);

            return states;
        }
    }
}
