namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class HackAndSlashSweep9Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _inventoryAddress;
        private readonly Address _worldInformationAddress;
        private readonly Address _questListAddress;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;
        private readonly IRandom _random;

        public HackAndSlashSweep9Test()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress
            )
            {
                level = 100,
            };
            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _weeklyArenaState = new WeeklyArenaState(0);
            _initialState = new MockStateDelta()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, agentState.SerializeV2())
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_inventoryAddress, _avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, _avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, _avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            foreach (var address in _avatarState.combinationSlotAddresses)
            {
                var slotState = new CombinationSlotState(
                    address,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                _initialState = _initialState.SetState(address, slotState.Serialize());
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

        [Theory]
        [InlineData(1, 1, 1, false, true)]
        [InlineData(1, 1, 1, false, false)]
        [InlineData(2, 1, 2, false, true)]
        [InlineData(2, 1, 2, false, false)]
        [InlineData(2, 2, 51, false, true)]
        [InlineData(2, 2, 51, false, false)]
        [InlineData(2, 2, 52, false, true)]
        [InlineData(2, 2, 52, false, false)]
        [InlineData(2, 1, 1, true, true)]
        [InlineData(2, 1, 1, true, false)]
        [InlineData(2, 1, 2, true, true)]
        [InlineData(2, 1, 2, true, false)]
        [InlineData(2, 2, 51, true, true)]
        [InlineData(2, 2, 51, true, false)]
        [InlineData(2, 2, 52, true, true)]
        [InlineData(2, 2, 52, true, false)]
        public void Execute(int apStoneCount, int worldId, int stageId, bool challenge, bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var prevStageId = stageId - 1;
            var worldInformation = new WorldInformation(
                    0, _initialState.GetSheet<WorldSheet>(), challenge ? prevStageId : stageId);

            if (challenge)
            {
                worldInformation.UnlockWorld(worldId, 0,  _tableSheets.WorldSheet);
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation = worldInformation,
                level = 400,
            };

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, apStoneCount);

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            state = state.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount = gameConfigState.ActionPointMax / stageRow.CostAP * apStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    playCount);

                var random = new TestRandom(_random.Seed);
                var expectedRewardItems = HackAndSlashSweep6.GetRewardItems(
                    random,
                    playCount,
                    stageRow,
                    _tableSheets.MaterialItemSheet);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep
                {
                    actionPoint = avatarState.actionPoint,
                    costumes = costumes,
                    equipments = equipments,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    apStoneCount = apStoneCount,
                    worldId = worldId,
                    stageId = stageId,
                };

                state = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    Random = _random,
                });

                var nextAvatarState = state.GetAvatarStateV2(_avatarAddress);

                Assert.Equal(expectedLevel, nextAvatarState.level);
                Assert.Equal(expectedExp, nextAvatarState.exp);
                Assert.Equal(
                    expectedRewardItems.Count(),
                    nextAvatarState.inventory.Items.Sum(x => x.count));
                foreach (var i in nextAvatarState.inventory.Items)
                {
                    nextAvatarState.inventory.TryGetItem(i.item.Id, out var item);
                    Assert.Equal(expectedRewardItems.Count(x => x.Id == i.item.Id), item.count);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_FailedLoadStateException(bool backward)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 1,
            };

            var state = backward ? new MockStateDelta() : _initialState;
            if (!backward)
            {
                state = _initialState
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), null!);
            }

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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

            var state = _initialState.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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

            var state = _initialState.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(1, 48, 1, 50, true)]
        [InlineData(1, 48, 1, 50, false)]
        [InlineData(1, 49, 2, 51, true)]
        [InlineData(1, 49, 2, 51, false)]
        public void Execute_InvalidStageException(int clearedWorldId, int clearedStageId, int worldId, int stageId, bool backward)
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

            var state = _initialState.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            if (backward)
            {
                state = state.SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                state = state
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        _avatarState.worldInformation.Serialize());
            }

            Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(GameConfig.MimisbrunnrWorldId, true, 10000001, false)]
        [InlineData(GameConfig.MimisbrunnrWorldId, false, 10000001, true)]
        // Unlock CRYSTAL first.
        [InlineData(2, false, 51, false)]
        [InlineData(2, true, 51, false)]
        public void Execute_InvalidWorldException(int worldId, bool backward, int stageId, bool unlockedIdsExist)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 10000001),
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            if (unlockedIdsExist)
            {
                state = state.SetState(
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
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(99, true)]
        [InlineData(99, false)]
        public void Execute_UsageLimitExceedException(int apStoneCount, bool backward)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = apStoneCount,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 2,
            };

            Assert.Throws<UsageLimitExceedException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(3, 2, true)]
        [InlineData(7, 5, false)]
        public void Execute_NotEnoughMaterialException(int useApStoneCount, int holdingApStoneCount, bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
            };

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, holdingApStoneCount);

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * useApStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);

                var action = new HackAndSlashSweep
                {
                    equipments = equipments,
                    costumes = costumes,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = useApStoneCount,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                }));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_NotEnoughActionPointException(bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
                actionPoint = 0,
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

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
                        Random = new TestRandom(),
                    }));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_PlayCountIsZeroException(bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
                actionPoint = 0,
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

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
                        Random = new TestRandom(),
                    }));
            }
        }

        [Theory]
        [InlineData(1, 24, true)]
        [InlineData(1, 24, false)]
        public void Execute_NotEnoughCombatPointException(int worldId, int stageId, bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 0,
                level = 1,
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws<NotEnoughCombatPointException>(() =>
                    action.Execute(new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        Random = new TestRandom(),
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 120,
                level = 3,
            };
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
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = avatarState.actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                var nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                });
                var nextAvatar = nextState.GetAvatarStateV2(_avatarAddress);
                Assert.Equal(expectedLevel, nextAvatar.level);
                Assert.Equal(expectedExp, nextAvatar.exp);
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 120,
                level = 3,
            };
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
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = avatarState.actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    playCount);

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
                    Random = new TestRandom(),
                });

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>()
                    {
                        new RuneSlotInfo(slotIndex, runeId),
                        new RuneSlotInfo(slotIndex2, runeId2),
                    },
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws(exception, () => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                }));
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }
        }
    }
}
