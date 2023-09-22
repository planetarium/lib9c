namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Exceptions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;
    using static SerializeKeys;

    public class EventMaterialItemCraftsTest
    {
        private readonly IWorld _initialStates;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public EventMaterialItemCraftsTest()
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

        public static IEnumerable<object[]> GetExecuteSuccessMemberData()
        {
            yield return new object[]
            {
                1002,
                10020001,
                new Dictionary<int, int>
                {
                    [700000] = 5,
                    [700001] = 5,
                    [700002] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020001,
                new Dictionary<int, int>
                {
                    [700102] = 5,
                    [700104] = 5,
                    [700106] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020001,
                new Dictionary<int, int>
                {
                    [700202] = 10,
                    [700204] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020002,
                new Dictionary<int, int>
                {
                    [700108] = 5,
                    [700206] = 5,
                },
            };
        }

        public static IEnumerable<object[]> GetExecuteInvalidMaterialCountExceptionMemberData()
        {
            yield return new object[]
            {
                1002,
                10020001,
                new Dictionary<int, int>
                {
                    [700102] = 5,
                    [700204] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020002,
                new Dictionary<int, int>
                {
                    [700108] = 10,
                    [700206] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020002,
                new Dictionary<int, int>
                {
                    [700102] = 10,
                    [700104] = 5,
                },
            };
            yield return new object[]
            {
                1002,
                10020002,
                new Dictionary<int, int>
                {
                    [700102] = 10,
                    [700206] = 5,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteSuccessMemberData))]
        public void Execute_Success(
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            Execute(
                new MockWorld(_initialStates),
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.RecipeEndBlockIndex;
            Execute(
                new MockWorld(_initialStates),
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse,
                contextBlockIndex);
        }

        [Theory]
        [MemberData(nameof(GetExecuteInvalidMaterialCountExceptionMemberData))]
        public void Execute_InvalidMaterialCountException(
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(1002, out var scheduleRow));
            Assert.Throws<InvalidMaterialCountException>(() =>
            {
                Execute(
                    new MockWorld(_initialStates),
                    eventScheduleId,
                    eventMaterialItemRecipeId,
                    materialsToUse,
                    scheduleRow.StartBlockIndex);
            });
        }

        private void Execute(
            IWorld previousStates,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse,
            long blockIndex = 0)
        {
            var previousAvatarState = AvatarModule.GetAvatarState(previousStates, _avatarAddress);

            var recipeSheet = LegacyModule.GetSheet<EventMaterialItemRecipeSheet>(previousStates);
            Assert.True(recipeSheet.TryGetValue(eventMaterialItemRecipeId, out var recipeRow));
            var materialItemSheet = LegacyModule.GetSheet<MaterialItemSheet>(previousStates);
            foreach (var pair in materialsToUse)
            {
                Assert.True(materialItemSheet.TryGetValue(pair.Key, out var materialRow));
                var material = ItemFactory.CreateItem(materialRow, new TestRandom());
                previousAvatarState.inventory.AddItem(material, pair.Value);
            }

            var worldSheet = LegacyModule.GetSheet<WorldSheet>(previousStates);
            previousAvatarState.worldInformation = new WorldInformation(
                blockIndex,
                worldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            previousStates = AvatarModule.SetAvatarState(
                previousStates,
                _avatarAddress,
                previousAvatarState,
                false,
                true,
                true,
                false);

            var previousMaterialCount = previousAvatarState.inventory.Items
                .Where(i => recipeRow.RequiredMaterialsId.Contains(i.item.Id))
                .ToDictionary(i => i.item.Id, i => i.count);
            var previousResultMaterialCount = previousAvatarState.inventory.Items
                .Sum(i => i.item.Id == recipeRow.ResultMaterialItemId ? i.count : 0);
            var previousMailCount = previousAvatarState.mailBox.Count;

            var action = new EventMaterialItemCrafts
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventMaterialItemRecipeId = eventMaterialItemRecipeId,
                MaterialsToUse = materialsToUse,
            };

            var nextStates = action.Execute(new ActionContext
            {
                PreviousState = previousStates,
                Signer = _agentAddress,
                RandomSeed = 0,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            var nextAvatarState = AvatarModule.GetAvatarState(nextStates, _avatarAddress);

            var nextMaterialCount = nextAvatarState.inventory.Items
                .Where(i => recipeRow.RequiredMaterialsId.Contains(i.item.Id))
                .ToDictionary(i => i.item.Id, i => i.count);
            foreach (var item in nextMaterialCount)
            {
                Assert.Equal(previousMaterialCount[item.Key], item.Value);
            }

            Assert.Equal(
                previousResultMaterialCount + recipeRow.ResultMaterialItemCount,
                nextAvatarState.inventory.Items
                    .Sum(e => e.item.Id == recipeRow.ResultMaterialItemId ? e.count : 0));
            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<MaterialCraftMail>(nextAvatarState.mailBox.First());
        }
    }
}
