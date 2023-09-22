namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
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
            _initialStates = new MockWorld();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialStates = LegacyModule.SetState(
                    _initialStates,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");

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

            _initialStates = AgentModule.SetAgentState(_initialStates, _agentAddress, agentState);
            _initialStates = AvatarModule.SetAvatarState(
                _initialStates,
                _avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);
            _initialStates = LegacyModule.SetState(
                _initialStates,
                gameConfigState.address,
                gameConfigState.Serialize());

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var addr = CombinationSlotState.DeriveAddress(_avatarAddress, i);
                const int unlock = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                _initialStates = LegacyModule.SetState(
                    _initialStates,
                    addr,
                    new CombinationSlotState(addr, unlock).Serialize());
            }
        }

        [Theory]
        [InlineData(1001, 10010001, 0)]
        public void Execute_Success(
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var world = _initialStates;
            Execute(
                world,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.RecipeEndBlockIndex;
            Execute(
                world,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
        }

        private void Execute(
            IWorld previousWorld,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex,
            long blockIndex = 0)
        {
            var previousAvatarState = AvatarModule.GetAvatarState(previousWorld, _avatarAddress);

            var recipeSheet = LegacyModule.GetSheet<EventConsumableItemRecipeSheet>(previousWorld);
            Assert.True(recipeSheet.TryGetValue(
                eventConsumableItemRecipeId,
                out var recipeRow));
            var materialItemSheet = LegacyModule.GetSheet<MaterialItemSheet>(previousWorld);
            foreach (var materialInfo in recipeRow.Materials)
            {
                Assert.True(materialItemSheet.TryGetValue(
                    materialInfo.Id,
                    out var materialRow));
                var material =
                    ItemFactory.CreateItem(materialRow, new TestRandom());
                previousAvatarState.inventory.AddItem(material, materialInfo.Count);
            }

            var worldSheet = LegacyModule.GetSheet<WorldSheet>(previousWorld);
            previousAvatarState.worldInformation = new WorldInformation(
                blockIndex,
                worldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            previousWorld = AvatarModule.SetAvatarState(
                previousWorld,
                _avatarAddress,
                previousAvatarState,
                false,
                true,
                true,
                false);

            var previousActionPoint = previousAvatarState.actionPoint;
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

            var nextWorld = action.Execute(new ActionContext
            {
                PreviousState = previousWorld,
                Signer = _agentAddress,
                RandomSeed = 0,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);
            var slotState = LegacyModule.GetCombinationSlotState(nextWorld, _avatarAddress, slotIndex);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = AvatarModule.GetAvatarState(nextWorld, _avatarAddress);
            Assert.Equal(
                previousActionPoint - recipeRow.RequiredActionPoint,
                nextAvatarState.actionPoint);
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
