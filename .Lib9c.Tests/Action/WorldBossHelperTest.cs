namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossHelperTest
    {
        private readonly Currency _crystalCurrency = CrystalCalculator.CRYSTAL;

        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        private readonly WorldBossContributionRewardSheet _sheet;

        public WorldBossHelperTest()
        {
            const string csv =
                "boss_id,reward1_count,reward1_item_id,reward1_ticker,reward2_count,reward2_item_id,reward2_ticker,reward3_count,reward3_item_id,reward3_ticker,reward4_count,reward4_item_id,reward4_ticker,reward5_count,reward5_item_id,reward5_ticker,reward6_count,reward6_item_id,reward6_ticker,reward7_count,reward7_item_id,reward7_ticker\n900001,1000000,,RUNESTONE_FENRIR4,1000000,,RUNESTONE_FENRIR5,1000000,,RUNESTONE_FENRIR6,1000000,600201,,1000000,600202,,1000000,,CRYSTAL,1000000,500000\n";
            _sheet = new WorldBossContributionRewardSheet();
            _sheet.Set(csv);
        }

        [Theory]
        [InlineData(10, 10, 0, 10)]
        [InlineData(10, 10, 1, 20)]
        [InlineData(10, 10, 5, 60)]
        public void CalculateTicketPrice(int ticketPrice, int additionalTicketPrice, int purchaseCount, int expected)
        {
            var row = new WorldBossListSheet.Row
            {
                TicketPrice = ticketPrice,
                AdditionalTicketPrice = additionalTicketPrice,
            };
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var raiderState = new RaiderState
            {
                PurchaseCount = purchaseCount,
            };
            Assert.Equal(expected * currency, WorldBossHelper.CalculateTicketPrice(row, raiderState, currency));
        }

        [Theory]
        [InlineData(7200L, 0L, 0L, true)]
        [InlineData(7250L, 7180L, 0L, true)]
        [InlineData(14400L, 14399L, 0L, true)]
        [InlineData(7250L, 7210L, 0L, false)]
        [InlineData(17200L, 10003L, 10000L, true)]
        [InlineData(17199L, 10003L, 10000L, false)]
        public void CanRefillTicketV1(long blockIndex, long refilledBlockIndex, long startedBlockIndex, bool expected)
        {
            Assert.Equal(expected, WorldBossHelper.CanRefillTicketV1(blockIndex, refilledBlockIndex, startedBlockIndex));
        }

        [Theory]
        [InlineData(7200L, 0L, 0L, 7200, true)]
        [InlineData(7250L, 7180L, 0L, 7200, true)]
        [InlineData(14400L, 14399L, 0L, 7200, true)]
        [InlineData(7250L, 7210L, 0L, 7200, false)]
        [InlineData(17200L, 10003L, 10000L, 7200, true)]
        [InlineData(17199L, 10003L, 10000L, 7200, false)]
        [InlineData(7300L, 5L, 0L, 7200, true)]
        [InlineData(7300L, 5L, 0L, 8400, false)]
        [InlineData(7200L, 0L, 0L, 0, false)]
        public void CanRefillTicket(long blockIndex, long refilledBlockIndex, long startedBlockIndex, int refillInterval, bool expected)
        {
            Assert.Equal(expected, WorldBossHelper.CanRefillTicket(blockIndex, refilledBlockIndex, startedBlockIndex, refillInterval));
        }

        [Theory]
        [InlineData(typeof(WorldBossRankRewardSheet))]
        [InlineData(typeof(WorldBossKillRewardSheet))]
        public void CalculateReward(Type sheetType)
        {
            var random = new TestRandom();
            IWorldBossRewardSheet sheet;
            if (sheetType == typeof(WorldBossRankRewardSheet))
            {
                sheet = _tableSheets.WorldBossRankRewardSheet;
            }
            else
            {
                sheet = _tableSheets.WorldBossKillRewardSheet;
            }

            foreach (var rewardRow in sheet.OrderedRows)
            {
                var bossId = rewardRow.BossId;
                var rank = rewardRow.Rank;
                var rewards = WorldBossHelper.CalculateReward(
                    rank,
                    bossId,
                    _tableSheets.RuneWeightSheet,
                    sheet,
                    _tableSheets.RuneSheet,
                    _tableSheets.MaterialItemSheet,
                    random
                );
                var expectedRune = rewardRow.Rune;
                var expectedCrystal = rewardRow.Crystal * _crystalCurrency;
                var expectedCircle = rewardRow.Circle;
                var crystal = rewards.assets.First(f => f.Currency.Equals(_crystalCurrency));
                var rune = rewards.assets
                    .Where(f => !f.Currency.Equals(_crystalCurrency))
                    .Sum(r => (int)r.MajorUnit);
                var circle = rewards.materials
                    .Where(kv => kv.Key.ItemSubType == ItemSubType.Circle)
                    .Sum(kv => kv.Value);

                Assert.Equal(expectedCrystal, crystal);
                Assert.Equal(expectedRune, rune);
                Assert.Equal(expectedCircle, circle);
            }
        }

        [Theory]
        [InlineData(1000, 250, "25")]
        [InlineData(1000000, 1, "0.0001")]
        [InlineData(1000, 0, "0.0000")]
        [InlineData(1000, 1500, "100")]
        public void CalculateContribution_ValidInput_ReturnsCorrectContribution(long totalDamage, long myDamage, string expected)
        {
            // Act
            decimal contribution = WorldBossHelper.CalculateContribution(totalDamage, myDamage);

            // Assert
            Assert.Equal(decimal.Parse(expected), contribution);
        }

        [Theory]
        [InlineData(0, 250)]
        [InlineData(-1000, 250)]
        public void CalculateContribution_ThrowsArgumentOutOfRangeException(long totalDamage, long myDamage)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldBossHelper.CalculateContribution(totalDamage, myDamage));
        }

        [Fact]
        public void CalculateContributionReward_Empty()
        {
            var row = _sheet[900001];
            var (items, fav) = WorldBossHelper.CalculateContributionReward(row, 0m);
            Assert.Empty(items);
            Assert.Empty(fav);
        }

        [Theory]
        [InlineData(0.0001, 1)]
        [InlineData(0.1, 1_000)]
        [InlineData(1, 10_000)]
        [InlineData(100, 1_000_000)]
        public void CalculateContributionReward(decimal contribution, int count)
        {
            var row = _sheet[900001];
            var (items, fav) = WorldBossHelper.CalculateContributionReward(row, contribution);
            Assert.All(items, i => Assert.Equal(count, i.count));
            Assert.All(fav, asset => Assert.Equal(count * asset.Currency, asset));
        }
    }
}
