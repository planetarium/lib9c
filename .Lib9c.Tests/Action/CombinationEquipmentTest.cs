namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Crystal;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class CombinationEquipmentTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly IAccountStateDelta _initialState;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;

        public CombinationEquipmentTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            _slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(sheets);

            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();

            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var combinationSlotState = new CombinationSlotState(
                _slotAddress,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);

            _initialState = new State()
                .SetState(_slotAddress, combinationSlotState.Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        // Tutorial recipe.
        [InlineData(null, false, false, true, true, false, 3, 0, true, 0L, 1, null, true, false, false, false, false)]
        // Migration AvatarState.
        [InlineData(null, false, false, true, true, true, 3, 0, true, 0L, 1, null, true, false, false, false, false)]
        // SubRecipe
        [InlineData(null, true, true, true, true, false, 11, 0, true, 0L, 2, 1, true, false, false, false, false)]
        // Mimisbrunnr Equipment.
        [InlineData(null, true, true, true, true, false, 11, 0, true, 0L, 2, 3, true, true, true, false, false)]
        // Purchase CRYSTAL.
        [InlineData(null, true, true, true, true, false, 3, 0, true, 0L, 1, null, false, false, false, true, false)]
        // Purchase CRYSTAL with calculate previous cost.
        [InlineData(null, true, true, true, true, false, 3, 0, true, 100_800L, 1, null, false, false, true, true, true)]
        // UnlockEquipmentRecipe not executed.
        [InlineData(typeof(FailedLoadStateException), false, true, true, true, false, 11, 0, true, 0L, 2, 1, true, false, false, false, false)]
        // CRYSTAL not paid.
        [InlineData(typeof(InvalidRecipeIdException), true, false, true, true, false, 11, 0, true, 0L, 2, 1, true, false, false, false, false)]
        // AgentState not exist.
        [InlineData(typeof(FailedLoadStateException), true, true, false, true, false, 3, 0, true, 0L, 1, null, true, false, false, false, false)]
        // AvatarState not exist.
        [InlineData(typeof(FailedLoadStateException), true, true, true, false, false, 3, 0, true, 0L, 1, null, true, false, false, false, false)]
        [InlineData(typeof(FailedLoadStateException), true, true, true, false, true, 3, 0, true, 0L, 1, null, true, false, false, false, false)]
        // Tutorial not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, true, true, true, false, 1, 0, true, 0L, 1, null, true, false, false, false, false)]
        // CombinationSlotState not exist.
        [InlineData(typeof(FailedLoadStateException), true, true, true, true, false, 3, 5, true, 0L, 1, null, true, false, false, false, false)]
        // CombinationSlotState locked.
        [InlineData(typeof(CombinationSlotUnlockException), true, true, true, true, false, 3, 0, false, 0L, 1, null, true, false, false, false, false)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, true, true, true, false, 3, 0, true, 0L, 2, null, true, false, false, false, false)]
        // Not enough material.
        [InlineData(typeof(NotEnoughMaterialException), true, true, true, true, false, 3, 0, true, 0L, 1, null, false, false, false, false, false)]
        // Purchase CRYSTAL failed by Mimisbrunnr material.
        [InlineData(typeof(ArgumentException), true, true, true, true, false, 11, 0, true, 0L, 2, 3, false, false, true, true, false)]
        // Insufficient NCG.
        [InlineData(typeof(InsufficientBalanceException), true, true, true, true, false, 11, 0, true, 0L, 2, 3, true, false, true, false, false)]
        public void Execute(
            Type exc,
            bool unlockIdsExist,
            bool crystalUnlock,
            bool agentExist,
            bool avatarExist,
            bool migrationRequired,
            int stageId,
            int slotIndex,
            bool slotUnlock,
            long blockIndex,
            int recipeId,
            int? subRecipeId,
            bool enoughMaterial,
            bool ncgBalanceExist,
            bool mimisbrunnr,
            bool payByCrystal,
            bool previousCostStateExist
        )
        {
            IAccountStateDelta state = _initialState;
            if (unlockIdsExist)
            {
                var unlockIds = List.Empty.Add(1.Serialize());
                if (crystalUnlock)
                {
                    for (int i = 2; i < recipeId + 1; i++)
                    {
                        unlockIds = unlockIds.Add(i.Serialize());
                    }
                }

                state = state.SetState(_avatarAddress.Derive("recipe_ids"), unlockIds);
            }

            if (agentExist)
            {
                state = state.SetState(_agentAddress, _agentState.Serialize());

                if (avatarExist)
                {
                    _avatarState.worldInformation = new WorldInformation(
                        0,
                        _tableSheets.WorldSheet,
                        stageId);

                    if (enoughMaterial)
                    {
                        var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
                        var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
                        var material = ItemFactory.CreateItem(materialRow, _random);
                        _avatarState.inventory.AddItem(material, row.MaterialCount);

                        if (subRecipeId.HasValue)
                        {
                            var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];

                            foreach (var materialInfo in subRow.Materials)
                            {
                                var subMaterial = ItemFactory.CreateItem(
                                    _tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                                _avatarState.inventory.AddItem(subMaterial, materialInfo.Count);
                            }

                            if (ncgBalanceExist)
                            {
                                state = state.MintAsset(
                                    _agentAddress,
                                    subRow.RequiredGold * state.GetGoldCurrency());
                            }
                        }
                    }

                    if (migrationRequired)
                    {
                        state = state.SetState(_avatarAddress, _avatarState.Serialize());
                    }
                    else
                    {
                        var inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
                        var worldInformationAddress =
                            _avatarAddress.Derive(LegacyWorldInformationKey);
                        var questListAddress = _avatarAddress.Derive(LegacyQuestListKey);

                        state = state
                            .SetState(_avatarAddress, _avatarState.SerializeV2())
                            .SetState(inventoryAddress, _avatarState.inventory.Serialize())
                            .SetState(
                                worldInformationAddress,
                                _avatarState.worldInformation.Serialize())
                            .SetState(questListAddress, _avatarState.questList.Serialize());
                    }

                    if (!slotUnlock)
                    {
                        // Lock slot.
                        state = state.SetState(
                            _slotAddress,
                            new CombinationSlotState(_slotAddress, stageId + 1).Serialize()
                        );
                    }
                }
            }

            int expectedCrystal = 0;
            if (payByCrystal)
            {
                var crystalBalance = 0;
                var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
                var costSheet = _tableSheets.CrystalMaterialCostSheet;
                crystalBalance += costSheet[row.MaterialId].CRYSTAL * row.MaterialCount;

                if (subRecipeId.HasValue)
                {
                    var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];

                    foreach (var materialInfo in subRow.Materials)
                    {
                        if (costSheet.ContainsKey(materialInfo.Id))
                        {
                            crystalBalance += costSheet[materialInfo.Id].CRYSTAL * row.MaterialCount;
                        }
                    }
                }

                if (previousCostStateExist)
                {
                    var previousCostAddress = Addresses.GetWeeklyCrystalCostAddress(6);
                    var previousCostState = new CrystalCostState(previousCostAddress, crystalBalance * CrystalCalculator.CRYSTAL * 2);
                    var beforePreviousCostAddress = Addresses.GetWeeklyCrystalCostAddress(5);
                    var beforePreviousCostState = new CrystalCostState(beforePreviousCostAddress, crystalBalance * CrystalCalculator.CRYSTAL);

                    state = state
                        .SetState(previousCostAddress, previousCostState.Serialize())
                        .SetState(beforePreviousCostAddress, beforePreviousCostState.Serialize());
                }

                expectedCrystal = crystalBalance;
                state = state.MintAsset(_agentAddress, expectedCrystal * CrystalCalculator.CRYSTAL);
            }

            var dailyCostAddress =
                Addresses.GetDailyCrystalCostAddress((int)(blockIndex / CrystalCostState.DailyIntervalIndex));
            var weeklyInterval = _tableSheets.CrystalFluctuationSheet.Values.First(r =>
                r.Type == CrystalFluctuationSheet.ServiceType.Combination).BlockInterval;
            var weeklyCostAddress = Addresses.GetWeeklyCrystalCostAddress((int)(blockIndex / weeklyInterval));

            Assert.Null(state.GetState(dailyCostAddress));
            Assert.Null(state.GetState(weeklyCostAddress));

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = payByCrystal,
            };

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                    Random = _random,
                });

                var currency = nextState.GetGoldCurrency();
                Assert.Equal(0 * currency, nextState.GetBalance(_agentAddress, currency));

                var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
                Assert.NotNull(slotState.Result);
                Assert.NotNull(slotState.Result.itemUsable);

                var equipment = (Equipment)slotState.Result.itemUsable;
                if (subRecipeId.HasValue)
                {
                    Assert.True(equipment.optionCountFromCombination > 0);

                    if (ncgBalanceExist)
                    {
                        Assert.Equal(450 * currency, nextState.GetBalance(Addresses.Blacksmith, currency));
                    }

                    Assert.Equal(mimisbrunnr, equipment.MadeWithMimisbrunnrRecipe);
                    Assert.Equal(
                        mimisbrunnr,
                        equipment.IsMadeWithMimisbrunnrRecipe(
                            _tableSheets.EquipmentItemRecipeSheet,
                            _tableSheets.EquipmentItemSubRecipeSheetV2,
                            _tableSheets.EquipmentItemOptionSheet
                        )
                    );

                    if (mimisbrunnr)
                    {
                        Assert.Equal(ElementalType.Fire, equipment.ElementalType);
                    }
                }
                else
                {
                    Assert.Equal(0, equipment.optionCountFromCombination);
                }

                var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
                var mail = nextAvatarState.mailBox.OfType<CombinationMail>().First();

                Assert.Equal(equipment, mail.attachment.itemUsable);
                Assert.Equal(payByCrystal, !(nextState.GetState(dailyCostAddress) is null));
                Assert.Equal(payByCrystal, !(nextState.GetState(weeklyCostAddress) is null));

                if (payByCrystal)
                {
                    var dailyCostState = nextState.GetCrystalCostState(dailyCostAddress);
                    var weeklyCostState = nextState.GetCrystalCostState(weeklyCostAddress);

                    Assert.Equal(0 * CrystalCalculator.CRYSTAL, nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
                    Assert.Equal(1, dailyCostState.Count);
                    Assert.Equal(expectedCrystal * CrystalCalculator.CRYSTAL, dailyCostState.CRYSTAL);
                    Assert.Equal(1, weeklyCostState.Count);
                    Assert.Equal(expectedCrystal * CrystalCalculator.CRYSTAL, weeklyCostState.CRYSTAL);
                }

                Assert.Equal(expectedCrystal * CrystalCalculator.CRYSTAL, nextState.GetBalance(Addresses.MaterialCost, CrystalCalculator.CRYSTAL));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                    Random = _random,
                }));
            }
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = 1,
                subRecipeId = 255,
            };
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );

            var updatedAddresses = new List<Address>
            {
                _agentAddress,
                _avatarAddress,
                slotAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                ItemEnhancement.GetFeeStoreAddress(),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        [Fact]
        public void AddAndUnlockOption()
        {
            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2.Last;
            Assert.NotNull(subRecipe);
            var equipment = (Necklace)ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet[10411000],
                Guid.NewGuid(),
                default);
            Assert.Equal(0, equipment.optionCountFromCombination);
            CombinationEquipment.AddAndUnlockOption(
                _agentState,
                equipment,
                _random,
                subRecipe,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(equipment.optionCountFromCombination > 0);
        }

        [Theory]
        [InlineData(1, false, 375, false)]
        [InlineData(1, false, 374, false)]
        [InlineData(2, true, 3, true)]
        [InlineData(2, true, 2, false)]
        [InlineData(3, false, 6, false)]
        [InlineData(3, false, 5, false)]
        [InlineData(134, true, 313, false)]
        [InlineData(134, true, 314, false)]
        [InlineData(134, true, 315, true)]
        public void MadeWithMimisbrunnrRecipe(
            int recipeId,
            bool isElementalTypeFire,
            int? subRecipeId,
            bool isMadeWithMimisbrunnrRecipe)
        {
            var currency = new Currency("NCG", 2, minter: null);
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var requiredStage = row.UnlockStage;
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                requiredStage);

            avatarState.inventory.AddItem(material, row.MaterialCount);

            if (subRecipeId.HasValue)
            {
                var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];

                foreach (var materialInfo in subRow.Materials)
                {
                    material = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                    avatarState.inventory.AddItem(material, materialInfo.Count);
                }
            }

            var previousState = _initialState
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

            previousState = previousState.MintAsset(_agentAddress, 10_000 * currency);

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);
            var isMadeWithMimisbrunnrRecipe_considerElementalType =
                isElementalTypeFire &&
                ((Equipment)slotState.Result.itemUsable).MadeWithMimisbrunnrRecipe;
            Assert.Equal(
                isMadeWithMimisbrunnrRecipe,
                isMadeWithMimisbrunnrRecipe_considerElementalType);
            Assert.Equal(
                isMadeWithMimisbrunnrRecipe,
                ((Equipment)slotState.Result.itemUsable).IsMadeWithMimisbrunnrRecipe(
                    _tableSheets.EquipmentItemRecipeSheet,
                    _tableSheets.EquipmentItemSubRecipeSheetV2,
                    _tableSheets.EquipmentItemOptionSheet
                ));
        }

        private void Execute(bool backward, int recipeId, int? subRecipeId, int mintNCG)
        {
            var currency = new Currency("NCG", 2, minter: null);
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var requiredStage = row.UnlockStage;
            var costActionPoint = row.RequiredActionPoint;
            var costNCG = row.RequiredGold * currency;
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var previousActionPoint = avatarState.actionPoint;
            var previousResultEquipmentCount =
                avatarState.inventory.Equipments.Count(e => e.Id == row.ResultEquipmentId);
            var previousMailCount = avatarState.mailBox.Count;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                requiredStage);

            avatarState.inventory.AddItem(material, row.MaterialCount);

            if (subRecipeId.HasValue)
            {
                var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
                costActionPoint += subRow.RequiredActionPoint;
                costNCG += subRow.RequiredGold * currency;

                foreach (var materialInfo in subRow.Materials)
                {
                    material = ItemFactory.CreateItem(_tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                    avatarState.inventory.AddItem(material, materialInfo.Count);
                }
            }

            IAccountStateDelta previousState;
            if (backward)
            {
                previousState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                previousState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            previousState = previousState.MintAsset(_agentAddress, mintNCG * currency);
            var goldCurrencyState = previousState.GetGoldCurrency();
            var previousNCG = previousState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(mintNCG * currency, previousNCG);

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            if (subRecipeId.HasValue)
            {
                Assert.True(((Equipment)slotState.Result.itemUsable).optionCountFromCombination > 0);
            }
            else
            {
                Assert.Equal(0, ((Equipment)slotState.Result.itemUsable).optionCountFromCombination);
            }

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(previousActionPoint - costActionPoint, nextAvatarState.actionPoint);
            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultEquipmentCount + 1,
                nextAvatarState.inventory.Equipments.Count(e => e.Id == row.ResultEquipmentId));

            var agentGold = nextState.GetBalance(_agentAddress, goldCurrencyState);
            Assert.Equal(previousNCG - costNCG, agentGold);
            var fee = nextState.GetBalance(ItemEnhancement.GetFeeStoreAddress(), goldCurrencyState);
            Assert.Equal(costNCG, fee);
        }
    }
}
