namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ClaimRaidRewardTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IWorld _state;

        public ClaimRaidRewardTest()
        {
            var tableCsv = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(tableCsv);
            _state = new World(MockUtil.MockModernWorldState);
            foreach (var kv in tableCsv)
            {
                _state = _state.SetLegacyState(Addresses.GetSheetAddress(kv.Key), kv.Value.Serialize());
            }
        }

        [Theory]
        // rank 0
        [InlineData(typeof(NotEnoughRankException), 0, 0)]
        // Already Claim.
        [InlineData(typeof(NotEnoughRankException), 1, 1)]
        // Skip previous reward.
        [InlineData(null, 5, 1)]
        // Claim all reward.
        [InlineData(null, 1, 0)]
        [InlineData(null, 2, 0)]
        [InlineData(null, 3, 0)]
        [InlineData(null, 4, 0)]
        [InlineData(null, 5, 0)]
        public void Execute(Type exc, int rank, int latestRank)
        {
            Address agentAddress = default;
            var avatarAddress = new PrivateKey().Address;
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var highScore = 0;
            var characterRow = _tableSheets.WorldBossCharacterSheet[bossRow.BossId];
            foreach (var waveInfo in characterRow.WaveStats)
            {
                if (waveInfo.Wave > rank)
                {
                    continue;
                }

                highScore += (int)waveInfo.HP;
            }

            var raiderState = new RaiderState
            {
                HighScore = highScore,
                LatestRewardRank = latestRank,
            };
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            var state = _state
                .SetLegacyState(raiderAddress, raiderState.Serialize())
                .SetAvatarState(avatarAddress, avatarState);
            var randomSeed = 0;

            var rows = _tableSheets.WorldBossRankRewardSheet.Values
                .Where(x => x.BossId == bossRow.BossId);
            var expectedCrystal = 0;
            var expectedRune = 0;
            var expectedCircle = 0;
            foreach (var row in rows)
            {
                if (row.Rank <= latestRank ||
                    row.Rank > rank)
                {
                    continue;
                }

                expectedCrystal += row.Crystal;
                expectedRune += row.Rune;
                expectedCircle += row.Circle;
            }

            const long blockIndex = 5055201L;
            var action = new ClaimRaidReward(avatarAddress);
            if (exc is null)
            {
                var nextState = action.Execute(
                    new ActionContext
                    {
                        Signer = agentAddress,
                        BlockIndex = blockIndex,
                        RandomSeed = randomSeed,
                        PreviousState = state,
                    });

                var crystalCurrency = CrystalCalculator.CRYSTAL;
                Assert.Equal(
                    expectedCrystal * crystalCurrency,
                    nextState.GetBalance(agentAddress, crystalCurrency));

                var rune = 0;
                var runeIds = _tableSheets.RuneWeightSheet.Values
                    .Where(r => r.BossId == bossRow.BossId)
                    .SelectMany(r => r.RuneInfos.Select(i => i.RuneId)).ToHashSet();
                foreach (var runeId in runeIds)
                {
                    var runeCurrency = RuneHelper.ToCurrency(_tableSheets.RuneSheet[runeId]);
                    rune += (int)nextState.GetBalance(avatarAddress, runeCurrency).MajorUnit;
                }

                Assert.Equal(expectedRune, rune);

                var circleRow = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Circle);
                var inventory = nextState.GetAvatarState(avatarAddress).inventory;
                var itemCount = inventory.TryGetTradableFungibleItems(circleRow.ItemId, null, blockIndex, out var items)
                    ? items.Sum(item => item.count)
                    : 0;
                Assert.Equal(expectedCircle, itemCount);
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(
                        new ActionContext
                        {
                            Signer = default,
                            BlockIndex = 5055201L,
                            RandomSeed = randomSeed,
                            PreviousState = state,
                        }));
            }
        }
    }
}
