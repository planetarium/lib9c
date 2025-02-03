using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Model.Swap;
using Nekoyume.Module;
using Nekoyume.TableData.Swap;

namespace Nekoyume.Action
{
    /// <summary>
    /// Swap action swaps one currency to another currency.
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Swap : ActionBase
    {
        /// <summary>
        /// The type identifier for the <see cref="Swap"/> class.
        /// </summary>
        public const string TypeIdentifier = "swap";

        /// <summary>
        /// Initializes a new instance of the <see cref="Swap"/> class.
        /// </summary>
        public Swap()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Swap"/> class.
        /// </summary>
        /// <param name="from">
        /// The <see cref="FungibleAssetValue"/> to swap from.
        /// </param>
        /// <param name="to">
        /// The <see cref="Currency"/> to swap to.
        /// </param>
        public Swap(FungibleAssetValue from, Currency to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// The <see cref="FungibleAssetValue"/> to swap from.
        /// </summary>
        public FungibleAssetValue From { get; private set; }

        /// <summary>
        /// The <see cref="Currency"/> to swap to.
        /// </summary>
        public Currency To { get; private set; }

        /// <inheritdoc/>
        public override IValue PlainValue
            => List.Empty
                .Add(From.Serialize())
                .Add(To.Serialize());

        /// <inheritdoc/>
        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var swapRateSheet = world.GetSheet<SwapRateSheet>();
            var swapPool = new SwapPool(swapRateSheet);
            return swapPool.Swap(world, context, From, To);
        }

        /// <inheritdoc/>
        public override void LoadPlainValue(IValue plainValue)
        {
            if (!(plainValue is List list))
            {
                throw new ArgumentException("Invalid plain value.");
            }

            if (list.Count != 2)
            {
                throw new ArgumentException("Invalid list count.");
            }

            From = new FungibleAssetValue(list[0]);
            To = new Currency(list[1]);
        }
    }
}
