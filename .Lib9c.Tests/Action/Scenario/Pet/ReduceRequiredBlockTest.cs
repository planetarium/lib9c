// Reduces required blocks to craft item by ratio.
// ReducedBlock = Round(RequiredBlock * (100 - {ratio})) (ref. PetHelper.CalculateReducedBlockOnCraft)

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
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class ReduceRequiredBlockTest
    {
        private const PetOptionType PetOptionType =
            Nekoyume.Model.Pet.PetOptionType.ReduceRequiredBlock;

        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IWorld _initialStateV2;
        private readonly Address _recipeAddr;
        private int? _petId;

        public ReduceRequiredBlockTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV2)
                = InitializeUtil.InitializeStates();
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(null)] // No Pet
        [InlineData(1)] // Lv.1 reduces 5.5%
        [InlineData(30)] // Lv.30 reduces 20%
        public void CombinationEquipmentTest(int? petLevel)
        {
            var targetItemId = 10114000;
            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(
                recipe => recipe.ResultEquipmentId == targetItemId);
            Assert.NotNull(recipe);

            // Get Materials and stages
            var materialList =
                recipe.GetAllMaterials(_tableSheets.EquipmentItemSubRecipeSheetV2).ToList();
            var stageList = List.Empty;
            for (var i = 1; i < recipe.UnlockStage + 1; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            var stateV2 = _initialStateV2.SetLegacyState(_recipeAddr, stageList);

            // Sub recipe options add blocks, so take the pet-less craft time as the baseline.
            var expectedBlock = Craft(stateV2, recipe, materialList, null);

            // Get pet
            if (!(petLevel is null))
            {
                var petRow = _tableSheets.PetOptionSheet.Values.First(
                    pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
                );

                _petId = petRow.PetId;
                stateV2 = stateV2.SetLegacyState(
                    PetState.DeriveAddress(_avatarAddr, (int)_petId),
                    new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
                );
                expectedBlock = (long)Math.Round(
                    expectedBlock * (1 - petRow.LevelOptionMap[(int)petLevel].OptionValue / 100)
                );
            }

            // TEST: RequiredBlockIndex
            Assert.Equal(expectedBlock, Craft(stateV2, recipe, materialList, _petId));
        }

        private long Craft(
            IWorld stateV2,
            EquipmentItemRecipeSheet.Row recipe,
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList,
            int? petId)
        {
            var random = new TestRandom();

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr);
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            // Do Combination
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds[0],
                petId = petId,
            };

            stateV2 = action.Execute(
                new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    RandomSeed = random.Seed,
                });

            return stateV2.GetAllCombinationSlotState(_avatarAddr)
                .GetSlot(0).RequiredBlockIndex;
        }
    }
}
