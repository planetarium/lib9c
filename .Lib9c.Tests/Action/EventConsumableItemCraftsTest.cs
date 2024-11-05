namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventConsumableItemCraftsTest
    {
        private readonly IWorld _initialStates;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public EventConsumableItemCraftsTest()
        {
            _initialStates = new World(MockUtil.MockModernWorldState);
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

            var allSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var addr = CombinationSlotState.DeriveAddress(_avatarAddress, i);
                allSlotState.AddSlot(addr, i);
            }

            _initialStates = _initialStates
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1001, 10010001, 0)]
        public void Execute_Success(
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            Assert.True(_tableSheets.EventScheduleSheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.RecipeEndBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
        }

        private void Execute(
            IWorld previousStates,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex,
            long blockIndex = 0)
        {
            var previousAvatarState = previousStates.GetAvatarState(_avatarAddress);

            var recipeSheet = previousStates.GetSheet<EventConsumableItemRecipeSheet>();
            Assert.True(
                recipeSheet.TryGetValue(
                    eventConsumableItemRecipeId,
                    out var recipeRow));
            var materialItemSheet = previousStates.GetSheet<MaterialItemSheet>();
            foreach (var materialInfo in recipeRow.Materials)
            {
                Assert.True(
                    materialItemSheet.TryGetValue(
                        materialInfo.Id,
                        out var materialRow));
                var material =
                    ItemFactory.CreateItem(materialRow, new TestRandom());
                previousAvatarState.inventory.AddItem(material, materialInfo.Count);
            }

            var worldSheet = previousStates.GetSheet<WorldSheet>();
            previousAvatarState.worldInformation = new WorldInformation(
                blockIndex,
                worldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            previousStates = previousStates
                .SetAvatarState(_avatarAddress, previousAvatarState);

            previousStates.TryGetActionPoint(_avatarAddress, out var previousActionPoint);
            var previousResultConsumableCount =
                previousAvatarState.inventory.Equipments
                    .Count(e => e.Id == recipeRow.ResultConsumableItemId);
            var previousMailCount = previousAvatarState.mailBox.Count;

            var action = new EventConsumableItemCrafts
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventConsumableItemRecipeId = eventConsumableItemRecipeId,
                SlotIndex = slotIndex,
            };

            var nextStates = action.Execute(
                new ActionContext
                {
                    PreviousState = previousStates,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                });

            var allCombinationSlotState = nextStates.GetAllCombinationSlotState(_avatarAddress);
            var slotState = allCombinationSlotState.GetSlot(slotIndex);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = nextStates.GetAvatarState(_avatarAddress);
            if (nextStates.TryGetActionPoint(_avatarAddress, out var nextAp))
            {
                Assert.Equal(
                    previousActionPoint - recipeRow.RequiredActionPoint,
                    nextAp);
            }

            Assert.Equal(
                previousMailCount + 1,
                nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultConsumableCount + 1,
                nextAvatarState.inventory.Consumables
                    .Count(e => e.Id == recipeRow.ResultConsumableItemId));
        }
    }
}
