namespace Lib9c.Tests
{
    using System.Linq;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class AddressesTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetAll(bool ignoreDerive)
        {
            var addresses = Addresses.GetAll(ignoreDerive);
            Assert.Contains(Addresses.Admin, addresses);
            Assert.Contains(Addresses.Blacksmith, addresses);
            Assert.Contains(Addresses.Credits, addresses);
            Assert.Contains(Addresses.Ranking, addresses);
            Assert.Contains(Addresses.Shop, addresses);
            Assert.Contains(Addresses.ActivatedAccount, addresses);
            Assert.Contains(Addresses.AuthorizedMiners, addresses);
            Assert.Contains(Addresses.GameConfig, addresses);
            Assert.Contains(Addresses.GoldCurrency, addresses);
            Assert.Contains(Addresses.GoldDistribution, addresses);
            Assert.Contains(Addresses.PendingActivation, addresses);
            Assert.Contains(Addresses.RedeemCode, addresses);
            Assert.Contains(Addresses.TableSheet, addresses);
            Assert.Contains(Addresses.WeeklyArena, addresses);

            if (ignoreDerive)
            {
                return;
            }

            // RankingState
            for (var i = 0; i < RankingState.RankingMapCapacity; i++)
            {
                Assert.Contains(RankingState.Derive(i), addresses);
            }

            // WeeklyArenaState
            // The address of `WeeklyArenaState` to subscribe to is not specified because since the address of `WeeklyArenaState` is different according to `BlockIndex`

            // TableSheet
            var iSheetType = typeof(ISheet);
            foreach (var address in iSheetType.Assembly.GetTypes()
                .Where(type => !type.IsInterface && !type.IsAbstract && type.IsSubclassOf(iSheetType))
                .Select(type => Addresses.TableSheet.Derive(type.Name)))
            {
                Assert.Contains(address, addresses);
            }
        }
    }
}
