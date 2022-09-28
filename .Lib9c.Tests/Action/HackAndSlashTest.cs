namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

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
        private readonly IAccountStateDelta _initialState;

        public HackAndSlashTest()
        {
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
            _weeklyArenaState = new WeeklyArenaState(0);
            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new State()
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, agentState.SerializeV2())
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_inventoryAddress, _avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, _avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, _avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

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

        [Theory]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 2, false, false, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 2, false, true, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, true, false, true)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, false, false, true)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, true, false, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, false, false, false)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, false, true, false)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, true, false, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, false, false, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, true, false, false)]
        public void Execute(int avatarLevel, int worldId, int stageId, bool backward, bool isWeaponLock, bool isClearedBefore)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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
                    _tableSheets.ItemSheet[costumeId], random);
                previousAvatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                var iLock = equipment.ItemSubType == ItemSubType.Weapon && isWeaponLock
                    ? new OrderLock(Guid.NewGuid())
                    : (ILock)null;
                previousAvatarState.inventory.AddItem(equipment, iLock: iLock);
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

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize());
            }

            state = state.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);

            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            Assert.Equal(!isWeaponLock, nextAvatarState.inventory.Equipments.OfType<Weapon>().Any(w => w.equipped));
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                state.GetAvatarSheets(),
                state.GetGameConfigState(),
                _rankingMapAddress)
            {
                level = 400,
                exp = state.GetSheet<CharacterLevelSheet>().OrderedList.First(e => e.Level == 400).Exp,
                worldInformation = new WorldInformation(0, state.GetSheet<WorldSheet>(), stageId),
            };
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            state = state
                .SetState(avatarState.address, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, avatarState.questList.Serialize());
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
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = avatarState.address,
            };

            avatarState = state.GetAvatarStateV2(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.DoesNotContain(avatarWorldQuests, e => e.Complete);

            state = state.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            // Second Execute
            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            avatarState = state.GetAvatarStateV2(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.Equal(worldQuestSheet.Count, avatarWorldQuests.Count);
            Assert.Single(avatarWorldQuests, e => e.Goal == stageId && e.Complete);
        }

        [Fact]
        public void MaxLevelTest()
        {
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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
                    .FirstOrDefault(row =>
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
                .FirstOrDefault(row => stageId >= row.Value.StageBegin &&
                stageId <= row.Value.StageEnd);
            var worldId = worldRow.Value.Id;

            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                Math.Max(_tableSheets.StageSheet.First?.Id ?? 1, stageId));

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.SerializeV2());

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
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
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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

            var state = _initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(_inventoryAddress, previousAvatarState.inventory.Serialize());

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = equipments,
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<DuplicateEquipmentException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            }));

            SerializeException<DuplicateEquipmentException>(exec);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_Throw_FailedLoadStateException(bool backward)
        {
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            IAccountStateDelta state = backward ? new State() : _initialState;
            if (!backward)
            {
                state = _initialState
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), null!);
            }

            var exec = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
                WorldId = 1,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState;
            state = state.SetState(Addresses.TableSheet.Derive(nameof(StageSheet)), "test".Serialize());

            var exec = Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
            state = state.SetState(_worldInformationAddress, avatarState.worldInformation.Serialize());

            Assert.False(avatarState.worldInformation.IsStageCleared(0));

            var exec = Assert.Throws<FailedAddWorldException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            IAccountStateDelta state = _initialState;
            if (unlockedIdsExist)
            {
                state = state.SetState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                );
            }

            var exec = Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
                WorldId = 1,
                StageId = 2,
                AvatarAddress = _avatarAddress,
            };

            _avatarState.worldInformation.TryGetWorld(1, out var world);
            Assert.False(world.IsStageCleared);

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var state = _initialState
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize());

            var exec = Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
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
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 0);
            avatarState.inventory.AddItem(equipment);
            state = state.SetState(_inventoryAddress, avatarState.inventory.Serialize());

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>
                {
                    equipment.ItemId,
                },
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var exec = Assert.Throws<EquipmentSlotUnlockException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<EquipmentSlotUnlockException>(exec);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5, 2)]
        [InlineData(120, 25)]
        public void ExecuteThrowNotEnoughActionPointException(int ap, int playCount = 1)
        {
            var avatarState = new AvatarState(_avatarState)
            {
                actionPoint = ap,
            };

            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
                PlayCount = playCount,
            };

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var exec = Assert.Throws<NotEnoughActionPointException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<NotEnoughActionPointException>(exec);
        }

        [Fact]
        public void ExecuteWithoutPlayCount()
        {
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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

            IAccountStateDelta state = _initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    previousAvatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    previousAvatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    previousAvatarState.questList.Serialize());

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
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
                actionPoint = 99999999,
                level = avatarLevel,
            };

            var state = _initialState;
            var itemIds = new[] { GameConfig.DefaultAvatarWeaponId, 40100000 };
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

                    state = state.SetState(avatarState.address, avatarState.SerializeV2())
                        .SetState(
                            avatarState.address.Derive(LegacyInventoryKey),
                            avatarState.inventory.Serialize());

                    var action = new HackAndSlash
                    {
                        Costumes = costumes,
                        Equipments = equipments,
                        Foods = new List<Guid>(),
                        WorldId = 1,
                        StageId = 1,
                        AvatarAddress = avatarState.address,
                    };

                    var exec = Assert.Throws<NotEnoughAvatarLevelException>(() => action.Execute(new ActionContext
                    {
                        PreviousStates = state,
                        Signer = avatarState.agentAddress,
                        Random = random,
                    }));

                    SerializeException<NotEnoughAvatarLevelException>(exec);
                }
            }
        }

        [Fact]
        public void ExecuteThrowPlayCountIsZeroException()
        {
            for (var playCount = -10; playCount <= 0; playCount++)
            {
                var avatarState = new AvatarState(_avatarState)
                {
                    actionPoint = 99999999,
                    level = 1,
                };

                var state = _initialState;
                var action = new HackAndSlash
                {
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    WorldId = 1,
                    StageId = 1,
                    AvatarAddress = avatarState.address,
                    PlayCount = playCount,
                };

                var exec = Assert.Throws<PlayCountIsZeroException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = avatarState.agentAddress,
                    Random = new TestRandom(),
                }));

                SerializeException<PlayCountIsZeroException>(exec);
            }
        }

        [Fact]
        public void Execute_V100291()
        {
            const string csv =
                @"id,group,_name,chance,duration,target_type,stat_type,modify_type,modify_value,icon_resource
101000,101000,체력 강화,20,10,Self,HP,Percentage,50,icon_buff_plus_hp
101001,101000,체력 강화,100,25,Self,HP,Percentage,50,icon_buff_plus_hp
102000,102000,공격 강화,20,10,Self,ATK,Percentage,25,icon_buff_plus_attack
102001,102000,공격 강화,100,25,Self,ATK,Percentage,50,icon_buff_plus_attack
102002,102000,공격 강화,100,10,Self,ATK,Percentage,25,icon_buff_plus_attack
103000,103000,방어 강화,20,10,Self,DEF,Percentage,25,icon_buff_plus_defense
103001,103000,방어 강화,100,25,Self,DEF,Percentage,50,icon_buff_plus_defense
103002,103000,방어 강화,100,10,Self,DEF,Percentage,25,icon_buff_plus_defense
104000,104000,치명 증가,20,10,Self,CRI,Percentage,50,icon_buff_plus_critical
104001,104000,치명 증가,100,25,Self,CRI,Percentage,75,icon_buff_plus_critical
104002,104000,치명 증가,100,10,Self,CRI,Percentage,25,icon_buff_plus_critical
105000,105000,회피 증가,20,10,Self,HIT,Percentage,50,icon_buff_plus_hit
105001,105000,회피 증가,100,25,Self,HIT,Percentage,75,icon_buff_plus_hit
105002,105000,회피 증가,100,10,Self,HIT,Percentage,25,icon_buff_plus_hit
106000,106000,속도 증가,20,10,Self,SPD,Percentage,50,icon_buff_plus_speed
106001,106000,속도 증가,100,25,Self,SPD,Percentage,75,icon_buff_plus_speed
106002,106000,속도 증가,100,10,Self,SPD,Percentage,25,icon_buff_plus_speed
202000,202000,공격 약화,20,10,Enemy,ATK,Percentage,-25,icon_buff_minus_attack
202001,202000,공격 약화,100,10,Enemy,ATK,Percentage,-25,icon_buff_minus_attack
203001,203000,방어 약화,100,10,Enemy,DEF,Percentage,-25,icon_buff_minus_defense
204001,204000,치명 감소,100,10,Enemy,CRI,Percentage,-25,icon_buff_minus_critical
205001,205000,회피 감소,100,10,Enemy,HIT,Percentage,-25,icon_buff_minus_hit
206001,206000,속도 감소,100,10,Enemy,SPD,Percentage,-25,icon_buff_minus_speed
301000,301000,체력 강화 (10),100,150,Self,HP,Percentage,10,icon_buff_plus_hp
302000,302000,공격 강화 (2),100,150,Self,ATK,Percentage,2,icon_buff_plus_attack
302001,302000,공격 강화 (2),100,150,Self,ATK,Percentage,2,icon_buff_plus_attack
302002,302000,공격 강화 (3),100,150,Self,ATK,Percentage,3,icon_buff_plus_attack
302003,302000,공격 강화 (6),100,150,Self,ATK,Percentage,6,icon_buff_plus_attack
302004,302000,공격 강화 (3),100,150,Self,ATK,Percentage,3,icon_buff_plus_attack
302005,302000,공격 강화 (5),100,150,Self,ATK,Percentage,5,icon_buff_plus_attack
302006,302000,공격 강화 (8),100,150,Self,ATK,Percentage,8,icon_buff_plus_attack
302007,302000,S 올스텟,100,150,Self,ATK,Percentage,20,icon_buff_plus_attack
302008,302000,S 공격력2,100,150,Self,ATK,Percentage,35,icon_buff_plus_attack
302009,302000,공격 강화 (18),100,150,Self,ATK,Percentage,18,icon_buff_plus_attack
302010,302000,SS 올스탯,100,150,Self,ATK,Percentage,50,icon_buff_plus_attack
302011,302000,S 공격력1,100,150,Self,ATK,Percentage,60,icon_buff_plus_attack
302012,302000,SS 공격력,100,150,Self,ATK,Percentage,90,icon_buff_plus_attack
303000,303000,방어 강화 (2),100,150,Self,DEF,Percentage,2,icon_buff_plus_defense
303001,303000,방어 강화 (2),100,150,Self,DEF,Percentage,2,icon_buff_plus_defense
303002,303000,방어 강화 (3),100,150,Self,DEF,Percentage,3,icon_buff_plus_defense
303003,303000,방어 강화 (6),100,150,Self,DEF,Percentage,6,icon_buff_plus_defense
303004,303000,방어 강화 (3),100,150,Self,DEF,Percentage,3,icon_buff_plus_defense
303005,303000,방어 강화 (5),100,150,Self,DEF,Percentage,5,icon_buff_plus_defense
303006,303000,방어 강화 (8),100,150,Self,DEF,Percentage,8,icon_buff_plus_defense
303007,303000,S 올스텟,100,150,Self,DEF,Percentage,20,icon_buff_plus_defense
303008,303000,S 방어력2,100,150,Self,DEF,Percentage,35,icon_buff_plus_defense
303009,303000,방어 강화 (18),100,150,Self,DEF,Percentage,18,icon_buff_plus_defense
303010,303000,SS 올스탯,100,150,Self,DEF,Percentage,50,icon_buff_plus_defense
303011,303000,S 방어력1,100,150,Self,DEF,Percentage,60,icon_buff_plus_defense
303012,303000,SS 방어력,100,150,Self,DEF,Percentage,90,icon_buff_plus_defense
304000,304000,치명 증가 (100),100,150,Self,CRI,Percentage,100,icon_buff_plus_critical
304001,304000,치명 증가 (250),100,150,Self,CRI,Percentage,250,icon_buff_plus_critical
305000,305000,명중 강화 (2),100,150,Self,HIT,Percentage,2,icon_buff_plus_hit
305001,305000,명중 강화 (2),100,150,Self,HIT,Percentage,2,icon_buff_plus_hit
305002,305000,명중 강화 (3),100,150,Self,HIT,Percentage,3,icon_buff_plus_hit
305003,305000,명중 강화 (6),100,150,Self,HIT,Percentage,6,icon_buff_plus_hit
305004,305000,명중 강화 (3),100,150,Self,HIT,Percentage,3,icon_buff_plus_hit
305005,305000,명중 강화 (5),100,150,Self,HIT,Percentage,5,icon_buff_plus_hit
305006,305000,명중 강화 (8),100,150,Self,HIT,Percentage,8,icon_buff_plus_hit
305007,305000,S 올스텟,100,150,Self,HIT,Percentage,20,icon_buff_plus_hit
305008,305000,S 명중2,100,150,Self,HIT,Percentage,35,icon_buff_plus_hit
305009,305000,명중 강화 (18),100,150,Self,HIT,Percentage,18,icon_buff_plus_hit
305010,305000,SS 올스탯,100,150,Self,HIT,Percentage,50,icon_buff_plus_hit
305011,305000,S 명중1,100,150,Self,HIT,Percentage,60,icon_buff_plus_hit
305012,305000,SS 명중,100,150,Self,HIT,Percentage,90,icon_buff_plus_hit
306000,306000,속도 강화 (2),100,150,Self,SPD,Percentage,2,icon_buff_plus_speed
306001,306000,속도 강화 (2),100,150,Self,SPD,Percentage,2,icon_buff_plus_speed
306002,306000,속도 강화 (3),100,150,Self,SPD,Percentage,3,icon_buff_plus_speed
306003,306000,속도 강화 (6),100,150,Self,SPD,Percentage,6,icon_buff_plus_speed
306004,306000,속도 강화 (3),100,150,Self,SPD,Percentage,3,icon_buff_plus_speed
306005,306000,속도 강화 (5),100,150,Self,SPD,Percentage,5,icon_buff_plus_speed
306006,306000,속도 강화 (8),100,150,Self,SPD,Percentage,8,icon_buff_plus_speed
306007,306000,S 올스텟,100,150,Self,SPD,Percentage,20,icon_buff_plus_speed
306008,306000,S 속도2,100,150,Self,SPD,Percentage,35,icon_buff_plus_speed
306009,306000,속도 강화 (18),100,150,Self,SPD,Percentage,18,icon_buff_plus_speed
306010,306000,SS 올스탯,100,150,Self,SPD,Percentage,50,icon_buff_plus_speed
306011,306000,S 속도1,100,150,Self,SPD,Percentage,60,icon_buff_plus_speed
306012,306000,SS 속도,100,150,Self,SPD,Percentage,90,icon_buff_plus_speed
501001,501001,공격 강화 (분노),100,5,Self,ATK,Percentage,50,icon_buff_plus_attack
502001,502001,속도 강화 (분노),100,5,Self,SPD,Percentage,50,icon_buff_plus_speed
503011,503011,Berserk ATK(60) Wave 1,100,150,Self,ATK,Percentage,60,icon_buff_plus_attack
503012,503012,Berserk DEF(-100) Wave 1,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503021,503011,Berserk ATK(80) Wave 2,100,150,Self,ATK,Percentage,80,icon_buff_plus_attack
503022,503012,Berserk DEF(-100) Wave 2,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503031,503011,Berserk ATK(100) Wave 3,100,150,Self,ATK,Percentage,100,icon_buff_plus_attack
503032,503012,Berserk DEF(-100) Wave 3,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503041,503011,Berserk ATK(120) Wave 4,100,150,Self,ATK,Percentage,120,icon_buff_plus_attack
503042,503012,Berserk DEF(-100) Wave 4,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503051,503011,Berserk ATK(150) Wave 5,100,150,Self,ATK,Percentage,150,icon_buff_plus_attack
503052,503012,Berserk DEF(-100) Wave 5,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503015,503015,Berserk CRI(1500),100,150,Self,CRI,Percentage,1500,icon_buff_plus_critical";

            var initialState = _initialState;
            var keys = new List<string>
            {
                nameof(SkillActionBuffSheet),
                nameof(ActionBuffSheet),
                nameof(StatBuffSheet),
            };
            foreach (var (key, value) in _sheets)
            {
                if (keys.Contains(key))
                {
                    initialState = initialState.SetState(Addresses.TableSheet.Derive(key), null!);
                }
            }

            initialState = initialState.SetState(Addresses.TableSheet.Derive(nameof(BuffSheet)), csv.Serialize());

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            var costumes = new List<Guid>();
            IRandom random = new TestRandom();
            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

            var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
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

            initialState = initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize());

            initialState = initialState.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(1)
            );

            foreach (var key in keys)
            {
                Assert.Null(initialState.GetState(Addresses.GetSheetAddress(key)));
            }

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                WorldId = 1,
                StageId = 1,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);

            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
        }

        [Theory]
        [InlineData(true, 1, 15)]
        [InlineData(true, 2, 55)]
        [InlineData(true, 3, 111)]
        [InlineData(true, 4, 189)]
        [InlineData(false, 1, 15)]
        [InlineData(false, 2, 55)]
        [InlineData(false, 3, 111)]
        [InlineData(false, 4, 189)]
        public void CheckRewardItems(bool backward, int worldId, int stageId)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out var stageRow));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.actionPoint = 999999;
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
                _tableSheets.ItemSheet[costumeId], random);
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

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        previousAvatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        previousAvatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        previousAvatarState.questList.Serialize());
            }

            state = state.SetState(
                _avatarAddress.Derive("world_ids"),
                Enumerable.Range(1, worldId).ToList().Select(i => i.Serialize()).Serialize()
            );

            var action = new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            var rewardItem = nextAvatarState.inventory.Items.Where(
                x => x.item.ItemSubType != ItemSubType.FoodMaterial &&
                     x.item is IFungibleItem ownedFungibleItem &&
                     x.item.Id != 400000 && x.item.Id != 500000);

            var worldQuestSheet = state.GetSheet<WorldQuestSheet>();
            var questRow = worldQuestSheet.OrderedList.FirstOrDefault(e => e.Goal == stageId);
            var questRewardSheet = state.GetSheet<QuestRewardSheet>();
            var rewardIds = questRewardSheet.First(x => x.Key == questRow.QuestRewardId).Value
                .RewardIds;
            var questItemRewardSheet = state.GetSheet<QuestItemRewardSheet>();
            var materialItemSheet = state.GetSheet<MaterialItemSheet>();
            var sortedMaterialItemSheet = materialItemSheet
                .Where(x =>
                    x.Value.ItemSubType == ItemSubType.EquipmentMaterial ||
                    x.Value.ItemSubType == ItemSubType.MonsterPart).ToList();

            var selectedIdn = new Dictionary<int, int>();
            foreach (var row in questItemRewardSheet)
            {
                if (sortedMaterialItemSheet.Exists(x => x.Key.Equals(row.ItemId)))
                {
                    selectedIdn.Add(row.Key, row.Count);
                }
            }

            var questSum = rewardIds.Where(rewardId => selectedIdn.ContainsKey(rewardId))
                .Sum(rewardId => selectedIdn[rewardId]);
            var min = stageRow.Rewards.OrderBy(x => x.Min).First().Min;
            var max = stageRow.Rewards.OrderBy(x => x.Max).First().Max;
            var totalMin = min * stageRow.DropItemMin + questSum;
            var totalMax = max * stageRow.DropItemMax + questSum;
            var totalCount = rewardItem.Sum(x => x.count);
            Assert.InRange(totalCount, totalMin, totalMax);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void CheckCrystalRandomSkillState(bool clear, bool skillStateExist, bool hasCrystalSkill)
        {
            const int worldId = 1;
            const int stageId = 5;
            const int clearedStageId = 4;
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.actionPoint = 999999;
            previousAvatarState.level = clear ? 400 : 3;
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
                _tableSheets.ItemSheet[costumeId], random);
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
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    previousAvatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    previousAvatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    previousAvatarState.questList.Serialize());

            state = state.SetState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            var skillStateAddress = Addresses.GetSkillStateAddressFromAvatarAddress(_avatarAddress);
            CrystalRandomSkillState skillState = null;
            if (skillStateExist)
            {
                skillState = new CrystalRandomSkillState(skillStateAddress, stageId);
                if (hasCrystalSkill)
                {
                    skillState.Update(int.MaxValue, _tableSheets.CrystalStageBuffGachaSheet);
                }

                state = state.SetState(skillStateAddress, skillState.Serialize());
            }

            var action = new HackAndSlash
            {
                Costumes = clear ? costumes : new List<Guid>(),
                Equipments = clear
                    ? equipments.Select(e => e.NonFungibleId).ToList()
                    : new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
                StageBuffId = skillState?.SkillIds
                    .OrderBy(key => _tableSheets.CrystalRandomBuffSheet[key].Rank)
                    .FirstOrDefault(),
            };

            var ctx = new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            };
            var nextState = action.Execute(ctx);
            var contextRandom = new TestRandom(ctx.Random.Seed);
            var simulator = new StageSimulator(
                contextRandom,
                previousAvatarState,
                new List<Guid>(),
                new List<Skill>(),
                worldId,
                stageId,
                _tableSheets.StageSheet[stageId],
                _tableSheets.StageWaveSheet[stageId],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    contextRandom,
                    _tableSheets.StageSheet[stageId],
                    _tableSheets.MaterialItemSheet));
            simulator.Simulate();
            var log = simulator.Log;
            var skillStateIValue =
                nextState.GetState(skillStateAddress);
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
                Assert.Equal(
                    hasCrystalSkill
                        ? _tableSheets.CrystalStageBuffGachaSheet[stageId].MaxStar
                        : log.clearedWaveNumber,
                    nextSkillState.StarCount);
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
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.actionPoint = 120;
            previousAvatarState.level = 400;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == level)?.RequiredGold ?? 0;
            var state = _initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    previousAvatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    previousAvatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    previousAvatarState.questList.Serialize())
                .SetState(stakeStateAddress, stakeState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize()))
                .MintAsset(stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());

            var expectedAp = previousAvatarState.actionPoint -
                             _tableSheets.StakeActionPointCoefficientSheet.GetActionPointByStaking(
                                 _tableSheets.StageSheet[stageId].CostAP, playCount, level);
            var action = new HackAndSlash
            {
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = _avatarAddress,
                StageBuffId = null,
                PlayCount = playCount,
            };

            var ctx = new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            };
            var nextState = action.Execute(ctx);
            var nextAvatar = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(expectedAp, nextAvatar.actionPoint);
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
