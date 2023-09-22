// patron increases block per hourglass by value.

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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Pet;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class IncreaseBlockPerHourglassTest
    {
        private const PetOptionType PetOptionType =
            Nekoyume.Model.Pet.PetOptionType.IncreaseBlockPerHourglass;

        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _recipeIdsAddr;
        private readonly IWorld _initialState;
        private readonly TableSheets _tableSheets;
        private readonly int _hourglassItemId;
        private int? _petId;

        public IncreaseBlockPerHourglassTest()
        {
            (
                _tableSheets,
                _agentAddr,
                _avatarAddr,
                _initialState
            ) = InitializeUtil.InitializeStates();
            _recipeIdsAddr = _avatarAddr.Derive("recipe_ids");
            _hourglassItemId = _tableSheets.MaterialItemSheet.Values.First(
                item => item.ItemSubType == ItemSubType.Hourglass
            ).Id;
        }

        [Theory]
        [InlineData(1, 10113000, null)] // No Pet
        [InlineData(1, 10113000, 1)] // Lv.1 increases 1 block per HG: 3 -> 4
        [InlineData(1, 10113000, 30)] // Lv.30 increases 30 blocks per HG: 3 -> 33
        [InlineData(1, 10120000, 30)] // Test for min. Hourglass is 1
        public void RapidCombinationTest_Equipment(
            int randomSeed,
            int targetItemId,
            int? petLevel
        )
        {
            var random = new TestRandom(randomSeed);

            // Disable all quests to prevent contamination by quest reward
            var state = QuestUtil.DisableQuestList(
                _initialState,
                _avatarAddr
            );

            // Get recipe
            var recipe =
                _tableSheets.EquipmentItemRecipeSheet.Values.First(
                    recipe => recipe.ResultEquipmentId == targetItemId
                );
            Assert.NotNull(recipe);

            // Get Materials and stages
            var materialList = recipe.GetAllMaterials(
                _tableSheets.EquipmentItemSubRecipeSheetV2
            ).ToList();

            var recipeIds = List.Empty;
            for (var i = 1; i < recipe.UnlockStage; i++)
            {
                recipeIds = recipeIds.Add(i.Serialize());
            }

            state = LegacyModule.SetState(state, _recipeIdsAddr, recipeIds);

            var expectedHourglass = (int)Math.Ceiling(
                ((double)recipe.RequiredBlockIndex
                 - LegacyModule.GetGameConfigState(state).RequiredAppraiseBlock)
                /
                LegacyModule.GetGameConfigState(state).HourglassPerBlock);

            // Get pet
            if (!(petLevel is null))
            {
                var petRow = _tableSheets.PetOptionSheet.Values.First(
                    pet => pet.LevelOptionMap[(int)petLevel!].OptionType == PetOptionType
                );
                _petId = petRow.PetId;
                state = LegacyModule.SetState(
                    state,
                    PetState.DeriveAddress(_avatarAddr, (int)_petId),
                    new List(_petId!.Serialize(), petLevel.Serialize(), 0L.Serialize())
                );
                expectedHourglass = (int)Math.Ceiling(
                    (recipe.RequiredBlockIndex
                     - LegacyModule.GetGameConfigState(state).RequiredAppraiseBlock)
                    /
                    (LegacyModule.GetGameConfigState(state).HourglassPerBlock
                     + petRow.LevelOptionMap[(int)petLevel].OptionValue)
                );
            }

            // Give hourglass
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                new List<EquipmentItemSubRecipeSheet.MaterialInfo>
                {
                    new EquipmentItemSubRecipeSheet.MaterialInfo(
                        _hourglassItemId,
                        expectedHourglass
                    ),
                },
                random
            );

            // Prepare to combination
            state = CraftUtil.PrepareCombinationSlot(state, _avatarAddr, 0);
            state = CraftUtil.AddMaterialsToInventory(
                state,
                _tableSheets,
                _avatarAddr,
                materialList,
                random
            );
            state = CraftUtil.UnlockStage(
                state,
                _tableSheets,
                _avatarAddr,
                recipe.UnlockStage
            );

            // Do combination
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
                recipeId = recipe.Id,
                subRecipeId = recipe.SubRecipeIds?[0],
                petId = _petId,
            };

            state = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddr,
                BlockIndex = 0L,
                RandomSeed = random.Seed,
            });

            // Do rapid combination
            var rapidAction = new RapidCombination
            {
                avatarAddress = _avatarAddr,
                slotIndex = 0,
            };
            state = rapidAction.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddr,
                BlockIndex = LegacyModule.GetGameConfigState(state).RequiredAppraiseBlock,
                RandomSeed = random.Seed,
            });

            var slotState = LegacyModule.GetCombinationSlotState(state, _avatarAddr, 0);
            // TEST: Combination should be done
            Assert.Equal(
                LegacyModule.GetGameConfigState(state).RequiredAppraiseBlock,
                slotState.RequiredBlockIndex
            );

            // TEST: All Hourglasses should be used
            var inventoryState = AvatarModule.GetInventory(state, _avatarAddr);
            Assert.Equal(1, inventoryState.Items.Count);
            Assert.Throws<InvalidOperationException>(() =>
                inventoryState.Items.First(item => item.item.Id == _hourglassItemId));
        }
    }
}
