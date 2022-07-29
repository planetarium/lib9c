namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class RaidTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public RaidTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _goldCurrency = new Currency("NCG", decimalPlaces: 2, minters: null);
        }

        [Theory]
        // Join new raid.
        [InlineData(null, true, true, 0L, true, false, 0, 0L, false, false, 0, false, false, false)]
        // Refill by interval.
        [InlineData(null, true, true, 300L, false, true, 0, 0L, false, false, 0, false, false, false)]
        // Refill by NCG.
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 0, false, false, false)]
        [InlineData(null, true, true, 200L, false, true, 0, 200L, true, true, 1, false, false, false)]
        // Boss level up.
        [InlineData(null, true, true, 200L, false, true, 3, 100L, false, false, 0, true, true, false)]
        // Update RaidRewardInfo.
        [InlineData(null, true, true, 200L, false, true, 3, 100L, false, false, 0, true, true, true)]
        // Boss skip level up.
        [InlineData(null, true, true, 200L, false, true, 3, 100L, false, false, 0, true, false, false)]
        // AvatarState null.
        [InlineData(typeof(FailedLoadStateException), false, false, 0L, false, false, 0, 0L, false, false, 0, false, false, false)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, false, 0L, false, false, 0, 0L, false, false, 0, false, false, false)]
        // Insufficient CRYSTAL.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, false, 0, 0L, false, false, 0, false, false, false)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, 0L, false, true, 0, 10L, true, false, 0, false, false, false)]
        // Exceed purchase limit.
        [InlineData(typeof(ExceedTicketPurchaseLimitException), true, true, 0L, false, true, 0, 10L, true, false, 1_000, false, false, false)]
        // Exceed challenge count.
        [InlineData(typeof(ExceedPlayCountException), true, true, 0L, false, true, 0, 0L, false, false, 0, false, false, false)]
        public void Execute(
            Type exc,
            bool avatarExist,
            bool stageCleared,
            long blockIndex,
            bool crystalExist,
            bool raiderStateExist,
            int remainChallengeCount,
            long refillBlockIndex,
            bool payNcg,
            bool ncgExist,
            int purchaseCount,
            bool kill,
            bool levelUp,
            bool rewardRecordExist)
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                PayNcg = payNcg,
            };
            Currency crystal = CrystalCalculator.CRYSTAL;
            int raidId = _tableSheets.WorldBossListSheet.FindRaidIdByBlockIndex(blockIndex);
            Address raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            WorldBossListSheet.Row worldBossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
            var hpSheet = _tableSheets.WorldBossGlobalHpSheet;
            Address bossAddress = Addresses.GetWorldBossAddress(raidId);
            Address worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);
            int level = 1;
            if (kill & !levelUp)
            {
                level = hpSheet.OrderedList.Last().Level;
            }

            IAccountStateDelta state = new State()
                .SetState(Addresses.GetSheetAddress<WorldBossListSheet>(), _tableSheets.WorldBossListSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossGlobalHpSheet>(), hpSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<CharacterSheet>(), _tableSheets.CharacterSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<CostumeStatSheet>(), _tableSheets.CostumeStatSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<RuneWeightSheet>(), _tableSheets.RuneWeightSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossKillRewardSheet>(), _tableSheets.WorldBossKillRewardSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<RuneSheet>(), _tableSheets.RuneSheet.Serialize())
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            if (avatarExist)
            {
                var avatarState = new AvatarState(
                    _avatarAddress,
                    _agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default
                );

                if (stageCleared)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                    }
                }

                if (crystalExist)
                {
                    state = state.MintAsset(_agentAddress, 300 * crystal);
                }

                if (raiderStateExist)
                {
                    var raiderState = new RaiderState();
                    raiderState.RefillBlockIndex = refillBlockIndex;
                    raiderState.RemainChallengeCount = remainChallengeCount;
                    raiderState.TotalScore = 1_000;
                    raiderState.HighScore = 1_000;
                    raiderState.TotalChallengeCount = 1;
                    raiderState.PurchaseCount = purchaseCount;
                    raiderState.Cp = 0;
                    raiderState.Level = 0;
                    raiderState.IconId = 0;
                    raiderState.AvatarNameWithHash = "hash";
                    raiderState.AvatarAddress = _avatarAddress;

                    state = state.SetState(raiderAddress, raiderState.Serialize());
                }

                if (rewardRecordExist)
                {
                    var rewardRecord = new WorldBossKillRewardRecord
                    {
                        [1] = new List<FungibleAssetValue>(),
                    };
                    state = state.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
                }

                if (ncgExist)
                {
                    var row = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
                    state = state.MintAsset(_agentAddress, (row.TicketPrice + row.AdditionalTicketPrice * purchaseCount) * _goldCurrency);
                }

                state = state
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());
            }

            if (kill)
            {
                var bossState =
                    new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[level])
                        {
                            CurrentHp = 1,
                            Level = level,
                        };
                state = state.SetState(bossAddress, bossState.Serialize());
            }

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _agentAddress,
                });

                Assert.Equal(0 * crystal, nextState.GetBalance(_agentAddress, crystal));
                if (crystalExist)
                {
                    Assert.Equal(300 * crystal, nextState.GetBalance(bossAddress, crystal));
                }

                Assert.True(nextState.TryGetState(raiderAddress, out List rawRaider));
                var raiderState = new RaiderState(rawRaider);
                int expectedTotalScore = raiderStateExist ? 11_000 : 10_000;
                int expectedRemainChallenge = payNcg ? 0 : 2;
                int expectedTotalChallenge = raiderStateExist ? 2 : 1;
                Assert.Equal(10_000, raiderState.HighScore);
                Assert.Equal(expectedTotalScore, raiderState.TotalScore);
                Assert.Equal(expectedRemainChallenge, raiderState.RemainChallengeCount);
                Assert.Equal(expectedTotalChallenge, raiderState.TotalChallengeCount);
                Assert.Equal(1, raiderState.Level);
                Assert.Equal(GameConfig.DefaultAvatarArmorId, raiderState.IconId);
                Assert.True(raiderState.Cp > 0);

                Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
                var bossState = new WorldBossState(rawBoss);
                int expectedLevel = level;
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
                else
                {
                    Assert.True(bossState.CurrentHp < hpSheet[level].Hp);
                }

                if (payNcg)
                {
                    Assert.Equal(0 * _goldCurrency, nextState.GetBalance(_agentAddress, _goldCurrency));
                    Assert.Equal(purchaseCount + 1, nextState.GetRaiderState(raiderAddress).PurchaseCount);
                }

                Assert.True(nextState.TryGetState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
                var rewardRecord = new WorldBossKillRewardRecord(rawRewardInfo);
                Assert.Contains(expectedLevel, rewardRecord.Keys);
                if (rewardRecordExist)
                {
                    Assert.NotEmpty(rewardRecord[1]);
                }
                else
                {
                    if (expectedLevel == 1)
                    {
                        Assert.Empty(rewardRecord[1]);
                    }
                    else
                    {
                        Assert.DoesNotContain(1, rewardRecord.Keys);
                    }
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = _agentAddress,
                }));
            }
        }

        [Fact]
        public void Execute_With_KillReward()
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                PayNcg = false,
            };
            Currency crystal = CrystalCalculator.CRYSTAL;
            long blockIndex = 1;
            int raidId = _tableSheets.WorldBossListSheet.FindRaidIdByBlockIndex(blockIndex);
            Address raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, raidId);
            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            WorldBossListSheet.Row worldBossRow = _tableSheets.WorldBossListSheet.FindRowByBlockIndex(blockIndex);
            var hpSheet = _tableSheets.WorldBossGlobalHpSheet;
            Address bossAddress = Addresses.GetWorldBossAddress(raidId);
            Address worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(_avatarAddress, raidId);

            IAccountStateDelta state = new State()
                .SetState(Addresses.GetSheetAddress<WorldBossListSheet>(), _tableSheets.WorldBossListSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossGlobalHpSheet>(), hpSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<CharacterSheet>(), _tableSheets.CharacterSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<CostumeStatSheet>(), _tableSheets.CostumeStatSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<RuneWeightSheet>(), _tableSheets.RuneWeightSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossKillRewardSheet>(), _tableSheets.WorldBossKillRewardSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<RuneSheet>(), _tableSheets.RuneSheet.Serialize())
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var raiderState = new RaiderState();
            raiderState.RefillBlockIndex = 0;
            raiderState.RemainChallengeCount = 3;
            raiderState.TotalScore = 1_000;
            raiderState.HighScore = 1_000;
            raiderState.TotalChallengeCount = 1;
            raiderState.PurchaseCount = 0;
            raiderState.Cp = 0;
            raiderState.Level = 0;
            raiderState.IconId = 0;
            raiderState.AvatarNameWithHash = "hash";
            raiderState.AvatarAddress = _avatarAddress;
            state = state.SetState(raiderAddress, raiderState.Serialize());

            var rewardRecord = new WorldBossKillRewardRecord
            {
                [1] = new List<FungibleAssetValue>(),
            };
            state = state.SetState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());

            state = state
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            var bossState =
                new WorldBossState(worldBossRow, _tableSheets.WorldBossGlobalHpSheet[2])
                    {
                        CurrentHp = 1,
                        Level = 2,
                    };
            state = state.SetState(bossAddress, bossState.Serialize());
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            Assert.Equal(0 * crystal, nextState.GetBalance(_agentAddress, crystal));
            Assert.True(nextState.TryGetState(raiderAddress, out List rawRaider));
            var nextRaiderState = new RaiderState(rawRaider);
            Assert.Equal(10_000, nextRaiderState.HighScore);
            Assert.Equal(1, nextRaiderState.Level);
            Assert.Equal(GameConfig.DefaultAvatarArmorId, nextRaiderState.IconId);
            Assert.True(nextRaiderState.Cp > 0);
            Assert.Equal(3, nextRaiderState.LatestBossLevel);
            Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
            var nextBossState = new WorldBossState(rawBoss);
            Assert.Equal(3, nextBossState.Level);
            Assert.True(nextState.TryGetState(worldBossKillRewardRecordAddress, out List rawRewardInfo));
            var nextRewardInfo = new WorldBossKillRewardRecord(rawRewardInfo);
            Assert.NotEmpty(nextRewardInfo[1]);
        }
    }
}
