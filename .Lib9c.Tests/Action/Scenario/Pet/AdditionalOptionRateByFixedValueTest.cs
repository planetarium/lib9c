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
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class AdditionalOptionRateByFixedValueTest
    {
        private const PetOptionType PetOptionType
            = Nekoyume.Model.Pet.PetOptionType.AdditionalOptionRateByFixedValue;

        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IWorld _initialStateV2;
        private readonly Address _recipeAddr;
        private int? _petId;

        public AdditionalOptionRateByFixedValueTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(PetOptionSheet)] = @"ID,_PET NAME,PetLevel,OptionType,OptionValue
1004,꼬마 펜리르,1,AdditionalOptionRateByFixedValue,5.5
1004,꼬마 펜리르,30,AdditionalOptionRateByFixedValue,20";
            (_tableSheets, _agentAddr, _avatarAddr, _initialStateV2)
                = InitializeUtil.InitializeStates(sheetsOverride: sheets);
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

            var stateV2 = _initialStateV2.SetLegacyState(_recipeAddr, stageList);
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2[recipe.SubRecipeIds![1]];
            // Get pet
            var petRow = _tableSheets.PetOptionSheet.Values.First(
                pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
            );
            _petId = petRow.PetId;
            stateV2 = stateV2.SetLegacyState(
                PetState.DeriveAddress(_avatarAddr, (int)_petId),
                new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
            );

            // Prepare
            stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr);

            // Find specific random seed to meet test condition
            var random = new TestRandom(randomSeed);

            // Give Materials
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
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
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            stateV2 = action.Execute(ctx);
            var allSlotState = stateV2.GetAllCombinationSlotState(_avatarAddr);
            var slotState = allSlotState.GetSlot(0);
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
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
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
                PreviousState = stateV2,
                Signer = _agentAddr,
                BlockIndex = 0L,
            };
            ctx.SetRandom(random);
            stateV2 = petAction.Execute(ctx);
            allSlotState = stateV2.GetAllCombinationSlotState(_avatarAddr);
            var petSlotState = allSlotState.GetSlot(1);
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
