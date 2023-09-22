namespace Lib9c.Tests.Action.Scenario.Pet
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
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class CommonTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _recipeAddr;
        private readonly IWorld _initialState;

        public CommonTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialState) =
                InitializeUtil.InitializeStates();
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        // Pet level range test (1~30)
        [Theory]
        [InlineData(0)] // Min. level of pet is 1
        [InlineData(31)] // Max. level of pet is 30
        public void PetLevelRangeTest(int petLevel)
        {
            foreach (var petOptionType in Enum.GetValues<PetOptionType>())
            {
                Assert.Throws<KeyNotFoundException>(
                    () => _tableSheets.PetOptionSheet.Values.First(
                        pet => pet.LevelOptionMap[petLevel].OptionType == petOptionType
                    )
                );
            }
        }

        // You cannot use one pet to the multiple slots at the same time
        [Fact]
        public void PetCannotBeUsedToTwoSlotsAtTheSameTime()
        {
            const int itemId = 10114000;
            const int petId = 1;
            const int petLevel = 1;

            var random = new TestRandom();

            // Get Pet
            var state = LegacyModule.SetState(
                _initialState,
                PetState.DeriveAddress(_avatarAddr, petId),
                new List(petId.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );

            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == itemId
            );
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 0; i < recipe.UnlockStage; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            state = LegacyModule.SetState(state, _recipeAddr, stageList);
            state = CraftUtil.UnlockStage(
                state,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            // Prepare Slots
            state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, 0);
            state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, 1);

            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            // Combination1
            var action1 = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = petId,
            };
            state = action1.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddr,
                BlockIndex = 0L,
                RandomSeed = random.Seed,
            });

            // Combination2: Raises error
            var action2 = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 1,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = petId,
            };
            Assert.Throws<PetIsLockedException>(() => action2.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddr,
                    BlockIndex = 1L,
                    RandomSeed = random.Seed,
                })
            );
        }
    }
}
