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
    using Nekoyume.TableData;
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
            var highScore = 0L;
            var characterRow = _tableSheets.WorldBossCharacterSheet[bossRow.BossId];
            foreach (var waveInfo in characterRow.WaveStats)
            {
                if (waveInfo.Wave > rank)
                {
                    continue;
                }

                highScore += (long)waveInfo.HP;
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

        [Theory]
        [InlineData(5_000_000L, 5_100_000L, 2)] // inside the schedule
        [InlineData(5_000_000L, 5_055_201L, 1)] // the schedule ended at the claim block
        [InlineData(5_055_202L, 5_100_000L, 1)] // the schedule has not started yet
        public void Execute_AppliesScheduledRankRewardBoost(
            long startBlock,
            long endBlock,
            int expectedMultiplier)
        {
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var csv =
                "id,target,target_id,op,value,start_block,end_block\n" +
                $"1,{BoostScheduleSheet.Targets.WorldBossRankReward},{bossRow.BossId},MUL,2," +
                $"{startBlock},{endBlock}\n" +
                $"2,{BoostScheduleSheet.Targets.WorldBossKillReward},*,MUL,3,0,{long.MaxValue}\n" +
                $"3,{BoostScheduleSheet.Targets.WorldBossRankReward},{bossRow.BossId + 1},MUL,5,0," +
                $"{long.MaxValue}\n";

            AssertRankReward(csv.Serialize(), expectedMultiplier);
        }

        [Fact]
        public void Execute_IsUnchangedWhenTheChainWasNeverPatchedWithBoostScheduleSheet()
        {
            AssertRankReward(Bencodex.Types.Null.Value, 1);
        }

        private void AssertRankReward(Bencodex.Types.IValue boostScheduleSheet, int multiplier)
        {
            const int rank = 5;
            const long blockIndex = 5055201L;
            const int randomSeed = 0;
            Address agentAddress = default;
            var avatarAddress = new PrivateKey().Address;
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var characterRow = _tableSheets.WorldBossCharacterSheet[bossRow.BossId];
            var highScore = characterRow.WaveStats
                .Where(w => w.Wave <= rank)
                .Sum(w => (long)w.HP);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            var state = _state
                .SetLegacyState(
                    Addresses.TableSheet.Derive(nameof(BoostScheduleSheet)),
                    boostScheduleSheet)
                .SetLegacyState(raiderAddress, new RaiderState { HighScore = highScore }.Serialize())
                .SetAvatarState(avatarAddress, avatarState);

            var nextState = new ClaimRaidReward(avatarAddress).Execute(
                new ActionContext
                {
                    Signer = agentAddress,
                    BlockIndex = blockIndex,
                    RandomSeed = randomSeed,
                    PreviousState = state,
                });

            // What the unboosted claim grants, drawn with the same random.
            var random = new TestRandom(randomSeed);
            var circleRow = _tableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Circle);
            var expectedCircle = 0;
            var inventory = nextState.GetAvatarState(avatarAddress).inventory;
            var balances = new System.Collections.Generic.Dictionary<
                Libplanet.Types.Assets.Currency, Libplanet.Types.Assets.FungibleAssetValue>();
            for (var i = 0; i < rank; i++)
            {
                var (assets, materials) = WorldBossHelper.CalculateReward(
                    i + 1,
                    bossRow.BossId,
                    _tableSheets.RuneWeightSheet,
                    _tableSheets.WorldBossRankRewardSheet,
                    _tableSheets.RuneSheet,
                    _tableSheets.MaterialItemSheet,
                    random);
                foreach (var asset in assets)
                {
                    balances[asset.Currency] = balances.TryGetValue(asset.Currency, out var sum)
                        ? sum + asset
                        : asset;
                }

                expectedCircle += materials.Values.Sum();
            }

            Assert.NotEmpty(balances);
            foreach (var (currency, expected) in balances)
            {
                var owner = currency.Equals(CrystalCalculator.CRYSTAL) ? agentAddress : avatarAddress;
                Assert.Equal(expected * multiplier, nextState.GetBalance(owner, currency));
            }

            var circle = inventory.TryGetTradableFungibleItems(
                circleRow.ItemId, null, blockIndex, out var items)
                ? items.Sum(item => item.count)
                : 0;
            Assert.Equal(expectedCircle * multiplier, circle);
        }
    }
}
