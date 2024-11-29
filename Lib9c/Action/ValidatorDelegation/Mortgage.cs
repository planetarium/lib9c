using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.ValidatorDelegation
{
    /// <summary>
    /// An action for mortgage gas fee for a transaction.
    /// Should be executed at the beginning of the tx.
    /// </summary>
    public sealed class Mortgage : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="Mortgage"/>.
        /// </summary>
        public Mortgage()
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
            var state = context.PreviousState;
            if (context.MaxGasPrice is not { Sign: > 0 } realGasPrice)
            {
                return state;
            }

            var gasOwned = state.GetBalance(context.Signer, realGasPrice.Currency);
            var gasRequired = realGasPrice * GasTracer.GasAvailable;
            var gasToMortgage = gasOwned < gasRequired ? gasOwned : gasRequired;
            if (gasOwned < gasRequired)
            {
                // var msg =
                //     $"The account {context.Signer}'s balance of {realGasPrice.Currency} is " +
                //     "insufficient to pay gas fee: " +
                //     $"{gasOwned} < {realGasPrice * gasLimit}.";
                GasTracer.CancelTrace();
                // throw new InsufficientBalanceException(msg, context.Signer, gasOwned);
            }

            if (gasToMortgage.Sign > 0)
            {
                return state.TransferAsset(
                    context, context.Signer, Addresses.MortgagePool, gasToMortgage);
            }

            return state;
        }
    }
}
