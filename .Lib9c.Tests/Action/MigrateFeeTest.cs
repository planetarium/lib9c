namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class MigrateFeeTest
    {
        [Fact]
        public void Execute()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
#pragma warning disable CS0618
            var ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, new AdminState(admin, 100).Serialize())
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(ncgCurrency).Serialize());
            var addresses = new List<Address>();
            for (int i = 1; i < 10; i++)
            {
                var address = new PrivateKey().Address;
                var amount = 0.1m * i;
                state = state.MintAsset(
                    context, address, FungibleAssetValue.Parse(ncgCurrency, amount.ToString(CultureInfo.InvariantCulture)));
                addresses.Add(address);
            }

            var action = new MigrateFee
            {
                FeeAddresses = addresses,
            };

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = state,
                RandomSeed = 0,
                Signer = admin,
            });

            foreach (var address in addresses)
            {
                Assert.Equal(0 * ncgCurrency, nextState.GetBalance(address, ncgCurrency));
            }

            Assert.Equal(FungibleAssetValue.Parse(ncgCurrency, "4.5"), nextState.GetBalance(MigrateFee.TargetAddress, ncgCurrency));
        }
    }
}
