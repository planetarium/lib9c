// Increases item option success ratio for each options independent

namespace Lib9c.Tests.Action.Scenario.Pet
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class AdditionalOptionRateByFixedValueTest
    {
        private const PetOptionType PetOptionType
            = Nekoyume.Model.Pet.PetOptionType.AdditionalOptionRateByFixedValue;

        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IWorld _initialState;
        private readonly Address _recipeAddr;
        private int? _petId;

        public AdditionalOptionRateByFixedValueTest()
        {
            (_tableSheets, _agentAddr, _avatarAddr, _initialState)
                = InitializeUtil.InitializeStates();
            _recipeAddr = _avatarAddr.Derive("recipe_ids");
        }

        [Theory]
        [InlineData(73, 10114000, 1)]
        [InlineData(37, 10114000, 30)]
        public void CombinationEquipmentTest(
            int randomSeed,
            int targetItemId,
            int petLevel
        )
        {
            var (beforeResult, afterResult) = (false, false);
            // Get Recipe
            var recipe = _tableSheets.EquipmentItemRecipeSheet.Values.First(
                recipe => recipe.ResultEquipmentId == targetItemId
            );
            Assert.NotNull(recipe);

            // Get Materials and stages
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materialList =
                recipe.GetAllMaterials(
                    _tableSheets.EquipmentItemSubRecipeSheetV2, CraftType.Premium
                ).ToList();
            var stageList = List.Empty;
            for (var i = 0; i < recipe.UnlockStage; i++)
            {
                stageList = stageList.Add(i.Serialize());
            }

            var state = LegacyModule.SetState(_initialState, _recipeAddr, stageList);
            state = CraftUtil.UnlockStage(
                state,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2[recipe.SubRecipeIds![1]];
            var (originalOption2Ratio, originalOption3Ratio, originalOption4Ratio) =
                (subRecipe.Options[1].Ratio, subRecipe.Options[2].Ratio,
                    subRecipe.Options[3].Ratio);
            var (expectedOption2Ratio, expectedOption3Ratio, expectedOption4Ratio) =
                (originalOption2Ratio, originalOption3Ratio, originalOption4Ratio);

            // Get pet
            var petRow = _tableSheets.PetOptionSheet.Values.First(
                pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
            );
            _petId = petRow.PetId;
            state = LegacyModule.SetState(
                state,
                PetState.DeriveAddress(_avatarAddr, (int)_petId),
                new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );
            var increment = (int)petRow.LevelOptionMap[petLevel].OptionValue * 100;
            (expectedOption2Ratio, expectedOption3Ratio, expectedOption4Ratio) =
            (
                originalOption2Ratio + increment,
                originalOption3Ratio + increment,
                originalOption4Ratio + increment
            );

            // Prepare
            state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, 0);
            state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, 1);

            // Find specific random seed to meet test condition
            var random = new TestRandom(randomSeed);

            // Give Materials
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            // Do combination without pet
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
            };
            var ctx = new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            state = action.Execute(ctx);
            var slotState = LegacyModule.GetCombinationSlotState(state, _avatarAddr, 0);
            // TEST: No additional option added (1 star)
            Assert.Equal(
                recipe.RequiredBlockIndex + subRecipe.RequiredBlockIndex +
                subRecipe.Options[0].RequiredBlockIndex,
                slotState.RequiredBlockIndex
            );

            /*
             * After this line, we reset random seed and retry combination "with" pet.
             * This should be success to add failed option before
             */

            // Reset Random
            random = new TestRandom(randomSeed);

            // Give materials
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );

            var petAction = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 1,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[1],
                petId = _petId,
            };
            ctx = new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            state = petAction.Execute(ctx);
            var petSlotState = LegacyModule.GetCombinationSlotState(state, _avatarAddr, 1);
            // TEST: One additional option added (2 star)
            Assert.Equal(
                recipe.RequiredBlockIndex + subRecipe.RequiredBlockIndex +
                subRecipe.Options[0].RequiredBlockIndex +
                subRecipe.Options[1].RequiredBlockIndex,
                petSlotState.RequiredBlockIndex
            );
        }
    }
}
