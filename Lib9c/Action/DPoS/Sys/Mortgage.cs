using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Nekoyume.Action.DPoS.Sys
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

            var balance = state.GetBalance(context.Signer, realGasPrice.Currency);
            var gasLimit = GasTracer.GasAvailable;
            if (balance < realGasPrice * gasLimit)
            {
                var msg =
                    $"The account {context.Signer}'s balance of {realGasPrice.Currency} is " +
                    "insufficient to pay gas fee: " +
                    $"{balance} < {realGasPrice * gasLimit}.";
                throw new InsufficientBalanceException(msg, context.Signer, balance);
            }

            return state.TransferAsset(
                context,
                context.Signer,
                Addresses.MeadPool,
                realGasPrice * gasLimit);
        }
    }
}
