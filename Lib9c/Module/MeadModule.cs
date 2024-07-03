using Bencodex.Types;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;

namespace Nekoyume.Module
{
    public static class MeadModule
    {
        private static readonly Address RealGasPriceAddress = new Address("0000000000000000000000000000000000000000");
        private static readonly Address GasLimitAddress =    new Address("0000000000000000000000000000000000000001");
        private static readonly Address GasUsedAddress =     new Address("0000000000000000000000000000000000000002");

        public static IWorld SetTxGasInfo(this IWorld world, FungibleAssetValue? realGasPrice, long? gasLimit)
        {
            var account = world.GetAccount(Addresses.MeadPool);
            account = account.SetState(RealGasPriceAddress, realGasPrice?.Serialize() ?? Null.Value);
            account = account.SetState(GasLimitAddress, gasLimit is null ? Null.Value : (Integer)gasLimit);
            account = account.SetState(GasUsedAddress, (Integer)0);
            return world.SetAccount(Addresses.MeadPool, account);
        }

        public static IWorld UseGas(this IWorld world, long gas)
        {
            if (gas < 0)
            {
                throw new GasUseNegativeException();
            }

            var account = world.GetAccount(Addresses.MeadPool);
            if (account.GetState(GasLimitAddress) is not Integer gasLimit ||
                account.GetState(GasUsedAddress) is not Integer gasUsed)
            {
                // Bypass if the gas limit is not set.
                return world;
            }

            long newGasUsed = 0;
            try
            {
                newGasUsed = checked(gasUsed + gas);
            }
            catch (System.OverflowException)
            {
                throw new GasLimitExceededException(gasLimit, gasUsed + gas);
            }

            if (newGasUsed > gasLimit)
            {
                throw new GasLimitExceededException(gasLimit, newGasUsed);
            }

            gasUsed = newGasUsed;
            account = account.SetState(GasUsedAddress, gasUsed);
            return world.SetAccount(Addresses.MeadPool, account);
        }

        public static FungibleAssetValue? GasPriceUsed(this IWorldState worldState)
        {
            var account = worldState.GetAccountState(Addresses.MeadPool);
            if (account.GetState(RealGasPriceAddress) is not List realGasPriceRaw ||
                account.GetState(GasUsedAddress) is not Integer gasUsed)
            {
                // Bypass if the gas limit is not set.
                return null;
            }

            if (gasUsed <= 0)
            {
                return null;
            }

            var realGasPrice = new FungibleAssetValue(realGasPriceRaw);
            return realGasPrice * gasUsed;
        }

        public static FungibleAssetValue RemainingGas(this IWorldState worldState)
        {
            return worldState.GetBalance(Addresses.MeadPool, Currencies.Mead);
        }

        public static IWorld CleanUpGasInfo(this IWorld world)
        {
            var account = world.GetAccount(Addresses.MeadPool);
            account = account.RemoveState(RealGasPriceAddress);
            account = account.RemoveState(GasLimitAddress);
            account = account.RemoveState(GasUsedAddress);
            return world.SetAccount(Addresses.MeadPool, account);
        }
    }
}
