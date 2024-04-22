using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// An action for refund gas fee for a transaction.
    /// Should be executed at the beginning of the tx.
    /// </summary>
    public sealed class Refund : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="Refund"/>.
        /// </summary>
        public Refund()
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

            if (context.GasLimit() - context.GasUsed() <= 0)
            {
                return world;
            }

            return world.MintAsset(
                context,
                context.Signer,
                (context.GasLimit() - context.GasUsed()) * realGasPrice);
        }
    }
}
