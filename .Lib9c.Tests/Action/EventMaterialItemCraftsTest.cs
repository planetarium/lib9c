namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
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
            _initialStates = new World(new MockWorldState());
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                new PrivateKey().Address
            )
            {
                level = 100,
            };

            _initialStates = _initialStates
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var addr = CombinationSlotState.DeriveAddress(_avatarAddress, i);
                const int unlock = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                _initialStates = _initialStates.SetLegacyState(
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
            };

            yield return new object[]
            {
                1002,
                10020002,
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
            int eventMaterialItemRecipeId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var materialsToUse = new Dictionary<int, int>();
            var totalCount = 0;
            var row = _tableSheets.EventMaterialItemRecipeSheet[eventMaterialItemRecipeId];
            while (totalCount < row.RequiredMaterialsCount)
            {
                foreach (var materialId in row.RequiredMaterialsId)
                {
                    materialsToUse.TryAdd(materialId, 0);
                    materialsToUse[materialId] += 1;
                    totalCount++;
                    if (totalCount == row.RequiredMaterialsCount)
                    {
                        break;
                    }
                }
            }

            var contextBlockIndex = scheduleRow.StartBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.RecipeEndBlockIndex;
            Execute(
                _initialStates,
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
                    _initialStates,
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
            var previousAvatarState = previousStates.GetAvatarState(_avatarAddress);

            var recipeSheet = previousStates.GetSheet<EventMaterialItemRecipeSheet>();
            Assert.True(recipeSheet.TryGetValue(eventMaterialItemRecipeId, out var recipeRow));
            var materialItemSheet = previousStates.GetSheet<MaterialItemSheet>();
            foreach (var pair in materialsToUse)
            {
                Assert.True(materialItemSheet.TryGetValue(pair.Key, out var materialRow));
                var material = ItemFactory.CreateItem(materialRow, new TestRandom());
                previousAvatarState.inventory.AddItem(material, pair.Value);
            }

            var worldSheet = previousStates.GetSheet<WorldSheet>();
            previousAvatarState.worldInformation = new WorldInformation(
                blockIndex,
                worldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            previousStates = previousStates
                .SetAvatarState(_avatarAddress, previousAvatarState);

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
                BlockIndex = blockIndex,
            });

            var nextAvatarState = nextStates.GetAvatarState(_avatarAddress);

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
