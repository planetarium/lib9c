namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RapidCombinationTest
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IWorld _initialStatesWithAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly int _hourGlassItemId;

        public RapidCombinationTest()
        {
            (
                _tableSheets,
                _agentAddr,
                _avatarAddr,
                _initialStatesWithAvatarState
            ) = InitializeUtil.InitializeStates();
            _hourGlassItemId = _tableSheets.MaterialItemSheet.OrderedList.First(
                e => e.ItemSubType == ItemSubType.Hourglass
            ).Id;
        }

        [Theory]
        // 롱 소드(땅) : (155-10)/3 = 48.3
        [InlineData(1, new[] { 10113000 }, 49)]
        // 롱 소드(땅) : (155-10)/3 = 48.3
        // 롱 소드(바람) : (477-10)/3 = 155.6
        [InlineData(1, new[] { 10113000, 10114000 }, 205)]
        public void RapidCombine_Equipment(
            int randomSeed,
            int[] targetItemIdList,
            int expectedHourGlassCount
        )
        {
            // Disable all quests to prevent contamination by quest reward
            var state = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarState,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var recipeList = _tableSheets.EquipmentItemRecipeSheet.OrderedList.Where(
                recipe => targetItemIdList.Contains(recipe.ResultEquipmentId)
            ).ToList();
            List<EquipmentItemSubRecipeSheet.MaterialInfo> allMaterialList =
                new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
            foreach (var recipe in recipeList)
            {
                allMaterialList = allMaterialList
                    .Concat(recipe.GetAllMaterials(
                        _tableSheets.EquipmentItemSubRecipeSheetV2,
                        CraftType.Normal
                    ))
                    .ToList();
            }

            // Unlock recipe
            var maxUnlockStage = recipeList.Aggregate(0, (e, c) => Math.Max(e, c.UnlockStage));
            var unlockRecipeIdsAddress = _avatarAddr.Derive("recipe_ids");
            var recipeIds = List.Empty;
            for (int i = 1; i < maxUnlockStage + 1; i++)
            {
                recipeIds = recipeIds.Add(i.Serialize());
            }

            state = LegacyModule.SetState(state, unlockRecipeIdsAddress, recipeIds);

            // Prepare combination slot
            for (var i = 0; i < targetItemIdList.Length; i++)
            {
                state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, i);
            }

            // Initial inventory must be empty
            var inventoryState = AvatarModule.GetInventory(state, _avatarAddr);
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                allMaterialList,
                random
            );

            // Give HourGlasses to execute RapidCombination
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                new List<EquipmentItemSubRecipeSheet.MaterialInfo>
                {
                    new EquipmentItemSubRecipeSheet.MaterialInfo(
                        _hourGlassItemId,
                        expectedHourGlassCount
                    ),
                },
                random
            );

            for (var i = 0; i < recipeList.Count; i++)
            {
                // Unlock stage
                var equipmentRecipe = recipeList[i];
                state = CraftUtil.UnlockStage(
                    state,
                    _tableSheets,
                    _avatarAddr,
                    equipmentRecipe.UnlockStage
                );

                // Do combination action
                var recipe = recipeList[i];
                var action = new CombinationEquipment
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = i,
                    recipeId = recipe.Id,
                    subRecipeId = recipe.SubRecipeIds?[0],
                };

                state = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    RandomSeed = random.Seed,
                });

                var slotState = LegacyModule.GetCombinationSlotState(state, _avatarAddr, i);
                // TEST: requiredBlock
                // TODO: Check reduced required block when pet comes in
                Assert.Equal(recipe.RequiredBlockIndex, slotState.RequiredBlockIndex);
            }

            // Do RapidCombination
            for (var i = 0; i < recipeList.Count; i++)
            {
                var action = new RapidCombination
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = i,
                };
                state = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddr,
                    BlockIndex = LegacyModule.GetGameConfigState(state).RequiredAppraiseBlock,
                    RandomSeed = random.Seed,
                });

                var slotState = LegacyModule.GetCombinationSlotState(state, _avatarAddr, i);
                // TEST: requiredBlockIndex should be 10, a RequiredAppraiseBlock
                Assert.Equal(10, slotState.RequiredBlockIndex);
            }

            // TEST: Only created items should remain in inventory
            // TEST: All HourGlasses are used
            inventoryState = AvatarModule.GetInventory(state, _avatarAddr);
            Assert.Equal(recipeList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }
    }
}
