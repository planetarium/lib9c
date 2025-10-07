namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Battle;
    using Lib9c.Extensions;
    using Lib9c.Model;
    using Lib9c.Model.EnumType;
    using Lib9c.Model.Item;
    using Lib9c.Model.Mail;
    using Lib9c.Model.Quest;
    using Lib9c.Model.Rune;
    using Lib9c.Model.Skill;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Lib9c.TableData.Character;
    using Lib9c.TableData.Quest;
    using Lib9c.TableData.WorldAndStage;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Xunit;
    using static SerializeKeys;

    public class HackAndSlashTest
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
        private readonly IWorld _initialState;

        public HackAndSlashTest()
        {
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

            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
            agentState.avatarAddresses.Add(0, _avatarAddress);
            _weeklyArenaState = new WeeklyArenaState(0);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new World(MockUtil.MockModernWorldState);
            _initialState = _initialState
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetLegacyState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
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

        [Theory]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 2, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, true)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, false)]
        public void Execute(int avatarLevel, int worldId, int stageId, bool isClearedBefore)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
            clearedStageId = isClearedBefore ? Math.Max(clearedStageId, stageId - 1) : stageId - 1;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            IRandom random = new TestRandom();
            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId],
                    random);
                previousAvatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment, iLock: null);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState
                .SetAvatarState(_avatarAddress, previousAvatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize()));

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = ActionObsoleteConfig.V100301ExecutedBlockIndex,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var nextCpAccount = nextState.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure));
            var nextCpState = new CpState(nextCpAccount.GetState(_avatarAddress));

            Assert.True(nextCpState.Cp > 0);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
        }

        [Theory]
        [InlineData(4, 200)]
        public void Execute_With_UpdateQuestList(int worldId, int stageId)
        {
            var state = _initialState;

            // Remove stageId from WorldQuestSheet
            var worldQuestSheet = state.GetSheet<WorldQuestSheet>();
            var targetRow = worldQuestSheet.OrderedList.FirstOrDefault(e => e.Goal == stageId);
            Assert.NotNull(targetRow);
            // Update new AvatarState
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                state.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.level = 400;
            avatarState.exp = state.GetSheet<CharacterLevelSheet>().OrderedList.First(e => e.Level == 400).Exp;
            avatarState.worldInformation = new WorldInformation(0, state.GetSheet<WorldSheet>(), stageId);

            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            state = state
                .SetAvatarState(avatarState.address, avatarState);
            Assert.Equal(400, avatarState.level);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldId));
            Assert.True(avatarState.worldInformation.IsStageCleared(stageId));

            var avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.Equal(worldQuestSheet.Count, avatarWorldQuests.Count);
            Assert.Empty(avatarState.questList.completedQuestIds);
            Assert.Equal(equipments.Count, avatarState.inventory.Items.Count);

            // HackAndSlash
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = avatarState.address,
            };

            avatarState = state.GetAvatarState(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.DoesNotContain(avatarWorldQuests, e => e.Complete);

            state = state.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            // Second Execute
            state = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });

            avatarState = state.GetAvatarState(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.Equal(worldQuestSheet.Count, avatarWorldQuests.Count);
            Assert.Single(avatarWorldQuests, e => e.Goal == stageId && e.Complete);
        }

        [Fact]
        public void MaxLevelTest()
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;
            var requiredExp = expRow.ExpNeed;

            previousAvatarState.level = maxLevel;
            previousAvatarState.exp = maxLevelExp + requiredExp - 1;

            var stageId = 0;
            try
            {
                stageId = _tableSheets.StageSheet
                    .FirstOrDefault(
                        row =>
                            previousAvatarState.level - row.Value.Id <= StageRewardExpHelper.DifferLowerLimit ||
                            previousAvatarState.level - row.Value.Id > StageRewardExpHelper.DifferUpperLimit)
                    .Value.Id;
            }
            catch
            {
                // There is no stage that a avatar state which level is max can earning exp.
                return;
            }

            var worldRow = _tableSheets.WorldSheet
                .FirstOrDefault(
                    row => stageId >= row.Value.StageBegin &&
                        stageId <= row.Value.StageEnd);
            var worldId = worldRow.Value.Id;

            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                Math.Max(_tableSheets.StageSheet.First?.Id ?? 1, stageId));

            var state = _initialState.SetAvatarState(_avatarAddress, previousAvatarState);

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(maxLevelExp + requiredExp - 1, nextAvatarState.exp);
            Assert.Equal(previousAvatarState.level, nextAvatarState.level);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, GameConfig.MaxEquipmentSlotCount.Weapon)]
        [InlineData(ItemSubType.Armor, GameConfig.MaxEquipmentSlotCount.Armor)]
        [InlineData(ItemSubType.Belt, GameConfig.MaxEquipmentSlotCount.Belt)]
        [InlineData(ItemSubType.Necklace, GameConfig.MaxEquipmentSlotCount.Necklace)]
        [InlineData(ItemSubType.Ring, GameConfig.MaxEquipmentSlotCount.Ring)]
        public void MultipleEquipmentTest(ItemSubType type, int maxCount)
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;

            previousAvatarState.level = maxLevel;
            previousAvatarState.exp = maxLevelExp;

            var weaponRows = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == type)
                .Take(maxCount + 1);

            var equipments = new List<Guid>();
            foreach (var row in weaponRows)
            {
                var equipment = ItemFactory.CreateItem(
                        _tableSheets.EquipmentItemSheet[row.Id],
                        new TestRandom())
                    as Equipment;

                equipments.Add(equipment.ItemId);
                previousAvatarState.inventory.AddItem(equipment);
            }

            var state = _initialState.SetAvatarState(_avatarAddress, previousAvatarState);

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = equipments,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<DuplicateEquipmentException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<DuplicateEquipmentException>(exec);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_Throw_FailedLoadStateException(bool empty)
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = empty
                ? new World(MockUtil.MockModernWorldState)
                : _initialState
                    .SetAvatarState(_avatarAddress, _avatarState)
                    .SetAccount(Addresses.Inventory, new Account(MockUtil.MockAccountState));

            var exec = Assert.Throws<FailedLoadStateException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<FailedLoadStateException>(exec);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public void ExecuteThrowSheetRowColumnException(int stageId)
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<SheetRowColumnException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialState,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<SheetRowColumnException>(exec);
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundExceptionByStage()
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState;
            state = state.SetLegacyState(Addresses.TableSheet.Derive(nameof(StageSheet)), "test".Serialize());

            var exec = Assert.Throws<SheetRowNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<SheetRowNotFoundException>(exec);
        }

        [Fact]
        public void ExecuteThrowFailedAddWorldException()
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState;
            var worldSheet = new WorldSheet();
            worldSheet.Set("test");
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(0, worldSheet, false),
            };
            state = state.SetAvatarState(_avatarAddress, avatarState);

            Assert.False(avatarState.worldInformation.IsStageCleared(0));

            var exec = Assert.Throws<FailedAddWorldException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<FailedAddWorldException>(exec);
        }

        [Theory]
        // Try challenge Mimisbrunnr.
        [InlineData(GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, false)]
        // Unlock CRYSTAL first.
        [InlineData(2, 51, false)]
        [InlineData(2, 51, true)]
        public void Execute_Throw_InvalidWorldException(int worldId, int stageId, bool unlockedIdsExist)
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState;
            if (unlockedIdsExist)
            {
                state = state.SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                );
            }

            var exec = Assert.Throws<InvalidWorldException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<InvalidWorldException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageException()
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 3,
                AvatarAddress = _avatarAddress,
            };

            var avatarState = new AvatarState(_avatarState);
            avatarState.worldInformation.ClearStage(
                1,
                1,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            avatarState.worldInformation.TryGetWorld(1, out var world);

            Assert.True(world.IsStageCleared);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(1));

            var state = _initialState;
            state = state.SetAvatarState(_avatarAddress, avatarState);

            var exec = Assert.Throws<InvalidStageException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<InvalidStageException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageExceptionUnlockedWorld()
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 2,
                AvatarAddress = _avatarAddress,
            };

            _avatarState.worldInformation.TryGetWorld(1, out var world);
            Assert.False(world.IsStageCleared);

            var exec = Assert.Throws<InvalidStageException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialState,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<InvalidStageException>(exec);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void ExecuteThrowInvalidEquipmentException(ItemSubType itemSubType)
        {
            var avatarState = new AvatarState(_avatarState);
            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 100);
            avatarState.inventory.AddItem(equipment);

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>
                {
                    equipment.ItemId,
                },
                RuneInfos = new List<RuneSlotInfo>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var exec = Assert.Throws<RequiredBlockIndexException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<RequiredBlockIndexException>(exec);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void ExecuteThrowEquipmentSlotUnlockException(ItemSubType itemSubType)
        {
            var state = _initialState;
            var avatarState = new AvatarState(_avatarState)
            {
                level = 0,
            };
            state = state.SetAvatarState(_avatarAddress, avatarState);

            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 0);
            avatarState.inventory.AddItem(equipment);
            state = state.SetAvatarState(_avatarAddress, avatarState);

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>
                {
                    equipment.ItemId,
                },
                RuneInfos = new List<RuneSlotInfo>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<EquipmentSlotUnlockException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<EquipmentSlotUnlockException>(exec);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5, 2)]
        [InlineData(120, 25)]
        public void ExecuteThrowNotEnoughActionPointException(int ap, int playCount = 1)
        {
            var avatarState = new AvatarState(_avatarState);

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
                TotalPlayCount = playCount,
            };

            var state = _initialState;
            state = state.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, ap);

            var exec = Assert.Throws<NotEnoughActionPointException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<NotEnoughActionPointException>(exec);
        }

        [Fact]
        public void ExecuteWithoutPlayCount()
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = 1;
            var clearedStageId = 0;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            var equipments = new List<Guid>();
            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState.SetAvatarState(_avatarAddress, previousAvatarState);

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = 1,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(30)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(150)]
        [InlineData(200)]
        public void Execute_Throw_NotEnoughAvatarLevelException(int avatarLevel)
        {
            var avatarState = new AvatarState(_avatarState)
            {
                level = avatarLevel,
            };

            var state = _initialState;
            var itemIds = new[] { GameConfig.DefaultAvatarWeaponId, 40100000, };
            foreach (var itemId in itemIds)
            {
                foreach (var requirementRow in _tableSheets.ItemRequirementSheet.OrderedList
                    .Where(e => e.ItemId >= itemId && e.Level > avatarState.level)
                    .Take(3))
                {
                    var costumes = new List<Guid>();
                    var equipments = new List<Guid>();
                    var random = new TestRandom(DateTimeOffset.Now.Millisecond);
                    if (_tableSheets.EquipmentItemSheet.TryGetValue(requirementRow.ItemId, out var row))
                    {
                        var equipment = ItemFactory.CreateItem(row, random);
                        avatarState.inventory.AddItem(equipment);
                        equipments.Add(((INonFungibleItem)equipment).NonFungibleId);
                    }
                    else if (_tableSheets.CostumeItemSheet.TryGetValue(requirementRow.ItemId, out var row2))
                    {
                        var costume = ItemFactory.CreateItem(row2, random);
                        avatarState.inventory.AddItem(costume);
                        costumes.Add(((INonFungibleItem)costume).NonFungibleId);
                    }

                    state = state.SetAvatarState(_avatarAddress, avatarState)
                        .SetActionPoint(_avatarAddress, 99999999);

                    var action = new HackAndSlash
                    {
                        Costumes = costumes,
                        Equipments = equipments,
                        Foods = new List<Guid>(),
                        RuneInfos = new List<RuneSlotInfo>(),
                        WorldId = 1,
                        StageId = 1,
                        AvatarAddress = avatarState.address,
                    };

                    var exec = Assert.Throws<NotEnoughAvatarLevelException>(
                        () => action.Execute(
                            new ActionContext
                            {
                                PreviousState = state,
                                Signer = avatarState.agentAddress,
                                RandomSeed = random.Seed,
                            }));

                    SerializeException<NotEnoughAvatarLevelException>(exec);
                }
            }
        }

        [Fact]
        public void ExecuteThrowInvalidItemCountException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                level = 1,
            };

            var state = _initialState.SetActionPoint(_avatarAddress, 99999999);
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = avatarState.address,
                TotalPlayCount = 24,
                ApStoneCount = -1,
            };

            var exec = Assert.Throws<InvalidItemCountException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = avatarState.agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<InvalidItemCountException>(exec);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        public void ExecuteThrowPlayCountIsZeroException(int totalPlayCount, int apStoneCount)
        {
            var avatarState = new AvatarState(_avatarState)
            {
                level = 1,
            };

            var state = _initialState.SetActionPoint(_avatarAddress, 99999999);
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = avatarState.address,
                TotalPlayCount = totalPlayCount,
                ApStoneCount = apStoneCount,
            };

            var exec = Assert.Throws<PlayCountIsZeroException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = avatarState.agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<PlayCountIsZeroException>(exec);
        }

        [Fact]
        public void ExecuteThrowUsageLimitExceedException()
        {
            var state = _initialState;
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarState.address,
                TotalPlayCount = 1,
                ApStoneCount = 11,
            };

            var exec = Assert.Throws<UsageLimitExceedException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _avatarState.agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<UsageLimitExceedException>(exec);
        }

        [Fact]
        public void ExecuteThrowNotEnoughMaterialException()
        {
            var state = _initialState;
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarState.address,
                TotalPlayCount = 1,
                ApStoneCount = 1,
            };

            var exec = Assert.Throws<NotEnoughMaterialException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _avatarState.agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<NotEnoughMaterialException>(exec);
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
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out var stageRow));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = 400;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                stageId);

            var costumes = new List<Guid>();
            var random = new TestRandom();
            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[costumeId],
                random);
            previousAvatarState.inventory.AddItem(costume);
            costumes.Add(costume.ItemId);

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState
                .SetAvatarState(_avatarAddress, previousAvatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    Enumerable.Range(1, worldId).ToList().Select(i => i.Serialize()).Serialize())
                .SetActionPoint(_avatarAddress, 999999);

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = 1,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            var prevItemMap = previousAvatarState.itemMap;
            var nextItemMap = nextAvatarState.itemMap;
            var totalCount = 0;
            // calculate get item count
            foreach (var kv in nextItemMap)
            {
                // exclude hourglass or appotion
                if (kv.Key is 400000 or 500000)
                {
                    continue;
                }

                var prevCount = prevItemMap.TryGetValue(kv.Key, out var value)
                    ? value
                    : 0;
                var nextCount = kv.Value;
                totalCount += nextCount - prevCount;
            }

            var questListCompletedQuestIds = nextAvatarState.questList.completedQuestIds;
            var completedQuest =
                nextAvatarState.questList.Where(i => questListCompletedQuestIds.Contains(i.Id));
            var questSum = 0;
            foreach (var quest in completedQuest)
            {
                foreach (var tuple in quest.Reward.ItemMap)
                {
                    // exclude hourglass or appotion
                    if (tuple.Item1 is 400000 or 500000)
                    {
                        continue;
                    }

                    questSum += tuple.Item2;
                }
            }

            var min = stageRow.Rewards.OrderBy(x => x.Min).First().Min;
            var max = stageRow.Rewards.OrderBy(x => x.Max).First().Max;
            var totalMin = min * stageRow.DropItemMin + questSum;
            var totalMax = max * stageRow.DropItemMax + questSum;
            // var totalCount = rewardItem.Sum(x => x.count);
            var materialItemSheet = _tableSheets.MaterialItemSheet;
            Assert.InRange(totalCount, totalMin, totalMax);

            var circleRow = materialItemSheet.Values.First(i => i.ItemSubType == ItemSubType.Circle);
            var circleRewardData = stageRow.Rewards.FirstOrDefault(reward => reward.ItemId == circleRow.Id);
            if (circleRewardData != null)
            {
                var circles = nextAvatarState.inventory.Items.Where(x => x.item.Id == circleRow.Id);
                Assert.All(circles, x => Assert.True(x.item is TradableMaterial));
            }
        }

        [Theory]
        [InlineData(false, false, false, false)]
        [InlineData(false, true, true, false)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, false)]
        [InlineData(true, false, false, false)]
        [InlineData(true, true, false, false)]
        [InlineData(true, true, true, false)]
        [InlineData(true, true, true, true)]
        public void CheckCrystalRandomSkillState(
            bool clear,
            bool skillStateExist,
            bool useCrystalSkill,
            bool setSkillByArgument)
        {
            const int worldId = 1;
            const int stageId = 10;
            const int clearedStageId = 9;
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = clear ? 400 : 1;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            var random = new TestRandom();
            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[costumeId],
                random);
            previousAvatarState.inventory.AddItem(costume);
            costumes.Add(costume.ItemId);

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState.SetAvatarState(_avatarAddress, previousAvatarState)
                .SetActionPoint(_avatarAddress, 999999);

            state = state.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(_avatarAddress);
            CrystalRandomSkillState skillState = null;
            if (skillStateExist)
            {
                skillState = new CrystalRandomSkillState(skillStateAddress, stageId);
                if (useCrystalSkill)
                {
                    skillState.Update(int.MaxValue, _tableSheets.CrystalStageBuffGachaSheet);
                    skillState.Update(
                        _tableSheets.CrystalRandomBuffSheet
                            .Select(pair => pair.Value.Id).ToList());
                }

                state = state.SetLegacyState(skillStateAddress, skillState.Serialize());
            }

            int? stageBuffId = null;
            if (useCrystalSkill)
            {
                stageBuffId = skillState?.GetHighestRankSkill(_tableSheets.CrystalRandomBuffSheet);
                Assert.NotNull(stageBuffId);
            }

            if (clear)
            {
                previousAvatarState.EquipItems(costumes.Concat(equipments.Select(e => e.ItemId)));
            }

            var action = new HackAndSlash
            {
                Costumes = clear ? costumes : new List<Guid>(),
                Equipments = clear
                    ? equipments.Select(e => e.NonFungibleId).ToList()
                    : new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
                StageBuffId = setSkillByArgument
                    ? stageBuffId
                    : null,
            };

            var ctx = new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = 1,
            };
            var nextState = action.Execute(ctx);
            var skillsOnWaveStart = new List<Skill>();
            if (useCrystalSkill)
            {
                var skill = _tableSheets
                    .SkillSheet
                    .FirstOrDefault(
                        pair => pair.Key == _tableSheets
                            .CrystalRandomBuffSheet[stageBuffId.Value].SkillId);
                if (skill.Value != null)
                {
                    skillsOnWaveStart.Add(SkillFactory.GetV1(skill.Value, default, 100));
                }
            }

            var contextRandom = new TestRandom(ctx.RandomSeed);
            var simulator = new StageSimulatorV3(
                contextRandom,
                previousAvatarState,
                new List<Guid>(),
                null,
                skillsOnWaveStart,
                worldId,
                stageId,
                _tableSheets.StageSheet[stageId],
                _tableSheets.StageWaveSheet[stageId],
                false,
                StageRewardExpHelper.GetExp(previousAvatarState.level, stageId),
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulatorV3.GetWaveRewards(
                    contextRandom,
                    _tableSheets.StageSheet[stageId],
                    _tableSheets.MaterialItemSheet));
            simulator.Simulate();
            var log = simulator.Log;
            var skillStateIValue =
                nextState.GetLegacyState(skillStateAddress);
            var serialized = skillStateIValue as List;
            Assert.NotNull(serialized);
            var nextSkillState = new CrystalRandomSkillState(skillStateAddress, serialized);
            Assert.Equal(skillStateAddress, nextSkillState.Address);
            if (log.IsClear)
            {
                Assert.Equal(stageId + 1, nextSkillState.StageId);
                Assert.Equal(0, nextSkillState.StarCount);
            }
            else
            {
                Assert.Equal(stageId, nextSkillState.StageId);
                skillState?.Update(log.clearedWaveNumber, _tableSheets.CrystalStageBuffGachaSheet);
                Assert.Equal(skillState?.StarCount ?? log.clearedWaveNumber, nextSkillState.StarCount);
            }

            Assert.Empty(nextSkillState.SkillIds);
        }

        [Theory]
        [InlineData(1, 24)]
        [InlineData(2, 24)]
        [InlineData(3, 30)]
        [InlineData(4, 30)]
        [InlineData(5, 40)]
        public void CheckUsedApByStaking(int level, int playCount)
        {
            const int worldId = 1;
            const int stageId = 5;
            const int clearedStageId = 4;
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = 400;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var stakeStateAddress = LegacyStakeState.DeriveAddress(_agentAddress);
            var stakeState = new LegacyStakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == level)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, previousAvatarState)
                .SetActionPoint(_avatarAddress, 120)
                .SetLegacyState(stakeStateAddress, stakeState.SerializeV2())
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize()))
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());

            var expectedAp = state.GetActionPoint(_avatarAddress) -
                _tableSheets.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                    _tableSheets.StageSheet[stageId].CostAP,
                    playCount,
                    level);
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
                StageBuffId = null,
                TotalPlayCount = playCount,
            };

            var ctx = new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = 1,
            };
            var nextState = action.Execute(ctx);
            Assert.Equal(expectedAp, nextState.GetActionPoint(_avatarAddress));
        }

        [Theory]
        [InlineData(1, 1, 24, 0)]
        [InlineData(1, 1, 25, 5)]
        [InlineData(2, 1, 24, 0)]
        [InlineData(2, 1, 25, 5)]
        [InlineData(3, 1, 30, 0)]
        [InlineData(3, 1, 31, 4)]
        [InlineData(4, 1, 30, 0)]
        [InlineData(4, 1, 31, 4)]
        [InlineData(5, 1, 40, 0)]
        [InlineData(5, 1, 41, 3)]
        public void CheckUsingApStoneWithStaking(int level, int apStoneCount, int totalRepeatCount, int expectedUsingAp)
        {
            const int worldId = 1;
            const int stageId = 5;
            const int clearedStageId = 4;
            const int itemId = 303100;
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = 400;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);
            var apStoneRow = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(apStoneRow);
            previousAvatarState.inventory.AddItem(apStone, apStoneCount);
            var stakeStateAddress = LegacyStakeState.DeriveAddress(_agentAddress);
            var stakeState = new LegacyStakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == level)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, previousAvatarState)
                .SetActionPoint(_avatarAddress, expectedUsingAp)
                .SetLegacyState(stakeStateAddress, stakeState.SerializeV2())
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize()))
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());

            var itemCount = previousAvatarState.inventory.Items
                .FirstOrDefault(i => i.item.Id == itemId)?.count ?? 0;
            var expectedItemCount = itemCount + totalRepeatCount;
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
                StageBuffId = null,
                TotalPlayCount = totalRepeatCount,
                ApStoneCount = apStoneCount,
            };

            var ctx = new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = 1,
            };
            var nextState = action.Execute(ctx);
            var nextAvatar = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(expectedItemCount, nextAvatar.inventory.Items.First(i => i.item.Id == itemId).count);
            Assert.False(nextAvatar.inventory.HasItem(apStoneRow.Id));
            Assert.Equal(0, nextState.GetActionPoint(_avatarAddress));
        }

        [Fact]
        public void ExecuteThrowInvalidRepeatPlayException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                level = 1,
            };

            var apStoneRow = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(apStoneRow);
            avatarState.inventory.AddItem(apStone);
            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 99999);
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = avatarState.address,
                TotalPlayCount = 1,
                ApStoneCount = 1,
            };

            var exec = Assert.Throws<InvalidRepeatPlayException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = avatarState.agentAddress,
                        RandomSeed = 0,
                    }));

            SerializeException<InvalidRepeatPlayException>(exec);
        }

        [Fact]
        public void ExecuteTwoRepetitions()
        {
            var avatarLevel = 50;
            var worldId = 1;
            var stageId = 20;
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
            clearedStageId = Math.Max(clearedStageId, stageId - 1);
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            IRandom random = new TestRandom();
            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId],
                    random);
                previousAvatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState
                .SetAvatarState(_avatarAddress, previousAvatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize()));

            var allRuneState = state.GetRuneState(_avatarAddress, out _);
            if (!allRuneState.TryGetRuneState(30001, out _))
            {
                allRuneState.AddRuneState(new RuneState(30001, 1));
            }

            if (!allRuneState.TryGetRuneState(10002, out _))
            {
                allRuneState.AddRuneState(new RuneState(10002, 1));
            }

            state = state.SetRuneState(_avatarAddress, allRuneState);

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>()
                {
                    new (0, 30001),
                    new (1, 10002),
                },
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = ActionObsoleteConfig.V100301ExecutedBlockIndex,
                });

            var action2 = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>()
                {
                    new (0, 10002),
                    new (1, 30001),
                },
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            action2.Execute(
                new ActionContext
                {
                    PreviousState = nextState,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = ActionObsoleteConfig.V100301ExecutedBlockIndex,
                });
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            var avatarLevel = 50;
            var worldId = 1;
            var stageId = 20;
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
            clearedStageId = Math.Max(clearedStageId, stageId - 1);
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            IRandom random = new TestRandom();
            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId],
                    random);
                previousAvatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var context = new ActionContext();
            var state = _initialState.SetAvatarState(_avatarAddress, previousAvatarState);

            state = state.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

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

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>()
                {
                    new (slotIndex, runeId),
                    new (slotIndex2, runeId2),
                },
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            Assert.Throws(
                exception,
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        private static void SerializeException<T>(Exception exec)
            where T : Exception
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, exec);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (T)formatter.Deserialize(ms);

            Assert.Equal(exec.Message, deserialized.Message);
        }
    }
}
