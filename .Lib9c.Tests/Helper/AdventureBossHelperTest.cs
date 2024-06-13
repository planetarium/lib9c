namespace Lib9c.Tests.Helper
{
    using System.Collections.Generic;
    using System.Numerics;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Data;
    using Nekoyume.Helper;
    using Nekoyume.Model.AdventureBoss;
    using Xunit;

    public class AdventureBossHelperTest
    {
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
        private readonly TableSheets _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        private Address _avatarAddress = new PrivateKey().Address;
        private string _name = "wanted";

        [Theory]
        // Raffle reward is always 0 when isReal == false
        [InlineData(false, false, 0)]
        [InlineData(true, false, 0)]
        [InlineData(true, true, 5)]
        public void CalculateWantedReward(bool isReal, bool winner, int expectedReward)
        {
            var bountyBoard = new BountyBoard(1);
            bountyBoard.FixedRewardFavId = 30001;
            bountyBoard.RandomRewardFavId = 30001;
            bountyBoard.AddOrUpdate(_avatarAddress, _name, 100 * NCG);

            AdventureBossHelper.PickWantedRaffle(bountyBoard, new TestRandom());

            if (!winner)
            {
                bountyBoard.RaffleWinner = new PrivateKey().Address;
            }

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
                isReal,
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
            var bountyBoard = new BountyBoard(1);
            bountyBoard.AddOrUpdate(_avatarAddress, _name, 100 * NCG);

            var exploreBoard = new ExploreBoard(1);
            var explorer = new Explorer(_avatarAddress, _name);
            exploreBoard.ExplorerList.Add((_avatarAddress, _name));
            exploreBoard.FixedRewardFavId = 20001;
            exploreBoard.UsedApPotion = 100;

            explorer.UsedApPotion = 100;

            AdventureBossHelper.PickExploreRaffle(bountyBoard, exploreBoard, new TestRandom());

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
                isReal,
                out var ncgReward
            );

            Assert.Equal(expectedNcgReward * NCG, ncgReward);
            Assert.Equal(expectedNcgReward * NCG, claimableReward.NcgReward);
        }
    }
}
