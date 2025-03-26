using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.ValidatorDelegation
{
    /// <summary>
    /// PayMaster is a contract that manages the proxy of mortgage and refund of gas.
    /// </summary>
    public static class PayMaster
    {
        /// <summary>
        /// Mortgage gas with the PayMaster.
        /// </summary>
        /// <param name="world">
        /// The <see cref="IWorld"/> where the mortgage happen.
        /// </param>
        /// <param name="context">
        /// The <see cref="IActionContext"/> where the mortgage happen.
        /// </param>
        /// <param name="address">
        /// The <see cref="Address"/> of the user to mortgage gas.
        /// </param>
        /// <param name="payMaster">
        /// The <see cref="Address"/> of the paymaster.
        /// </param>
        /// <param name="gasToMortgage">
        /// The <see cref="FungibleAssetValue"/> of gas to mortgage.
        /// </param>
        /// <returns>
        /// The updated <see cref="IWorld"/>.
        /// </returns>
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

        /// <summary>
        /// Refund gas with the PayMaster.
        /// </summary>
        /// <param name="world">
        /// The <see cref="IWorld"/> where the refund happen.
        /// </param>
        /// <param name="context">
        /// The <see cref="IActionContext"/> where the refund happen.
        /// </param>
        /// <param name="address">
        /// The <see cref="Address"/> of the user to refund gas.
        /// </param>
        /// <param name="gasToRefund">
        /// The <see cref="FungibleAssetValue"/> of gas to refund.
        /// </param>
        /// <returns>
        /// The updated <see cref="IWorld"/>.
        /// </returns>
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
