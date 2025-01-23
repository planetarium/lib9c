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
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Swap : ActionBase
    {
        public const string TypeIdentifier = "swap";

        public Swap()
        {
        }

        public Swap(FungibleAssetValue from, Currency to)
        {
            From = from;
            To = to;
        }

        public FungibleAssetValue From { get; private set; }

        public Currency To { get; private set; }

        public override IValue PlainValue
            => List.Empty
                .Add(From.Serialize())
                .Add(To.Serialize());

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var swapRateSheet = world.GetSheet<SwapRateSheet>();
            var swapPool = new SwapPool(swapRateSheet);
            return swapPool.Swap(world, context, From, To);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
        }
    }
}
