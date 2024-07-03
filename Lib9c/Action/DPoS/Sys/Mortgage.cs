using System;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Exceptions;
using Nekoyume.Module;

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
            if (!(context.TxId is { } txId &&
                  context.Txs.Any(tx => tx.Id.Equals(txId))))
            {
                return state;
            }

            var tx = context.Txs.First(tx => tx.Id.Equals(context.TxId));
            switch ((tx.MaxGasPrice, tx.GasLimit))
            {
                case (null, null):
                    return state.SetTxGasInfo(null, null);
                case (not null, null): case (null, not null):
                    throw new ArgumentException("Pairity of null-ness of price and gas limit must match.");
                case ({ Sign: > 0 } realGasPrice, { } gasLimit):
                    if (gasLimit < 0)
                    {
                        throw new GasLimitNegativeException();
                    }
                    state = state.SetTxGasInfo(realGasPrice, gasLimit);

                    var balance = state.GetBalance(context.Signer, Currencies.Mead);
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
                default:
                    // Gas price is negative.
                    throw new ArgumentException("Sign of gas price must be positive.");
            }
        }
    }
}
