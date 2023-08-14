namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class EventDungeonBattleV5Test
    {
        private readonly Currency _ncgCurrency;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private IWorld _initialWorld;

        public EventDungeonBattleV5Test()
        {
            _initialWorld = new MockWorld();

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                GoldCurrencyState.Address,
                new GoldCurrencyState(_ncgCurrency).Serialize());
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialWorld = LegacyModule.SetState(
                    _initialWorld, Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            var inventoryAddr = _avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddr = _avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddr = _avatarAddress.Derive(LegacyQuestListKey);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                new PrivateKey().ToAddress()
            )
            {
                level = 100,
            };

            _initialWorld = LegacyModule.SetState(_initialWorld, _agentAddress, agentState.Serialize());
            _initialWorld = LegacyModule.SetState(_initialWorld, _avatarAddress, avatarState.SerializeV2());
            _initialWorld = LegacyModule.SetState(_initialWorld, inventoryAddr, avatarState.inventory.Serialize());
            _initialWorld = LegacyModule.SetState(_initialWorld, worldInformationAddr, avatarState.worldInformation.Serialize());
            _initialWorld = LegacyModule.SetState(_initialWorld, questListAddr, avatarState.questList.Serialize());
            _initialWorld = LegacyModule.SetState(_initialWorld, gameConfigState.address, gameConfigState.Serialize());
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
            var nextWorld = Execute(
                _initialWorld,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextAccount.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextWorld = Execute(
                _initialWorld,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            eventDungeonInfo =
                new EventDungeonInfo(nextAccount.GetState(eventDungeonInfoAddr));
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
            var previousWorld = _initialWorld;
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
            previousWorld = LegacyModule.SetState(
                previousWorld,
                Addresses.GetSheetAddress<EventScheduleSheet>(),
                sb.ToString().Serialize());

            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousWorld = LegacyModule.SetState(
                previousWorld,
                eventDungeonInfoAddr,
                eventDungeonInfo.Serialize());

            Assert.True(LegacyModule.GetSheet<EventScheduleSheet>(_initialWorld)
                .TryGetValue(eventScheduleId, out var newScheduleRow));
            var ncgHas = newScheduleRow.GetDungeonTicketCost(
                numberOfTicketPurchases,
                _ncgCurrency);
            if (ncgHas.Sign > 0)
            {
                previousWorld = LegacyModule.MintAsset(previousWorld, context, _agentAddress, ncgHas);
            }

            var nextWorld = Execute(
                previousWorld,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                buyTicketIfNeeded: true,
                blockIndex: scheduleRow.StartBlockIndex);
            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            var nextEventDungeonInfoList =
                (Bencodex.Types.List)nextAccount.GetState(eventDungeonInfoAddr)!;
            Assert.Equal(
                numberOfTicketPurchases + 1,
                nextEventDungeonInfoList[2].ToInteger());
            Assert.True(
                LegacyModule.TryGetGoldBalance(
                    nextWorld,
                    _agentAddress,
                    _ncgCurrency,
                    out FungibleAssetValue balance
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
            int eventDungeonStageId) =>
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialWorld,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId));

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
                    _initialWorld,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex));
            contextBlockIndex = scheduleRow.DungeonEndBlockIndex + 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialWorld,
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
                    _initialWorld,
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
                    _initialWorld,
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
            var previousStates = _initialWorld;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo();
            previousStates = LegacyModule
                .SetState(previousStates, eventDungeonInfoAddr, eventDungeonInfo.Serialize());
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
            var previousStates = _initialWorld;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousStates = LegacyModule
                .SetState(previousStates, eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var ncgHas = scheduleRow.GetDungeonTicketCost(
                numberOfTicketPurchases,
                _ncgCurrency) - 1 * _ncgCurrency;
            if (ncgHas.Sign > 0)
            {
                previousStates = LegacyModule.MintAsset(previousStates, context, _agentAddress, ncgHas);
            }

            Assert.Throws<InsufficientBalanceException>(() =>
                Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    buyTicketIfNeeded: true,
                    blockIndex: scheduleRow.StartBlockIndex));
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
                    _initialWorld,
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
            var previousAccount = _initialWorld;
            previousAccount = LegacyModule.MintAsset(previousAccount, context, _agentAddress, 99999 * _ncgCurrency);
            IWorld previousWorld = new MockWorld(previousAccount);

            var unlockRuneSlot = new UnlockRuneSlot()
            {
                AvatarAddress = _avatarAddress,
                SlotIndex = 1,
            };

            previousWorld = unlockRuneSlot.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousState = previousWorld,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            Assert.Throws(exception, () =>
                Execute(
                    previousWorld,
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
            int eventScheduleId = 1001;
            int eventDungeonId = 10010001;
            int eventDungeonStageId = 10010001;
            var csv = $@"id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_ticket_price,dungeon_ticket_additional_price,dungeon_exp_seed_value,recipe_end_block_index
            1001,2022 Summer Event,{ActionObsoleteConfig.V100301ExecutedBlockIndex},{ActionObsoleteConfig.V100301ExecutedBlockIndex + 100},5,7200,5,2,1,5018000";
            var previousStates = _initialWorld;
            previousStates =
                LegacyModule.SetState(
                    previousStates,
                    Addresses.GetSheetAddress<EventScheduleSheet>(),
                    csv.Serialize());
            var previousWorld = new MockWorld(previousStates);
            var sheet = new EventScheduleSheet();
            sheet.Set(csv);
            Assert.True(sheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var nextWorld = Execute(
                previousWorld,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextAccount.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextWorld = Execute(
                previousWorld,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            eventDungeonInfo =
                new EventDungeonInfo(nextAccount.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }

        private IWorld Execute(
            IWorld previousWorld,
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
            var previousAccount = previousWorld.GetAccount(ReservedAddresses.LegacyAccount);
            var previousAvatarState = AvatarModule.GetAvatarStateV2(previousWorld, _avatarAddress);
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
                    new RuneSlotInfo(slotIndex, runeId),
                    new RuneSlotInfo(slotIndex2, runeId2),
                },
                BuyTicketIfNeeded = buyTicketIfNeeded,
            };

            var nextWorld = action.Execute(new ActionContext
            {
                PreviousState = previousWorld,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = blockIndex,
            });
            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

            Assert.True(LegacyModule.GetSheet<EventScheduleSheet>(nextWorld).TryGetValue(
                eventScheduleId,
                out var scheduleRow));
            var nextAvatarState = AvatarModule.GetAvatarStateV2(nextWorld, _avatarAddress);
            var expectExp = scheduleRow.GetStageExp(
                eventDungeonStageId.ToEventDungeonStageNumber());
            Assert.Equal(
                previousAvatarState.exp + expectExp,
                nextAvatarState.exp);

            return nextWorld;
        }
    }
}
