namespace Lib9c.Tests.Helper
{
    using System.Collections.Generic;
    using System.Globalization;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Data;
    using Nekoyume.Helper;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.TableData;
    using Xunit;

    public class AdventureBossHelperTest
    {
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());
        private Address _avatarAddress = new PrivateKey().Address;
        private string _name = "wanted";

        [Theory]
        [InlineData(1, "0000000000000000000000000000000000000001")]
        [InlineData(10, "0000000000000000000000000000000000000010")]
        [InlineData(16, "0000000000000000000000000000000000000016")]
        [InlineData(100, "0000000000000000000000000000000000000100")]
        public void SeasonToAddressForm(int season, string expectedAddr)
        {
            Assert.Equal(expectedAddr, AdventureBossHelper.GetSeasonAsAddressForm(season));
        }

        [Theory]
        // Raffle reward is always 0 when isReal == false
        [InlineData(0)]
        public void CalculateWantedReward(int expectedReward)
        {
            var ncgRuneRatio =
                TableExtensions.ParseDecimal(
                    _tableSheets
                        .GameConfigSheet["adventure_boss_ncg_rune_ratio"].Value);
            var bountyBoard = new BountyBoard(1);
            bountyBoard.FixedRewardFavId = 30001;
            bountyBoard.RandomRewardFavId = 30001;
            bountyBoard.AddOrUpdate(_avatarAddress, _name, 100 * NCG);

            var claimableReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            claimableReward = AdventureBossHelper.CalculateWantedReward(
                claimableReward,
                bountyBoard,
                _avatarAddress,
                _tableSheets.AdventureBossNcgRewardRatioSheet,
                ncgRuneRatio,
                out var ncgReward
            );

            Assert.Equal(expectedReward * NCG, ncgReward);
            Assert.Equal(expectedReward * NCG, claimableReward.NcgReward);
        }

        [Theory]
        // Raffle reward is always 0 when isReal == false
        [InlineData(false, false, 0 + 15)]
        [InlineData(true, false, 0 + 15)]
        [InlineData(true, true, 5 + 15)]
        public void CalculateExploreReward(bool isReal, bool winner, int expectedNcgReward)
        {
            var ncgApRatio =
                TableExtensions.ParseDecimal(
                    _tableSheets
                        .GameConfigSheet["adventure_boss_ncg_ap_ratio"].Value);
            var ncgRuneRatio =
                TableExtensions.ParseDecimal(
                    _tableSheets
                        .GameConfigSheet["adventure_boss_ncg_rune_ratio"].Value);
            var bountyBoard = new BountyBoard(1);
            bountyBoard.AddOrUpdate(_avatarAddress, _name, 100 * NCG);

            var exploreBoard = new ExploreBoard(1);
            var explorerList = new ExplorerList(1);
            var explorer = new Explorer(_avatarAddress, _name);
            explorerList.Explorers.Add((_avatarAddress, _name));
            exploreBoard.FixedRewardFavId = 20001;
            exploreBoard.UsedApPotion = 100;
            exploreBoard.TotalPoint = 100;

            explorer.UsedApPotion = 100;
            explorer.Score = 100;

            AdventureBossHelper.PickExploreRaffle(
                bountyBoard,
                exploreBoard,
                explorerList,
                new TestRandom()
            );

            if (!winner)
            {
                exploreBoard.RaffleWinner = new PrivateKey().Address;
            }

            var claimableReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            claimableReward = AdventureBossHelper.CalculateExploreReward(
                claimableReward,
                bountyBoard,
                exploreBoard,
                explorer,
                _avatarAddress,
                _tableSheets.AdventureBossNcgRewardRatioSheet,
                ncgApRatio,
                ncgRuneRatio,
                isReal,
                out var ncgReward
            );

            Assert.Equal(expectedNcgReward * NCG, ncgReward);
            Assert.Equal(expectedNcgReward * NCG, claimableReward.NcgReward);
        }
    }
}
