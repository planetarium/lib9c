namespace Lib9c.Tests.Util
{
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Module;

    public static class CurrencyUtil
    {
        public static IWorld AddCurrency(
            IActionContext context,
            IWorld world,
            Address agentAddress,
            Currency currency,
            FungibleAssetValue amount
        )
        {
            return LegacyModule.MintAsset(world, context, agentAddress, amount);
        }
    }
}
