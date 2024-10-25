using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Nekoyume.Action.ValidatorDelegation
{
    /// <summary>
    /// An action for reward for a transaction.
    /// Should be executed at the beginning of the tx.
    /// </summary>
    public sealed class Reward : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="Refund"/>.
        /// </summary>
        public Reward()
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
            var world = context.PreviousState;
            if (context.MaxGasPrice is not { Sign: > 0 } realGasPrice)
            {
                return world;
            }

            if (GasTracer.GasUsed <= 0)
            {
                return world;
            }

            var gasMortgaged = world.GetBalance(Addresses.MortgagePool, realGasPrice.Currency);
            var gasUsedPrice = realGasPrice * GasTracer.GasUsed;
            var gasToTransfer = gasMortgaged < gasUsedPrice ? gasMortgaged : gasUsedPrice;

            if (gasToTransfer.Sign <= 0)
            {
                return world;
            }

            return world.TransferAsset(
                    context, Addresses.MortgagePool, Addresses.GasPool, gasToTransfer);
        }
    }
}
