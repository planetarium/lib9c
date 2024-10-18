namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
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
        private readonly IWorld _initialStateV2;

        public CommonTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV2) =
                InitializeUtil.InitializeStates();
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
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
            var stateV2 = _initialStateV2.SetLegacyState(
                PetState.DeriveAddress(_avatarAddr, petId),
                new List(petId.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );

            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == itemId
            );
            var materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 0; i < recipe.UnlockStage; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            stateV2 = stateV2.SetLegacyState(_recipeAddr, stageList);
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            // Prepare Slots
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr);

            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
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
            stateV2 = action1.Execute(new ActionContext
            {
                PreviousState = stateV2,
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
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 1L,
                    RandomSeed = random.Seed,
                })
            );
        }
    }
}
