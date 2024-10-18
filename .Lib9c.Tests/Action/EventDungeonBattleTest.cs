namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonBattleTest
    {
        private readonly Currency _ncgCurrency;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private IWorld _initialStates;

        public EventDungeonBattleTest()
        {
            _initialStates = new World(MockUtil.MockModernWorldState);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _initialStates = _initialStates.SetLegacyState(
                GoldCurrencyState.Address,
                new GoldCurrencyState(_ncgCurrency).Serialize());
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialStates = _initialStates
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().Address;
            _avatarAddress = _agentAddress.Derive("avatar");

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address
            );
            avatarState.level = 100;

            _initialStates = _initialStates
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Success_Within_Event_Period(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetLegacyState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetLegacyState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001, 0, 0, 0)]
        [InlineData(1001, 10010001, 10010001, 1, 1, 1)]
        [InlineData(1001, 10010001, 10010001, int.MaxValue, int.MaxValue, int.MaxValue - 1)]
        public void Execute_Success_With_Ticket_Purchase(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            int dungeonTicketPrice,
            int dungeonTicketAdditionalPrice,
            int numberOfTicketPurchases)
        {
            var context = new ActionContext();
            var previousStates = _initialStates;
            var scheduleSheet = _tableSheets.EventScheduleSheet;
            Assert.True(scheduleSheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var sb = new StringBuilder();
            sb.AppendLine(
                "id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_exp_seed_value,recipe_end_block_index,dungeon_ticket_price,dungeon_ticket_additional_price");
            sb.AppendLine(
                $"{eventScheduleId}" +
                $",\"2022 Summer Event\"" +
                $",{scheduleRow.StartBlockIndex}" +
                $",{scheduleRow.DungeonEndBlockIndex}" +
                $",{scheduleRow.DungeonTicketsMax}" +
                $",{scheduleRow.DungeonTicketsResetIntervalBlockRange}" +
                $",{dungeonTicketPrice}" +
                $",{dungeonTicketAdditionalPrice}" +
                $",{scheduleRow.DungeonExpSeedValue}" +
                $",{scheduleRow.RecipeEndBlockIndex}");
            previousStates = previousStates.SetLegacyState(
                Addresses.GetSheetAddress<EventScheduleSheet>(),
                sb.ToString().Serialize());

            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousStates = previousStates.SetLegacyState(
                eventDungeonInfoAddr,
                eventDungeonInfo.Serialize());

            Assert.True(previousStates.GetSheet<EventScheduleSheet>()
                .TryGetValue(eventScheduleId, out var newScheduleRow));
            var ncgHas = newScheduleRow.GetDungeonTicketCost(
                numberOfTicketPurchases,
                _ncgCurrency);
            if (ncgHas.Sign > 0)
            {
                previousStates = previousStates.MintAsset(context, _agentAddress, ncgHas);
            }

            var nextStates = Execute(
                previousStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                true,
                scheduleRow.StartBlockIndex);
            var nextEventDungeonInfoList =
                (Bencodex.Types.List)nextStates.GetLegacyState(eventDungeonInfoAddr)!;
            Assert.Equal(
                numberOfTicketPurchases + 1,
                nextEventDungeonInfoList[2].ToInteger());
            Assert.True(
                nextStates.TryGetGoldBalance(
                    _agentAddress,
                    _ncgCurrency,
                    out var balance
                )
            );
            Assert.Equal(0 * _ncgCurrency, balance);
        }

        [Theory]
        [InlineData(10000001, 10010001, 10010001)]
        [InlineData(10010001, 10010001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventScheduleId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_ContextBlockIndex(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex - 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex));
            contextBlockIndex = scheduleRow.DungeonEndBlockIndex + 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10020001, 10010001)]
        [InlineData(1001, 1001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventDungeonId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10020001)]
        [InlineData(1001, 10010001, 1001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventDungeonStageId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_NotEnoughEventDungeonTicketsException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            var previousStates = _initialStates;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo();
            previousStates = previousStates
                .SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<NotEnoughEventDungeonTicketsException>(() =>
                Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001, 0)]
        [InlineData(1001, 10010001, 10010001, int.MaxValue - 1)]
        public void Execute_Throw_InsufficientBalanceException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            int numberOfTicketPurchases)
        {
            var context = new ActionContext();
            var previousStates = _initialStates;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousStates = previousStates
                .SetLegacyState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var ncgHas = scheduleRow.GetDungeonTicketCost(
                numberOfTicketPurchases,
                _ncgCurrency) - 1 * _ncgCurrency;
            if (ncgHas.Sign > 0)
            {
                previousStates = previousStates.MintAsset(context, _agentAddress, ncgHas);
            }

            Assert.Throws<InsufficientBalanceException>(() =>
                Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    true,
                    scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010002)]
        public void Execute_Throw_StageNotClearedException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<StageNotClearedException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void Execute_DuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(1001, out var scheduleRow));

            var context = new ActionContext();
            _initialStates = _initialStates.MintAsset(context, _agentAddress, 99999 * _ncgCurrency);

            var unlockRuneSlot = new UnlockRuneSlot()
            {
                AvatarAddress = _avatarAddress,
                SlotIndex = 1,
            };

            _initialStates = unlockRuneSlot.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousState = _initialStates,
                Signer = _agentAddress,
                RandomSeed = 0,
            });

            Assert.Throws(exception, () =>
                Execute(
                    _initialStates,
                    1001,
                    10010001,
                    10010001,
                    false,
                    scheduleRow.StartBlockIndex,
                    slotIndex,
                    runeId,
                    slotIndex2,
                    runeId2));
        }

        [Fact]
        public void Execute_V100301()
        {
            var eventScheduleId = 1001;
            var eventDungeonId = 10010001;
            var eventDungeonStageId = 10010001;
            var csv = $@"id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_ticket_price,dungeon_ticket_additional_price,dungeon_exp_seed_value,recipe_end_block_index
            1001,2022 Summer Event,{ActionObsoleteConfig.V100301ExecutedBlockIndex},{ActionObsoleteConfig.V100301ExecutedBlockIndex + 100},5,7200,5,2,1,5018000";
            _initialStates =
                _initialStates.SetLegacyState(
                    Addresses.GetSheetAddress<EventScheduleSheet>(),
                    csv.Serialize());
            var sheet = new EventScheduleSheet();
            sheet.Set(csv);
            Assert.True(sheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetLegacyState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetLegacyState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void CheckRewardItems(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;

            var avatarState = _initialStates.GetAvatarState(_avatarAddress);
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            var state = _initialStates.SetAvatarState(_avatarAddress, avatarState);
            var action = new EventDungeonBattle
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventDungeonId = eventDungeonId,
                EventDungeonStageId = eventDungeonStageId,
                Equipments = equipments
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = contextBlockIndex,
            });
            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            var stageSheet = nextState.GetSheet<EventDungeonStageSheet>();
            if (!stageSheet.TryGetValue(eventDungeonStageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(nameof(EventDungeonStageSheet), eventDungeonStageId);
            }

            var materialItemSheet = nextState.GetSheet<MaterialItemSheet>();
            var circleRow = materialItemSheet.Values.First(i => i.ItemSubType == ItemSubType.Circle);
            var circleRewardData = stageRow.Rewards.FirstOrDefault(reward => reward.ItemId == circleRow.Id);
            if (circleRewardData != null)
            {
                var circles = nextAvatar.inventory.Items.Where(x => x.item.Id == circleRow.Id);
                Assert.All(circles, x => Assert.True(x.item is TradableMaterial));
            }
        }

        private IWorld Execute(
            IWorld previousStates,
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            bool buyTicketIfNeeded = false,
            long blockIndex = 0,
            int slotIndex = 0,
            int runeId = 10002,
            int slotIndex2 = 1,
            int runeId2 = 30001)
        {
            var previousAvatarState = previousStates.GetAvatarState(_avatarAddress);
            var equipments =
                Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment, iLock: null);
            }

            var action = new EventDungeonBattle
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventDungeonId = eventDungeonId,
                EventDungeonStageId = eventDungeonStageId,
                Equipments = equipments
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>()
                {
                    new (slotIndex, runeId),
                    new (slotIndex2, runeId2),
                },
                BuyTicketIfNeeded = buyTicketIfNeeded,
            };

            var nextStates = action.Execute(new ActionContext
            {
                PreviousState = previousStates,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = blockIndex,
            });

            Assert.True(nextStates.GetSheet<EventScheduleSheet>().TryGetValue(
                eventScheduleId,
                out var scheduleRow));
            var nextAvatarState = nextStates.GetAvatarState(_avatarAddress);
            var expectExp = scheduleRow.GetStageExp(
                eventDungeonStageId.ToEventDungeonStageNumber());
            Assert.Equal(
                previousAvatarState.exp + expectExp,
                nextAvatarState.exp);

            return nextStates;
        }
    }
}
