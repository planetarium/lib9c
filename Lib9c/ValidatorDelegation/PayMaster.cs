using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.ValidatorDelegation
{
    public static class PayMaster
    {
        public static IWorld Mortgage(
            this IWorld world,
            IActionContext context,
            Address address,
            Address payMaster,
            FungibleAssetValue gasToMortgage)
        {
            world = world
                .SetAccount(
                    Addresses.PayMaster,
                    world.GetAccount(Addresses.PayMaster)
                        .SetState(address, payMaster.Bencoded));

            world = world.TransferAsset(
                context,
                payMaster,
                Addresses.MortgagePool,
                gasToMortgage);

            return world;
        }

        public static IWorld Refund(
            this IWorld world,
            IActionContext context,
            Address address,
            FungibleAssetValue gasToRefund)
        {
            var payMaster = world
                .GetAccountState(Addresses.PayMaster)
                .GetState(address) is Binary serialized
                    ? new Address(serialized)
                    : address;

            world = world.TransferAsset(
                context,
                Addresses.MortgagePool,
                payMaster,
                gasToRefund);

            return world;
        }

    }
}
