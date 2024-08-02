namespace Lib9c.Tests.Action.CustomEquipmentCraft
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions.CustomEquipmentCraft;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class CustomEquipmentCraftTest
    {
        private const int DrawingItemId = 600401;
        private const int DrawingToolItemId = 600402;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly IWorld _initialState;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;

        public CustomEquipmentCraftTest()
        {
            _agentAddress = new PrivateKey().Address;
            _avatarAddress = _agentAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CreateAvatar.DeriveFormat,
                0
            ));
            _slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0
            ));
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _random = new TestRandom();
            _agentState = new AgentState(_agentAddress)
            {
                avatarAddresses =
                {
                    [0] = _avatarAddress,
                },
            };

            _avatarState = new AvatarState(
                _avatarAddress, _agentAddress, 0, _tableSheets.GetAvatarSheets(), default
            );
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var combinationSlotState = new CombinationSlotState(
                _slotAddress,
                0);

            _initialState = new World(MockUtil.MockModernWorldState)
                    .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                    .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax)
                    .SetLegacyState(_slotAddress, combinationSlotState.Serialize())
                    .SetAgentState(_agentAddress, _agentState)
                    .SetLegacyState(
                        GameConfigState.Address,
                        new GameConfigState(sheets["GameConfigSheet"]).Serialize()
                    )
                ;

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(
                        Addresses.TableSheet.Derive(key),
                        value.Serialize()
                    );
            }
        }

        [Theory]
        // Success
        // First Craft
        [InlineData(1, 10100000, true, 0, false, ElementalType.Wind, 10, null)]
        // Random Icon
        [InlineData(1, 0, true, 0, false, ElementalType.Wind, 10, null)]
        // Move to next relationship group
        [InlineData(1, 10100000, true, 10, false, ElementalType.Wind, 10, null)]
        [InlineData(1, 10100000, true, 100, false, ElementalType.Wind, 12, null)]
        [InlineData(1, 10100000, true, 1000, false, ElementalType.Wind, 15, null)]
        // Failure
        // Not enough materials
        [InlineData(
            1,
            10100000,
            false,
            0,
            false,
            ElementalType.Normal,
            0,
            typeof(NotEnoughItemException)
        )]
        // Slot already occupied
        [InlineData(
            1,
            10100000,
            true,
            0,
            true,
            ElementalType.Normal,
            0,
            typeof(CombinationSlotUnlockException)
        )]
        // Not enough relationship for icon
        [InlineData(
            1,
            10110000,
            true,
            0,
            false,
            ElementalType.Normal,
            0,
            typeof(NotEnoughRelationshipException)
        )]
        public void Execute(
            int recipeId,
            int iconId,
            bool enoughMaterials,
            int initialRelationship,
            bool slotOccupied,
            ElementalType expectedElementalType,
            long additionalBlock,
            Type exc
        )
        {
            const long currentBlockIndex = 2L;
            var context = new ActionContext();
            var state = _initialState;

            state = state.SetRelationship(_avatarAddress, initialRelationship);

            var gameConfig = state.GetGameConfigState();
            var materialList = new List<int> { DrawingItemId, DrawingToolItemId };
            if (enoughMaterials)
            {
                var relationshipSheet = _tableSheets.CustomEquipmentCraftRelationshipSheet;
                var relationshipRow = relationshipSheet.OrderedList
                    .First(row => row.Relationship >= initialRelationship);
                var materialSheet = _tableSheets.MaterialItemSheet;
                var recipeRow = _tableSheets.CustomEquipmentCraftRecipeSheet[recipeId];
                var drawingRow = materialSheet[DrawingItemId];
                var drawing = ItemFactory.CreateMaterial(drawingRow);
                _avatarState.inventory.AddItem(
                    drawing,
                    (int)Math.Floor(recipeRow.DrawingAmount * relationshipRow.CostMultiplier / 10000m)
                );

                var drawingToolRow = materialSheet[DrawingToolItemId];
                var drawingTool = ItemFactory.CreateMaterial(drawingToolRow);
                var drawingToolAmount =
                    (decimal)recipeRow.DrawingToolAmount * relationshipRow.CostMultiplier;
                if (iconId != 0)
                {
                    drawingToolAmount *= gameConfig.CustomEquipmentCraftIconCostMultiplier / 10000m;
                }

                _avatarState.inventory.AddItem(drawingTool, (int)Math.Floor(drawingToolAmount));

                var costRow = _tableSheets.CustomEquipmentCraftCostSheet.Values
                    .FirstOrDefault(row => row.Relationship == initialRelationship);
                if (costRow is not null)
                {
                    if (costRow.GoldAmount > 0)
                    {
                        state = state.MintAsset(
                            context, _agentAddress, state.GetGoldCurrency() * costRow.GoldAmount
                        );
                    }

                    foreach (var cost in costRow.MaterialCosts)
                    {
                        var row = materialSheet[cost.ItemId];
                        _avatarState.inventory.AddItem(
                            ItemFactory.CreateMaterial(row),
                            cost.Amount
                        );
                        materialList.Add(cost.ItemId);
                    }
                }
            }

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            if (slotOccupied)
            {
                // Lock slot.
                state = state.SetLegacyState(
                    _slotAddress,
                    new CombinationSlotState(
                            ((Dictionary)new CombinationSlotState(_slotAddress, 0).Serialize())
                            .SetItem("unlockBlockIndex", 10.Serialize()
                            )
                        )
                        .Serialize()
                );
            }

            var action = new Nekoyume.Action.CustomEquipmentCraft.CustomEquipmentCraft
            {
                AvatarAddress = _avatarAddress,
                RecipeId = recipeId,
                IconId = iconId,
                SlotIndex = 0,
            };
            if (exc is not null)
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    BlockIndex = currentBlockIndex,
                    Signer = _agentAddress,
                    RandomSeed = _random.Seed,
                }));
            }
            else
            {
                var resultState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    BlockIndex = currentBlockIndex,
                    Signer = _agentAddress,
                    RandomSeed = _random.Seed,
                });

                // Test
                var gold = resultState.GetGoldCurrency();
                Assert.Equal(0 * gold, resultState.GetBalance(_agentAddress, gold));

                var inventory = resultState.GetInventoryV2(_avatarAddress);
                foreach (var material in materialList)
                {
                    // Assert.False(inventory.HasItem(material));
                    Assert.Null(inventory.Items.FirstOrDefault(i => i.item.Id == material));
                }

                Assert.Equal(initialRelationship + 1, resultState.GetRelationship(_avatarAddress));

                var slotState = resultState.GetCombinationSlotState(_avatarAddress, 0);
                Assert.Equal(currentBlockIndex + additionalBlock, slotState.UnlockBlockIndex);

                var equipment = inventory.Equipments.First();
                if (iconId != 0)
                {
                    Assert.Equal(iconId, equipment.IconId);
                }

                var itemSubType = _tableSheets.CustomEquipmentCraftRecipeSheet.Values
                    .First(row => row.Id == recipeId).ItemSubType;
                var expectedEquipmentId =
                    _tableSheets.CustomEquipmentCraftRelationshipSheet.OrderedList
                        .First(row => row.Relationship >= initialRelationship)
                        .GetItemId(itemSubType);
                Assert.Equal(expectedEquipmentId, equipment.Id);

                Assert.Equal(expectedElementalType, equipment.ElementalType);
            }
        }
    }
}
