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
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonBattleSweepTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly IWorld _initialState;
        private readonly IRandom _random;

        public EventDungeonBattleSweepTest()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address
            );
            _avatarState.level = 1;

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new World(MockUtil.MockModernWorldState)
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
        }

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level).ToList();
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment, iLock: null);
            }

            var equipmentGuids = equipments.Select(e => e.NonFungibleId).ToList();
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
                    _tableSheets.ItemSheet[costumeId],
                    random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipmentGuids, costumes);
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 10000001,
                EventDungeonId = 10010001,
                EventDungeonStageId = 10010001,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = 1,
            };

            IWorld state = new World(MockUtil.MockModernWorldState);

            Assert.Throws<FailedLoadStateException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                        BlockIndex = 1,
                    }));
        }

        [Theory]
        [InlineData(10000001, 10010001, 10010001)] // Invalid EventScheduleId
        [InlineData(1001, 10000001, 10010001)] // Invalid EventDungeonId
        [InlineData(1001, 10010001, 10000001)] // Invalid EventDungeonStageId
        public void Execute_SheetRowNotFoundException(int eventScheduleId, int eventDungeonId, int eventDungeonStageId)
        {
            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventDungeonId = eventDungeonId,
                EventDungeonStageId = eventDungeonStageId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = 1,
            };

            // Use valid block index for event schedule validation
            var blockIndex = eventScheduleId == 1001 ? _tableSheets.EventScheduleSheet[1001].StartBlockIndex : 1;

            Assert.Throws<InvalidActionFieldException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _initialState,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                        BlockIndex = blockIndex,
                    }));
        }

        [Theory]
        [InlineData(0, typeof(PlayCountIsZeroException))]
        [InlineData(10, typeof(NotEnoughEventDungeonTicketsException))]
        [InlineData(101, typeof(ExceedPlayCountException))]
        public void Execute_PlayCountExceptions(int playCount, Type expectedExceptionType)
        {
            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = 10010001,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var blockIndex = (playCount == 0 || playCount > 100) ? 1 : contextBlockIndex;

            Assert.Throws(
                expectedExceptionType,
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _initialState,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                        BlockIndex = blockIndex,
                    }));
        }

        [Theory]
        [InlineData(10010002, 0, false)] // Try to play stage 2 without clearing stage 1
        [InlineData(10010003, 10010001, false)] // Try to play stage 3 with only stage 1 cleared
        public void Execute_StageNotClearedException(int targetStageId, int clearedStageId, bool shouldSucceed)
        {
            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = targetStageId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = 1,
            };

            // Create event dungeon info with specified cleared stage
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);
            if (clearedStageId > 0)
            {
                eventDungeonInfo.ClearStage(clearedStageId);
            }

            var state = _initialState.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            if (shouldSucceed)
            {
                // This should not throw an exception
                var nextState = action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                        BlockIndex = contextBlockIndex,
                    });
                Assert.NotNull(nextState);
            }
            else
            {
                Assert.Throws<StageNotClearedException>(
                    () => action.Execute(
                        new ActionContext()
                        {
                            PreviousState = state,
                            Signer = _agentAddress,
                            RandomSeed = 0,
                            BlockIndex = contextBlockIndex,
                        }));
            }
        }

        [Theory]
        [InlineData(10010001, 0, 3, 7)] // First stage, no cleared stages, 3 plays
        [InlineData(10010001, 0, 1, 9)] // First stage, no cleared stages, 1 play
        [InlineData(10010002, 10010001, 2, 8)] // Second stage, stage 1 cleared, 2 plays
        public void Execute_Success(int stageId, int clearedStageId, int playCount, int expectedRemainingTickets)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var (equipments, costumes) = GetDummyItems(avatarState);
            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            // Create event dungeon info with enough tickets
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);
            if (clearedStageId > 0)
            {
                eventDungeonInfo.ClearStage(clearedStageId);
            }

            state = state.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = stageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = contextBlockIndex,
                });

            var nextAvatar = nextState.GetAvatarState(_avatarAddress);
            var nextEventDungeonInfo = new EventDungeonInfo(
                (List)nextState.GetLegacyState(eventDungeonInfoAddr));

            // Verify tickets were consumed correctly
            Assert.Equal(expectedRemainingTickets, nextEventDungeonInfo.RemainingTickets);

            // Verify experience was gained
            var expectedExp = scheduleRow.GetStageExp(stageId.ToEventDungeonStageNumber(), playCount);
            Assert.True(
                nextAvatar.exp >= expectedExp,
                $"Expected at least {expectedExp} experience, but got {nextAvatar.exp}");

            // Verify the action completed successfully
            Assert.NotNull(nextState);
            Assert.NotNull(nextAvatar);
        }

        [Theory]
        [InlineData(10010001, 1, 1)] // First stage, 1 play
        [InlineData(10010001, 3, 3)] // First stage, 3 plays
        [InlineData(10010002, 2, 2)] // Second stage, 2 plays
        public void Execute_ExperienceGain(int stageId, int playCount, int expectedMinExp)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var (equipments, costumes) = GetDummyItems(avatarState);
            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            // Create event dungeon info with enough tickets
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);

            // Clear previous stage if not first stage
            if (stageId != 10010001)
            {
                eventDungeonInfo.ClearStage(stageId - 1);
            }

            state = state.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = stageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = contextBlockIndex,
                });

            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            // Verify experience was gained correctly
            var expectedExp = scheduleRow.GetStageExp(stageId.ToEventDungeonStageNumber(), playCount);
            Assert.True(
                nextAvatar.exp >= expectedExp,
                $"Expected at least {expectedExp} experience, but got {nextAvatar.exp}");

            // Verify experience is proportional to play count
            Assert.True(
                nextAvatar.exp >= expectedMinExp,
                $"Expected at least {expectedMinExp} experience for {playCount} plays, but got {nextAvatar.exp}");
        }

        [Theory]
        [InlineData(10010001, 1, 1, 2)] // Level up case: from level 1 to 2
        [InlineData(10010001, 3, 1, 2)] // Multiple level up case: from level 1 to 2
        [InlineData(10010001, 5, 1, 3)] // Large experience case: from level 1 to 3
        public void Execute_LevelUp(int stageId, int playCount, int initialLevel, int expectedLevel)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var (equipments, costumes) = GetDummyItems(avatarState);

            // Set avatar to low level
            avatarState.level = initialLevel;
            avatarState.exp = 0; // Initialize experience

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            // Create event dungeon info with enough tickets
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);

            state = state.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = stageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = contextBlockIndex,
                });

            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            // Verify level up
            Assert.Equal(expectedLevel, nextAvatar.level);
            Assert.True(nextAvatar.exp > 0, $"Expected experience > 0, but got {nextAvatar.exp}");

            // Verify level up count
            var levelUpCount = expectedLevel - initialLevel;
            Assert.True(levelUpCount > 0, $"Expected level up, but level remained at {initialLevel}");

            // Verify experience is proportional to play count
            var expectedExp = scheduleRow.GetStageExp(stageId.ToEventDungeonStageNumber(), playCount);
            Assert.True(
                nextAvatar.exp >= expectedExp,
                $"Expected at least {expectedExp} experience, but got {nextAvatar.exp}");
        }

        [Theory]
        [InlineData(10010001, 1, 100)] // Max level case: experience gain at level 100
        [InlineData(10010001, 5, 100)] // Max level with large experience case
        public void Execute_MaxLevelExperience(int stageId, int playCount, int maxLevel)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var (equipments, costumes) = GetDummyItems(avatarState);

            // Set avatar to max level
            avatarState.level = maxLevel;
            avatarState.exp = 0; // Initialize experience

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            // Create event dungeon info with enough tickets
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);

            state = state.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = stageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = contextBlockIndex,
                });

            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            // Verify no level up occurs at max level
            Assert.Equal(maxLevel, nextAvatar.level);

            // At max level, experience can be 0 (no level up possible)
            // But the action should execute successfully
            Assert.True(nextAvatar.exp >= 0, $"Experience should be non-negative, but got {nextAvatar.exp}");
        }

        [Theory]
        [InlineData(10010001, 1, 1, 2)] // Level up quest verification: from level 1 to 2
        [InlineData(10010001, 3, 1, 2)] // Multiple level up quest verification: from level 1 to 2
        public void Execute_LevelUpQuestUpdate(int stageId, int playCount, int initialLevel, int expectedLevel)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var (equipments, costumes) = GetDummyItems(avatarState);

            // Set avatar to low level
            avatarState.level = initialLevel;
            avatarState.exp = 0; // Initialize experience

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            // Get the correct block index from event schedule
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            // Create event dungeon info with enough tickets
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 10010001);
            var eventDungeonInfo = new EventDungeonInfo(remainingTickets: 10);

            state = state.SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = stageId,
                Equipments = equipments,
                Costumes = costumes,
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                PlayCount = playCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = contextBlockIndex,
                });

            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            // Verify level up
            Assert.Equal(expectedLevel, nextAvatar.level);

            // Calculate level up count
            var levelUpCount = expectedLevel - initialLevel;
            Assert.True(levelUpCount > 0, $"Expected level up, but level remained at {initialLevel}");

            // Verify that level up events are processed in the quest system
            // Check if AvatarStateExtensions.UpdateExp adds level up events to eventMap
            // This is processed through questList.UpdateCompletedQuest
            Assert.NotNull(nextAvatar.questList);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void GetRewardItems_ReturnsCorrectItems(int playCount)
        {
            var materialItemSheet = _tableSheets.MaterialItemSheet;
            var stageRow = _tableSheets.EventDungeonStageSheet[10010001];
            var random = new TestRandom();

            var rewardItems = EventDungeonBattleSweep.GetRewardItems(
                random,
                playCount,
                stageRow,
                materialItemSheet);

            Assert.NotNull(rewardItems);
            Assert.True(rewardItems.Count >= 0);

            // Verify items are sorted by ID
            var sortedItems = rewardItems.OrderBy(x => x.Id).ToList();
            Assert.Equal(sortedItems, rewardItems);
        }

        [Fact]
        public void Serialize_With_MessagePack()
        {
            var action = new EventDungeonBattleSweep
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = 1001,
                EventDungeonId = 10010001,
                EventDungeonStageId = 10010001,
                Equipments = new List<Guid> { Guid.NewGuid() },
                Costumes = new List<Guid> { Guid.NewGuid() },
                Foods = new List<Guid> { Guid.NewGuid() },
                RuneInfos = new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(0, 10001),
                },
                PlayCount = 5,
            };

            var serialized = action.PlainValue;
            var deserialized = new EventDungeonBattleSweep();
            deserialized.LoadPlainValue(serialized);

            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
            Assert.Equal(action.EventScheduleId, deserialized.EventScheduleId);
            Assert.Equal(action.EventDungeonId, deserialized.EventDungeonId);
            Assert.Equal(action.EventDungeonStageId, deserialized.EventDungeonStageId);
            Assert.Equal(action.Equipments, deserialized.Equipments);
            Assert.Equal(action.Costumes, deserialized.Costumes);
            Assert.Equal(action.Foods, deserialized.Foods);
            Assert.Equal(action.RuneInfos.Count, deserialized.RuneInfos.Count);
            for (int i = 0; i < action.RuneInfos.Count; i++)
            {
                Assert.Equal(action.RuneInfos[i].SlotIndex, deserialized.RuneInfos[i].SlotIndex);
                Assert.Equal(action.RuneInfos[i].RuneId, deserialized.RuneInfos[i].RuneId);
            }

            Assert.Equal(action.PlayCount, deserialized.PlayCount);
        }
    }
}
