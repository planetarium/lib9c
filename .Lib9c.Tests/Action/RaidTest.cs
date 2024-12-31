namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RaidTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public RaidTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
            _agentAddress = new PrivateKey().Address;
            _avatarAddress = new PrivateKey().Address;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _goldCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Theory]
        // Join new raid.
        [InlineData(null, true, true, true, false, 0, 0L, false, false, 0, false, false, false, 5, false, 0, 10002, 1, 30001)]
        [InlineData(null, true, true, true, false, 0, 0L, false, false, 0, false, false, false, 5, true, 0, 10002, 1, 30001)]
        // Refill by interval.
        [InlineData(null, true, true, false, true, 0, -10800, false, false, 0, false, false, false, 5, true, 0, 10002, 1, 30001)]
        // Refill by NCG.
        [InlineData(null, true, true, false, true, 0, 200L, true, true, 0, false, false, false, 5, true, 0, 10002, 1, 30001)]
        [InlineData(null, true, true, false, true, 0, 200L, true, true, 1, false, false, false, 5, true, 0, 10002, 1, 30001)]
        // Boss level up.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, true, false, 5, true, 0, 10002, 1, 30001)]
        // Update RaidRewardInfo.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, true, true, 5, true, 0, 10002, 1, 30001)]
        // Boss skip level up.
        [InlineData(null, true, true, false, true, 3, 100L, false, false, 0, true, false, false, 5, true, 0, 10002, 1, 30001)]
        // AvatarState null.
        [InlineData(typeof(FailedLoadStateException), false, false, false, false, 0, 0L, false, false, 0, false, false, false, 5, false, 0, 10002, 1, 30001)]
        // Insufficient CRYSTAL.
        [InlineData(typeof(InsufficientBalanceException), true, true, false, false, 0, 0L, false, false, 0, false, false, false, 5, false, 0, 10002, 1, 30001)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, false, true, 0, 0L, true, false, 0, false, false, false, 5, false, 0, 10002, 1, 30001)]
        // Wait interval.
        [InlineData(typeof(RequiredBlockIntervalException), true, true, false, true, 3, 10L, false, false, 0, false, false, false, 1, false, 0, 10002, 1, 30001)]
        // Exceed purchase limit.
        [InlineData(typeof(ExceedTicketPurchaseLimitException), true, true, false, true, 0, 100L, true, false, 1_000, false, false, false, 5, false, 0, 10002, 1, 30001)]
        // Exceed challenge count.
        [InlineData(typeof(ExceedPlayCountException), true, true, false, true, 0, 100L, false, false, 0, false, false, false, 5, false, 0, 10002, 1, 30001)]
        [InlineData(typeof(DuplicatedRuneIdException), true, true, false, true, 3, 100L, true, false, 0, false, false, false, 5, false, 0, 30001, 1, 30001)]
        [InlineData(typeof(DuplicatedRuneSlotIndexException), true, true, false, true, 3, 100L, true, false, 0, false, false, false, 5, false, 1, 10002, 1, 30001)]
        public void Execute(
            Type exc,
            bool avatarExist,
            bool stageCleared,
            bool crystalExist,
            bool raiderStateExist,
            int remainChallengeCount,
            long refillBlockIndexOffset,
            bool payNcg,
            bool ncgExist,
            int purchaseCount,
            bool kill,
            bool levelUp,
            bool rewardRecordExist,
            long executeOffset,
            bool raiderListExist,
            int slotIndex,
            int runeId,
            int slotIndex2,
            int runeId2
        )
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First(
                    x =>
                    {
                        if (exc == typeof(InsufficientBalanceException))
                        {
                            return ncgExist ? x.TicketPrice > 0 : x.EntranceFee > 0;
                        }

                        return true;
                    })
                .StartedBlockIndex;

            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>()
                {
                    new (slotIndex, runeId),
                    new (slotIndex2, runeId2),
                },
                PayNcg = payNcg,
            };
            var crystal = CrystalCalculator.CRYSTAL;
            var raidId = _tableSheets.WorldBossListSheet.FindRaidIdByBlockIndex(blockIndex);
            var raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var worldBossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
            var hpSheet = _tableSheets.WorldBossGlobalHpSheet;
            var bossAddress = Addresses.GetWorldBossAddress(raidId);
            var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);
            var raiderListAddress = Addresses.GetRaiderListAddress(raidId);
            var level = 1;
            if (kill & !levelUp)
            {
                level = hpSheet.OrderedList.Last().Level;
            }

            var fee = _tableSheets.WorldBossListSheet[raidId].EntranceFee;

            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(_agentAddress, new AgentState(_agentAddress));

            foreach (var (key, value) in _sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var allRuneState = state.GetRuneState(_avatarAddress, out _);
            if (!allRuneState.TryGetRuneState(runeId, out _))
            {
                allRuneState.AddRuneState(new RuneState(runeId, 1));
            }

            if (!allRuneState.TryGetRuneState(runeId2, out _))
            {
                allRuneState.AddRuneState(new RuneState(runeId2, 1));
            }

            state = state.SetRuneState(_avatarAddress, allRuneState);

            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            if (avatarExist)
            {
                var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
                foreach (var equipment in equipments)
                {
                    avatarState.inventory.AddItem(equipment);
                }

                if (stageCleared)
                {
                    for (var i = 0; i < 50; i++)
                    {
                        avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                    }
                }

                if (crystalExist)
                {
                    var price = _tableSheets.WorldBossListSheet[raidId].EntranceFee;
                    state = state.MintAsset(context, _agentAddress, price * crystal);
                }

                if (raiderStateExist)
                {
                    var raiderState = new RaiderState();
                    raiderState.RefillBlockIndex = blockIndex + refillBlockIndexOffset;
                    raiderState.RemainChallengeCount = remainChallengeCount;
                    raiderState.TotalScore = 1_000;
                    raiderState.HighScore = 0;
                    raiderState.TotalChallengeCount = 1;
                    raiderState.PurchaseCount = purchaseCount;
                    raiderState.Cp = 0;
                    raiderState.Level = 0;
                    raiderState.IconId = 0;
                    raiderState.AvatarName = "hash";
                    raiderState.AvatarAddress = _avatarAddress;
                    raiderState.UpdatedBlockIndex = blockIndex;

                    state = state.SetLegacyState(raiderAddress, raiderState.Serialize());

                    var raiderList = new List().Add(raiderAddress.Serialize());

                    if (raiderListExist)
                    {
                        raiderList = raiderList.Add(new PrivateKey().Address.Serialize());
                    }

                    state = state.SetLegacyState(raiderListAddress, raiderList);
                }

                if (rewardRecordExist)
                {
                    var rewardRecord = new WorldBossKillRewardRecord
                    {
                        [0] = false,
                    };
                    state = state.SetLegacyState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
                }

                if (ncgExist)
                {
                    var row = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
                    state = state.MintAsset(context, _agentAddress, (row.TicketPrice + row.AdditionalTicketPrice * purchaseCount) * _goldCurrency);
                }

                state = state
                    .SetAvatarState(_avatarAddress, avatarState)
                    .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            }

            if (kill)
            {
                var bossState =
                    new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[level])
                    {
                        CurrentHp = 0,
                        Level = level,
                    };
                state = state.SetLegacyState(bossAddress, bossState.Serialize());
            }

            if (exc is null)
            {
                var randomSeed = 0;
                var ctx = new ActionContext
                {
                    BlockIndex = blockIndex + executeOffset,
                    PreviousState = state,
                    RandomSeed = randomSeed,
                    Signer = _agentAddress,
                };

                var nextState = action.Execute(ctx);

                var random = new TestRandom(randomSeed);
                var bossListRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(ctx.BlockIndex);
                var raidSimulatorSheets = _tableSheets.GetRaidSimulatorSheets();
                var simulator = new RaidSimulator(
                    bossListRow.BossId,
                    random,
                    avatarState,
                    action.FoodIds,
                    new AllRuneState(),
                    new RuneSlotState(BattleType.Raid),
                    raidSimulatorSheets,
                    _tableSheets.CostumeStatSheet,
                    new List<StatModifier>(),
                    _tableSheets.BuffLimitSheet,
                    _tableSheets.BuffLinkSheet
                );
                simulator.Simulate();
                var score = simulator.DamageDealt;

                var assetRewardMap = new Dictionary<Currency, FungibleAssetValue>();
                foreach (var reward in simulator.AssetReward)
                {
                    assetRewardMap[reward.Currency] = reward;
                }

                var materialRewardMap = new Dictionary<TradableMaterial, int>();
                foreach (var reward in simulator.Reward)
                {
                    Assert.True(reward is TradableMaterial);
                    if (reward is TradableMaterial tradableMaterial)
                    {
                        materialRewardMap.TryAdd(tradableMaterial, 0);
                        materialRewardMap[tradableMaterial]++;
                    }
                }

                if (rewardRecordExist)
                {
                    var bossRow = raidSimulatorSheets.WorldBossCharacterSheet[bossListRow.BossId];
                    Assert.True(state.TryGetLegacyState(bossAddress, out List prevRawBoss));
                    var prevBossState = new WorldBossState(prevRawBoss);
                    var rank = WorldBossHelper.CalculateRank(bossRow, raiderStateExist ? 1_000 : 0);
                    var rewards = WorldBossHelper.CalculateReward(
                        rank,
                        prevBossState.Id,
                        _tableSheets.RuneWeightSheet,
                        _tableSheets.WorldBossKillRewardSheet,
                        _tableSheets.RuneSheet,
                        _tableSheets.MaterialItemSheet,
                        random
                    );

                    foreach (var reward in rewards.assets)
                    {
                        if (!assetRewardMap.ContainsKey(reward.Currency))
                        {
                            assetRewardMap[reward.Currency] = reward;
                        }
                        else
                        {
                            assetRewardMap[reward.Currency] += reward;
                        }
                    }

                    foreach (var reward in rewards.materials)
                    {
                        materialRewardMap.TryAdd(reward.Key, 0);
                        materialRewardMap[reward.Key] += reward.Value;
                    }

                    foreach (var reward in assetRewardMap)
                    {
                        if (reward.Key.Equals(CrystalCalculator.CRYSTAL))
                        {
                            Assert.Equal(reward.Value, nextState.GetBalance(_agentAddress, reward.Key));
                        }
                        else
                        {
                            Assert.Equal(reward.Value, nextState.GetBalance(_avatarAddress, reward.Key));
                        }
                    }

                    var inventory = nextState.GetInventoryV2(_avatarAddress);
                    foreach (var reward in materialRewardMap)
                    {
                        var itemCount = inventory.TryGetTradableFungibleItems(reward.Key.FungibleId, null, context.BlockIndex, out var items)
                            ? items.Sum(item => item.count)
                            : 0;
                        Assert.Equal(reward.Value, itemCount);
                    }
                }

                if (assetRewardMap.ContainsKey(crystal))
                {
                    Assert.Equal(assetRewardMap[crystal], nextState.GetBalance(_agentAddress, crystal));
                }

                if (crystalExist)
                {
                    Assert.Equal(fee * crystal, nextState.GetBalance(bossAddress, crystal));
                }

                Assert.True(nextState.TryGetLegacyState(raiderAddress, out List rawRaider));
                var raiderState = new RaiderState(rawRaider);
                var expectedTotalScore = raiderStateExist ? 1_000 + score : score;
                var expectedRemainChallenge = payNcg ? 0 : 2;
                var expectedTotalChallenge = raiderStateExist ? 2 : 1;

                Assert.Equal(score, raiderState.HighScore);
                Assert.Equal(expectedTotalScore, raiderState.TotalScore);
                Assert.Equal(expectedRemainChallenge, raiderState.RemainChallengeCount);
                Assert.Equal(expectedTotalChallenge, raiderState.TotalChallengeCount);
                Assert.Equal(1, raiderState.Level);
                Assert.Equal(GameConfig.DefaultAvatarArmorId, raiderState.IconId);
                Assert.True(raiderState.Cp > 0);

                Assert.True(nextState.TryGetLegacyState(bossAddress, out List rawBoss));
                var bossState = new WorldBossState(rawBoss);
                var expectedLevel = level;
                if (kill & levelUp)
                {
                    expectedLevel++;
                }

                Assert.Equal(expectedLevel, bossState.Level);
                Assert.Equal(expectedLevel, raiderState.LatestBossLevel);
                if (kill)
                {
                    Assert.Equal(hpSheet[expectedLevel].Hp, bossState.CurrentHp);
                }

                if (payNcg)
                {
                    Assert.Equal(0 * _goldCurrency, nextState.GetBalance(_agentAddress, _goldCurrency));
                    Assert.Equal(purchaseCount + 1, nextState.GetRaiderState(raiderAddress).PurchaseCount);
                    Assert.True(nextState.GetBalance(Addresses.RewardPool, _goldCurrency) > 0 * _goldCurrency);
                }

                Assert.True(nextState.TryGetLegacyState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
                var rewardRecord = new WorldBossKillRewardRecord(rawRewardInfo);
                Assert.Contains(expectedLevel, rewardRecord.Keys);
                if (rewardRecordExist)
                {
                    Assert.True(rewardRecord[0]);
                }
                else
                {
                    if (expectedLevel == 1)
                    {
                        Assert.False(rewardRecord[1]);
                    }
                    else
                    {
                        Assert.DoesNotContain(1, rewardRecord.Keys);
                    }
                }

                Assert.True(nextState.TryGetLegacyState(raiderListAddress, out List rawRaiderList));
                var raiderList = rawRaiderList.ToList(StateExtensions.ToAddress);

                Assert.Contains(raiderAddress, raiderList);
            }
            else
            {
                if (exc == typeof(DuplicatedRuneIdException) || exc == typeof(DuplicatedRuneSlotIndexException))
                {
                    var ncgCurrency = state.GetGoldCurrency();
                    state = state.MintAsset(context, _agentAddress, 99999 * ncgCurrency);

                    var unlockRuneSlot = new UnlockRuneSlot()
                    {
                        AvatarAddress = _avatarAddress,
                        SlotIndex = 1,
                    };

                    state = unlockRuneSlot.Execute(
                        new ActionContext
                        {
                            BlockIndex = 1,
                            PreviousState = state,
                            Signer = _agentAddress,
                            RandomSeed = 0,
                        });
                }

                Assert.Throws(
                    exc,
                    () => action.Execute(
                        new ActionContext
                        {
                            BlockIndex = blockIndex + executeOffset,
                            PreviousState = state,
                            RandomSeed = 0,
                            Signer = _agentAddress,
                        }));
            }
        }

        [Fact]
        public void Execute_With_Reward()
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PayNcg = false,
            };

            var worldBossRow = _tableSheets.WorldBossListSheet.First().Value;
            var raidId = worldBossRow.Id;
            var raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var bossAddress = Addresses.GetWorldBossAddress(raidId);
            var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);

            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(_agentAddress, new AgentState(_agentAddress));

            foreach (var (key, value) in _sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            for (var i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var raiderState = new RaiderState();
            raiderState.RefillBlockIndex = 0;
            raiderState.RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
            raiderState.TotalScore = 1_000;
            raiderState.TotalChallengeCount = 1;
            raiderState.PurchaseCount = 0;
            raiderState.Cp = 0;
            raiderState.Level = 0;
            raiderState.IconId = 0;
            raiderState.AvatarName = "hash";
            raiderState.AvatarAddress = _avatarAddress;
            state = state.SetLegacyState(raiderAddress, raiderState.Serialize());

            var rewardRecord = new WorldBossKillRewardRecord
            {
                [1] = false,
            };
            state = state.SetLegacyState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());

            state = state
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            var bossState =
                new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[2])
                {
                    CurrentHp = 0,
                    Level = 2,
                };
            state = state.SetLegacyState(bossAddress, bossState.Serialize());
            var randomSeed = 0;
            var random = new TestRandom(randomSeed);

            var simulator = new RaidSimulator(
                worldBossRow.BossId,
                random,
                avatarState,
                action.FoodIds,
                new AllRuneState(),
                new RuneSlotState(BattleType.Raid),
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            simulator.Simulate();

            var rewardMap = new Dictionary<Currency, FungibleAssetValue>();
            foreach (var reward in simulator.AssetReward)
            {
                rewardMap[reward.Currency] = reward;
            }

            var materialRewardMap = new Dictionary<TradableMaterial, int>();
            foreach (var reward in simulator.Reward)
            {
                Assert.True(reward is TradableMaterial);
                if (reward is TradableMaterial tradableMaterial)
                {
                    materialRewardMap.TryAdd(tradableMaterial, 0);
                    materialRewardMap[tradableMaterial]++;
                }
            }

            var killRewards = WorldBossHelper.CalculateReward(
                0,
                bossState.Id,
                _tableSheets.RuneWeightSheet,
                _tableSheets.WorldBossKillRewardSheet,
                _tableSheets.RuneSheet,
                _tableSheets.MaterialItemSheet,
                random
            );

            var blockIndex = worldBossRow.StartedBlockIndex + gameConfigState.WorldBossRequiredInterval;
            var nextState = action.Execute(
                new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousState = state,
                    RandomSeed = randomSeed,
                    Signer = _agentAddress,
                });

            Assert.True(nextState.TryGetLegacyState(raiderAddress, out List rawRaider));
            var nextRaiderState = new RaiderState(rawRaider);
            Assert.Equal(simulator.DamageDealt, nextRaiderState.HighScore);

            foreach (var reward in killRewards.assets)
            {
                if (!rewardMap.ContainsKey(reward.Currency))
                {
                    rewardMap[reward.Currency] = reward;
                }
                else
                {
                    rewardMap[reward.Currency] += reward;
                }
            }

            foreach (var reward in killRewards.materials)
            {
                materialRewardMap.TryAdd(reward.Key, 0);
                materialRewardMap[reward.Key] += reward.Value;
            }

            foreach (var reward in rewardMap)
            {
                if (reward.Key.Equals(CrystalCalculator.CRYSTAL))
                {
                    Assert.Equal(reward.Value, nextState.GetBalance(_agentAddress, reward.Key));
                }
                else
                {
                    Assert.Equal(reward.Value, nextState.GetBalance(_avatarAddress, reward.Key));
                }
            }

            var inventory = nextState.GetInventoryV2(_avatarAddress);
            foreach (var reward in materialRewardMap)
            {
                var itemCount = inventory.TryGetTradableFungibleItems(reward.Key.FungibleId, null, blockIndex, out var items)
                    ? items.Sum(item => item.count)
                    : 0;
                Assert.Equal(reward.Value, itemCount);
            }

            Assert.Equal(1, nextRaiderState.Level);
            Assert.Equal(GameConfig.DefaultAvatarArmorId, nextRaiderState.IconId);
            Assert.True(nextRaiderState.Cp > 0);
            Assert.Equal(3, nextRaiderState.LatestBossLevel);
            Assert.True(nextState.TryGetLegacyState(bossAddress, out List rawBoss));
            var nextBossState = new WorldBossState(rawBoss);
            Assert.Equal(3, nextBossState.Level);
            Assert.True(nextState.TryGetLegacyState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
            var nextRewardInfo = new WorldBossKillRewardRecord(rawRewardInfo);
            Assert.True(nextRewardInfo[1]);
        }

        [Fact]
        public void Execute_With_Free_Crystal_Fee()
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PayNcg = false,
            };
            var crystal = CrystalCalculator.CRYSTAL;

            _sheets[nameof(WorldBossListSheet)] =
                "id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count\r\n" +
                "1,900002,0,100,0,1,1,40";

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(_agentAddress, new AgentState(_agentAddress));

            foreach (var (key, value) in _sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            for (var i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            state = state
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            var blockIndex = gameConfigState.WorldBossRequiredInterval;
            var randomSeed = 0;
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = randomSeed,
                Signer = _agentAddress,
            };
            action.Execute(ctx);
        }
    }
}
