namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lib9c.Tests.Util;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonBattleTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta[] _initialStatesArray;
        private readonly Currency _ncgCurrency;

        public EventDungeonBattleTest()
        {
            IAccountStateDelta initialStatesWithAvatarStateV1;
            IAccountStateDelta initialStatesWithAvatarStateV2;
            (
                _tableSheets,
                _agentAddress,
                _avatarAddress,
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates(
                avatarLevel: 100);
            _initialStatesArray = new[]
            {
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2,
            };
            _ncgCurrency = initialStatesWithAvatarStateV1.GetGoldCurrency();
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
            foreach (var initialStates in _initialStatesArray)
            {
                var nextStates = Execute(
                    initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex);
                var ediAddr =
                    EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
                var edi =
                    new EventDungeonInfo(nextStates.GetVersionedState(ediAddr));
                Assert.Equal(
                    scheduleRow.DungeonTicketsMax - 1,
                    edi.RemainingTickets);

                contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
                nextStates = Execute(
                    initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex);
                edi =
                    new EventDungeonInfo(nextStates.GetVersionedState(ediAddr));
                Assert.Equal(
                    scheduleRow.DungeonTicketsMax - 1,
                    edi.RemainingTickets);
            }
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
            foreach (var initialStates in _initialStatesArray)
            {
                var previousStates = initialStates;
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
                previousStates = previousStates.SetState(
                    Addresses.GetSheetAddress<EventScheduleSheet>(),
                    sb.ToString().Serialize());

                var ediAddr =
                    EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
                var edi = new EventDungeonInfo(
                    remainingTickets: 0,
                    numberOfTicketPurchases: numberOfTicketPurchases);
                previousStates = previousStates.SetState(
                    ediAddr,
                    edi.Serialize());

                Assert.True(previousStates.GetSheet<EventScheduleSheet>()
                    .TryGetValue(eventScheduleId, out var newScheduleRow));
                var ncgHas = newScheduleRow.GetDungeonTicketCost(
                    numberOfTicketPurchases,
                    _ncgCurrency);
                previousStates = previousStates.MintAsset(_agentAddress, ncgHas);

                var nextStates = Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    buyTicketIfNeeded: true,
                    blockIndex: scheduleRow.StartBlockIndex);
                var nextEventDungeonInfoList =
                    (Bencodex.Types.List)nextStates.GetVersionedState(ediAddr)!;
                Assert.Equal(
                    numberOfTicketPurchases + 1,
                    nextEventDungeonInfoList[2].ToInteger());
                Assert.True(
                    nextStates.TryGetGoldBalance(
                        _agentAddress,
                        _ncgCurrency,
                        out FungibleAssetValue balance
                    )
                );
                Assert.Equal(0 * _ncgCurrency, balance);
            }
        }

        [Theory]
        [InlineData(10000001, 10010001, 10010001)]
        [InlineData(10010001, 10010001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventScheduleId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<InvalidActionFieldException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId));
            }
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
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<InvalidActionFieldException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        blockIndex: contextBlockIndex));
            }

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex + 1;
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<InvalidActionFieldException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        blockIndex: contextBlockIndex));
            }
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
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<InvalidActionFieldException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        blockIndex: scheduleRow.StartBlockIndex));
            }
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
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<InvalidActionFieldException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        blockIndex: scheduleRow.StartBlockIndex));
            }
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_NotEnoughEventDungeonTicketsException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            foreach (var initialStates in _initialStatesArray)
            {
                var previousStates = initialStates;
                var ediAddr =
                    EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
                var edi = new EventDungeonInfo();
                previousStates = previousStates
                    .SetState(ediAddr, edi.Serialize());
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
            foreach (var initialStates in _initialStatesArray)
            {
                var previousStates = initialStates;
                var ediAddr =
                    EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
                var edi = new EventDungeonInfo(
                    remainingTickets: 0,
                    numberOfTicketPurchases: numberOfTicketPurchases);
                previousStates = previousStates
                    .SetState(ediAddr, edi.Serialize());

                Assert.True(_tableSheets.EventScheduleSheet
                    .TryGetValue(eventScheduleId, out var scheduleRow));
                var ncgHas = scheduleRow.GetDungeonTicketCost(
                    numberOfTicketPurchases,
                    _ncgCurrency) - 1 * _ncgCurrency;
                previousStates = previousStates.MintAsset(_agentAddress, ncgHas);

                Assert.Throws<InsufficientBalanceException>(() =>
                    Execute(
                        previousStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        buyTicketIfNeeded: true,
                        blockIndex: scheduleRow.StartBlockIndex));
            }
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
            foreach (var initialStates in _initialStatesArray)
            {
                Assert.Throws<StageNotClearedException>(() =>
                    Execute(
                        initialStates,
                        eventScheduleId,
                        eventDungeonId,
                        eventDungeonStageId,
                        blockIndex: scheduleRow.StartBlockIndex));
            }
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void Execute_DuplicatedException(
            int slotIndex,
            int runeId,
            int slotIndex2,
            int runeId2,
            Type exception)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(1001, out var scheduleRow));
            foreach (var initialStates in _initialStatesArray)
            {
                var prevStates =
                    initialStates.MintAsset(_agentAddress, 99999 * _ncgCurrency);

                var unlockRuneSlot = new UnlockRuneSlot()
                {
                    AvatarAddress = _avatarAddress,
                    SlotIndex = 1,
                };

                prevStates = unlockRuneSlot.Execute(new ActionContext
                {
                    BlockIndex = 1,
                    PreviousStates = prevStates,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                });

                Assert.Throws(exception, () =>
                    Execute(
                        prevStates,
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
        }

        [Fact]
        public void Execute_V100301()
        {
            const int eventScheduleId = 1001;
            const int eventDungeonId = 10010001;
            const int eventDungeonStageId = 10010001;
            var csv =
                $@"id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_ticket_price,dungeon_ticket_additional_price,dungeon_exp_seed_value,recipe_end_block_index
            1001,2022 Summer Event,{BlockPolicySource.V100301ExecutedBlockIndex},{BlockPolicySource.V100301ExecutedBlockIndex + 100},5,7200,5,2,1,5018000";
            foreach (var initialStates in _initialStatesArray)
            {
                var prevStates =
                    initialStates.SetState(
                        Addresses.GetSheetAddress<EventScheduleSheet>(),
                        csv.Serialize());
                var sheet = new EventScheduleSheet();
                sheet.Set(csv);
                Assert.True(sheet.TryGetValue(eventScheduleId, out var scheduleRow));
                var contextBlockIndex = scheduleRow.StartBlockIndex;
                var nextStates = Execute(
                    prevStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex);
                var ediAddr =
                    EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
                var edi =
                    new EventDungeonInfo(nextStates.GetVersionedState(ediAddr));
                Assert.Equal(
                    scheduleRow.DungeonTicketsMax - 1,
                    edi.RemainingTickets);

                contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
                nextStates = Execute(
                    prevStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex);
                edi =
                    new EventDungeonInfo(nextStates.GetVersionedState(ediAddr));
                Assert.Equal(
                    scheduleRow.DungeonTicketsMax - 1,
                    edi.RemainingTickets);
            }
        }

        private IAccountStateDelta Execute(
            IAccountStateDelta previousStates,
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
            Assert.True(previousStates.TryGetAvatarStateV2(
                _agentAddress,
                _avatarAddress,
                out var prevAvatarState,
                out _));
            var equipments =
                Doomfist.GetAllParts(_tableSheets, prevAvatarState.level);
            foreach (var equipment in equipments)
            {
                prevAvatarState.inventory.AddItem(equipment, iLock: null);
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
                RuneInfos = new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(slotIndex, runeId),
                    new RuneSlotInfo(slotIndex2, runeId2),
                },
                BuyTicketIfNeeded = buyTicketIfNeeded,
            };

            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            Assert.True(nextStates.GetSheet<EventScheduleSheet>().TryGetValue(
                eventScheduleId,
                out var scheduleRow));
            var nextAvatarState = nextStates.GetAvatarStateV2(_avatarAddress);
            var expectExp = scheduleRow.GetStageExp(
                eventDungeonStageId.ToEventDungeonStageNumber());
            Assert.Equal(
                prevAvatarState.exp + expectExp,
                nextAvatarState.exp);
            var ediAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var ediVal = nextStates.GetVersionedState(
                ediAddr,
                out var moniker,
                out var version);
            Assert.NotNull(ediVal);
            Assert.Equal(IEventDungeonInfo.Moniker, moniker);
            Assert.Equal(IEventDungeonInfo.Version, version);
            var edi = new EventDungeonInfo(ediVal);

            return nextStates;
        }
    }
}
