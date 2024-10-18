namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class HackAndSlashSweepTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IWorld _initialState;
        private readonly IRandom _random;

        public HackAndSlashSweepTest()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _rankingMapAddress
            );
            _avatarState.level = 100;

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _weeklyArenaState = new WeeklyArenaState(0);
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax);

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            foreach (var address in _avatarState.combinationSlotAddresses)
            {
                var slotState = new CombinationSlotState(
                    address,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                _initialState = _initialState.SetLegacyState(address, slotState.Serialize());
            }
        }

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level)
                .Select(e => e.NonFungibleId).ToList();
            var random = new TestRandom();
            var costumes = new List<Guid>();
            if (avatarState.level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipments, costumes);
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 1,
            };

            IWorld state = new World(MockUtil.MockModernWorldState);

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Theory]
        [InlineData(100, 1)]
        public void Execute_SheetRowNotFoundException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            var state = _initialState.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Theory]
        [InlineData(1, 999)]
        [InlineData(2, 50)]
        public void Execute_SheetRowColumnException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            var state = _initialState.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Theory]
        [InlineData(1, 48, 1, 50)]
        [InlineData(1, 49, 2, 51)]
        public void Execute_InvalidStageException(int clearedWorldId, int clearedStageId, int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };
            var worldSheet = _initialState.GetSheet<WorldSheet>();
            var worldUnlockSheet = _initialState.GetSheet<WorldUnlockSheet>();

            _avatarState.worldInformation.ClearStage(clearedWorldId, clearedStageId, 1, worldSheet, worldUnlockSheet);

            var state = _initialState
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                )
                .SetAvatarState(_avatarAddress, _avatarState);

            Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Theory]
        [InlineData(GameConfig.MimisbrunnrWorldId, 10000001, false)]
        [InlineData(GameConfig.MimisbrunnrWorldId, 10000001, true)]
        // Unlock CRYSTAL first.
        [InlineData(2, 51, false)]
        public void Execute_InvalidWorldException(int worldId, int stageId, bool unlockedIdsExist)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 10000001);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            if (unlockedIdsExist)
            {
                state = state.SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                );
            }

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Fact]
        public void Execute_UsageLimitExceedException()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 99,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 2,
            };

            Assert.Throws<UsageLimitExceedException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            }));
        }

        [Theory]
        [InlineData(3, 2)]
        [InlineData(7, 5)]
        public void Execute_NotEnoughMaterialException(int useApStoneCount, int holdingApStoneCount)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, holdingApStoneCount);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * useApStoneCount;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);

                var action = new HackAndSlashSweep
                {
                    equipments = equipments,
                    costumes = costumes,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = useApStoneCount,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                }));
            }
        }

        [Fact]
        public void Execute_NotEnoughActionPointException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep
                {
                    runeInfos = new List<RuneSlotInfo>(),
                    costumes = costumes,
                    equipments = equipments,
                    avatarAddress = _avatarAddress,
                    actionPoint = 999999,
                    apStoneCount = 0,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughActionPointException>(() =>
                    action.Execute(new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
            }
        }

        [Fact]
        public void Execute_PlayCountIsZeroException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = state.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep
                {
                    costumes = costumes,
                    equipments = equipments,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = 0,
                    apStoneCount = 0,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<PlayCountIsZeroException>(() =>
                    action.Execute(new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
            }
        }

        [Fact]
        public void Execute_NotEnoughCombatPointException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 1;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = state.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            var stageId = 24;
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    (int)playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = 1,
                    stageId = stageId,
                };

                Assert.Throws<NotEnoughCombatPointException>(() =>
                    action.Execute(new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void ExecuteWithStake(int stakingLevel)
        {
            const int worldId = 1;
            const int stageId = 1;
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 3;

            var itemRow = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == stakingLevel)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var actionPoint = _initialState.GetActionPoint(_avatarAddress);
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    (int)playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                var nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });
                var nextAvatar = nextState.GetAvatarState(_avatarAddress);
                Assert.Equal(expectedLevel, nextAvatar.level);
                Assert.Equal(expectedExp, nextAvatar.exp);
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }
        }

        [Theory]
        [InlineData(1, 15)]
        [InlineData(2, 55)]
        [InlineData(3, 111)]
        [InlineData(4, 189)]
        [InlineData(4, 200)]
        [InlineData(5, 250)]
        [InlineData(6, 300)]
        public void CheckRewardItems(int worldId, int stageId)
        {
            const int apStoneCount = 10;
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.level = 400;
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), stageId);

            var materialSheet = _initialState.GetSheet<MaterialItemSheet>();
            var itemRow = materialSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone, apStoneCount);

            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    Enumerable.Range(1, worldId).ToList().Select(i => i.Serialize()).Serialize())
                .SetActionPoint(_avatarAddress, 120);
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }

            var actionPoint = _initialState.GetActionPoint(_avatarAddress);
            var action = new HackAndSlashSweep
            {
                costumes = new List<Guid>(),
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
                actionPoint = (int)actionPoint,
                apStoneCount = apStoneCount,
                worldId = worldId,
                stageId = stageId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
            });
            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            var circleRow = materialSheet.Values.First(i => i.ItemSubType == ItemSubType.Circle);
            var circleRewardData = stageRow.Rewards.FirstOrDefault(reward => reward.ItemId == circleRow.Id);
            if (circleRewardData != null)
            {
                var circles = nextAvatar.inventory.Items.Where(x => x.item.Id == circleRow.Id);
                Assert.All(circles, x => Assert.True(x.item is TradableMaterial));
            }
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            var stakingLevel = 1;
            const int worldId = 1;
            const int stageId = 1;
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 3;

            var itemRow = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == stakingLevel)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var actionPoint = _initialState.GetActionPoint(_avatarAddress);
                var itemPlayCount =
                    DailyReward.ActionPointMax / costAp * 1;
                var apPlayCount = actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    (int)playCount);

                var ncgCurrency = state.GetGoldCurrency();
                state = state.MintAsset(context, _agentAddress, 99999 * ncgCurrency);

                var unlockRuneSlot = new UnlockRuneSlot()
                {
                    AvatarAddress = _avatarAddress,
                    SlotIndex = 1,
                };

                state = unlockRuneSlot.Execute(new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>()
                    {
                        new (slotIndex, runeId),
                        new (slotIndex2, runeId2),
                    },
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws(exception, () => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                }));
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, int.MinValue + 1)]
        [InlineData(-1, 0)]
        [InlineData(int.MinValue + 1, 0)]
        public void Execute_ArgumentOutOfRangeException(int ap, int apPotion)
        {
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep
                {
                    runeInfos = new List<RuneSlotInfo>(),
                    costumes = costumes,
                    equipments = equipments,
                    avatarAddress = _avatarAddress,
                    actionPoint = ap,
                    apStoneCount = apPotion,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    action.Execute(new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
            }
        }
    }
}
