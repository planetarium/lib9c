namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class ClaimWorldBossKillRewardTest
    {
        [Theory]
        [InlineData(200L, typeof(InvalidClaimException))]
        [InlineData(200L, null)]
        [InlineData(1L, null)]
        [InlineData(1L, typeof(InvalidClaimException))]
        public void Execute(long blockIndex, Type exc)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            IWorld state = new World(MockUtil.MockModernWorldState);

            var runeWeightSheet = new RuneWeightSheet();
            runeWeightSheet.Set(
                @"id,boss_id,rank,rune_id,weight
1,900001,0,10001,100
");
            var killRewardSheet = new WorldBossKillRewardSheet();
            killRewardSheet.Set(
                @"id,boss_id,rank,rune_min,rune_max,crystal
1,900001,0,1,1,100
");
            var worldBossListSheet = new WorldBossListSheet();
            worldBossListSheet.Set(
                @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,900001,0,100,300,200,100,10
");
            var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, 1);
            var worldBossKillRewardRecord = new WorldBossKillRewardRecord();
            if (exc is null)
            {
                worldBossKillRewardRecord[0] = false;
            }

            var raiderStateAddress = Addresses.GetRaiderAddress(avatarAddress, 1);
            var raiderState = new RaiderState();

            var worldBossAddress = Addresses.GetWorldBossAddress(1);
            var worldBossState = new WorldBossState(worldBossListSheet[1], tableSheets.WorldBossGlobalHpSheet[1]);

            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                rankingMapAddress);

            state = state
                .SetLegacyState(Addresses.GetSheetAddress<RuneWeightSheet>(), runeWeightSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossListSheet>(), worldBossListSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossKillRewardSheet>(), killRewardSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<RuneSheet>(), tableSheets.RuneSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossCharacterSheet>(), tableSheets.WorldBossCharacterSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<MaterialItemSheet>(), tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetAvatarState(avatarAddress, avatarState)
                .SetLegacyState(worldBossKillRewardRecordAddress, worldBossKillRewardRecord.Serialize())
                .SetLegacyState(worldBossAddress, worldBossState.Serialize())
                .SetLegacyState(raiderStateAddress, raiderState.Serialize());

            var action = new ClaimWordBossKillReward
            {
                AvatarAddress = avatarAddress,
            };

            if (exc is null)
            {
                var randomSeed = 0;
                var nextState = action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        Signer = agentAddress,
                        PreviousState = state,
                        RandomSeed = randomSeed,
                    });

                var runeCurrency = RuneHelper.ToCurrency(tableSheets.RuneSheet[10001]);
                Assert.Equal(1 * runeCurrency, nextState.GetBalance(avatarAddress, runeCurrency));
                Assert.Equal(100 * CrystalCalculator.CRYSTAL, nextState.GetBalance(agentAddress, CrystalCalculator.CRYSTAL));
                var nextRewardInfo = new WorldBossKillRewardRecord((List)nextState.GetLegacyState(worldBossKillRewardRecordAddress));
                Assert.All(nextRewardInfo, kv => Assert.True(kv.Value));

                var rewards = WorldBossHelper.CalculateReward(
                    0,
                    worldBossState.Id,
                    runeWeightSheet,
                    killRewardSheet,
                    tableSheets.RuneSheet,
                    tableSheets.MaterialItemSheet,
                    new TestRandom(randomSeed)
                );

                foreach (var reward in rewards.assets)
                {
                    if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                    {
                        Assert.Equal(reward, nextState.GetBalance(agentAddress, reward.Currency));
                    }
                    else
                    {
                        Assert.Equal(reward, nextState.GetBalance(avatarAddress, reward.Currency));
                    }
                }

                var inventory = nextState.GetAvatarState(avatarAddress).inventory;
                foreach (var reward in rewards.materials)
                {
                    var itemCount = inventory.TryGetTradableFungibleItems(reward.Key.FungibleId, null, blockIndex, out var items)
                        ? items.Sum(item => item.count)
                        : 0;
                    Assert.Equal(reward.Value, itemCount);
                }
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(
                        new ActionContext
                        {
                            BlockIndex = blockIndex,
                            Signer = default,
                            PreviousState = state,
                            RandomSeed = 0,
                        }));
            }
        }

        [Theory]
        [InlineData(150L, 3)] // inside [100, 200)
        [InlineData(200L, 1)] // the schedule has ended
        [InlineData(99L, 1)] // the schedule has not started yet
        public void Execute_AppliesScheduledKillRewardBoost(long blockIndex, int multiplier)
        {
            var csv =
                "id,target,target_id,op,value,start_block,end_block\n" +
                $"1,{BoostScheduleSheet.Targets.WorldBossKillReward},900001,MUL,3,100,200\n" +
                $"2,{BoostScheduleSheet.Targets.WorldBossRankReward},*,MUL,5,0,1000\n";

            AssertKillReward(csv.Serialize(), blockIndex, multiplier);
        }

        [Fact]
        public void Execute_IsUnchangedWhenTheChainWasNeverPatchedWithBoostScheduleSheet()
        {
            AssertKillReward(Null.Value, 150L, 1);
        }

        private static void AssertKillReward(IValue boostScheduleSheet, long blockIndex, int multiplier)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var runeWeightSheet = new RuneWeightSheet();
            runeWeightSheet.Set("id,boss_id,rank,rune_id,weight\n1,900001,0,10001,100\n");
            var killRewardSheet = new WorldBossKillRewardSheet();
            killRewardSheet.Set("id,boss_id,rank,rune_min,rune_max,crystal\n1,900001,0,1,1,100\n");
            var worldBossListSheet = new WorldBossListSheet();
            worldBossListSheet.Set(
                "id,boss_id,started_block_index,ended_block_index,fee,ticket_price," +
                "additional_ticket_price,max_purchase_count\n1,900001,0,100,300,200,100,10\n");
            var rewardRecord = new WorldBossKillRewardRecord
            {
                [0] = false,
                [1] = false,
            };
            var worldBossState = new WorldBossState(
                worldBossListSheet[1],
                tableSheets.WorldBossGlobalHpSheet[1])
            {
                Level = 2,
            };
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                avatarAddress.Derive("ranking_map"));
            IWorld state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GetSheetAddress<RuneWeightSheet>(), runeWeightSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossListSheet>(), worldBossListSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossKillRewardSheet>(), killRewardSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<RuneSheet>(), tableSheets.RuneSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<WorldBossCharacterSheet>(), tableSheets.WorldBossCharacterSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<MaterialItemSheet>(), tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<BoostScheduleSheet>(), boostScheduleSheet)
                .SetLegacyState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetAvatarState(avatarAddress, avatarState)
                .SetLegacyState(Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, 1), rewardRecord.Serialize())
                .SetLegacyState(Addresses.GetWorldBossAddress(1), worldBossState.Serialize())
                .SetLegacyState(Addresses.GetRaiderAddress(avatarAddress, 1), new RaiderState().Serialize());

            var nextState = new ClaimWordBossKillReward { AvatarAddress = avatarAddress }.Execute(
                new ActionContext
                {
                    BlockIndex = blockIndex,
                    Signer = agentAddress,
                    PreviousState = state,
                    RandomSeed = 0,
                });

            // Two unclaimed levels, each granting 1 rune and 100 crystal before the boost.
            var runeCurrency = RuneHelper.ToCurrency(tableSheets.RuneSheet[10001]);
            Assert.Equal(2 * multiplier * runeCurrency, nextState.GetBalance(avatarAddress, runeCurrency));
            Assert.Equal(
                2 * 100 * multiplier * CrystalCalculator.CRYSTAL,
                nextState.GetBalance(agentAddress, CrystalCalculator.CRYSTAL));
        }
    }
}
