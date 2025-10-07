namespace Lib9c.Tests.Action
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Arena;
    using Lib9c.Helper;
    using Lib9c.Model;
    using Lib9c.Model.Item;
    using Lib9c.Model.Mail;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData.Crystal;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class CombinationEquipmentTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly IWorld _initialState;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;

        public CombinationEquipmentTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _agentAddress = new PrivateKey().Address;
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

            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            var combinationSlotState = new CombinationSlotState(
                _slotAddress,
                0);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(_slotAddress, combinationSlotState.Serialize())
                .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax);

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        // Tutorial recipe.
        [InlineData(null, false, false, true, true, 3, 0, true, 1L, 1, null, true, false, false, false)]
        // SubRecipe
        [InlineData(null, true, true, true, true, 27, 0, true, 1L, 6, 376, true, false, false, false)]
        // 3rd sub recipe, not Mimisbrunnr Equipment.
        [InlineData(null, true, true, true, true, 349, 0, true, 1L, 28, 101520003, true, false, false, false)]
        // Purchase CRYSTAL.
        [InlineData(null, true, true, true, true, 3, 0, true, 1L, 1, null, false, false, true, false)]
        // Purchase CRYSTAL with calculate previous cost.
        [InlineData(null, true, true, true, true, 3, 0, true, 100_800L, 1, null, false, false, true, true)]
        // Arena round not found
        [InlineData(null, false, false, true, true, 3, 0, true, 0L, 1, null, true, false, false, false)]
        // UnlockEquipmentRecipe not executed.
        [InlineData(typeof(FailedLoadStateException), false, true, true, true, 11, 0, true, 0L, 6, 1, true, false, false, false)]
        // CRYSTAL not paid.
        [InlineData(typeof(InvalidRecipeIdException), true, false, true, true, 11, 0, true, 0L, 6, 1, true, false, false, false)]
        // AgentState not exist.
        [InlineData(typeof(FailedLoadStateException), true, true, false, true, 3, 0, true, 0L, 1, null, true, false, false, false)]
        // AvatarState not exist.
        [InlineData(typeof(FailedLoadStateException), true, true, true, false, 3, 0, true, 0L, 1, null, true, false, false, false)]
        // CombinationSlotState not exist.
        [InlineData(typeof(CombinationSlotNotFoundException), true, true, true, true, 3, 5, true, 0L, 1, null, true, false, false, false)]
        // CombinationSlotState locked.
        [InlineData(typeof(CombinationSlotUnlockException), true, true, true, true, 3, 0, false, 0L, 1, null, true, false, false, false)]
        // Stage not cleared.
        [InlineData(typeof(NotEnoughClearedStageLevelException), true, true, true, true, 3, 0, true, 0L, 6, null, true, false, false, false)]
        // Not enough material.
        [InlineData(typeof(NotEnoughMaterialException), true, true, true, true, 3, 0, true, 0L, 1, null, false, false, false, false)]
        public void Execute(
            Type exc,
            bool unlockIdsExist,
            bool crystalUnlock,
            bool agentExist,
            bool avatarExist,
            int stageId,
            int slotIndex,
            bool slotUnlock,
            long blockIndex,
            int recipeId,
            int? subRecipeId,
            bool enoughMaterial,
            bool ncgBalanceExist,
            bool payByCrystal,
            bool previousCostStateExist
        )
        {
            var context = new ActionContext();
            var state = _initialState;
            if (unlockIdsExist)
            {
                var unlockIds = List.Empty.Add(1.Serialize());
                if (crystalUnlock)
                {
                    for (var i = 2; i < recipeId + 1; i++)
                    {
                        unlockIds = unlockIds.Add(i.Serialize());
                    }
                }

                state = state.SetLegacyState(_avatarAddress.Derive("recipe_ids"), unlockIds);
            }

            if (agentExist)
            {
                state = state.SetAgentState(_agentAddress, _agentState);

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
                                    _tableSheets.MaterialItemSheet[materialInfo.Id],
                                    _random);
                                _avatarState.inventory.AddItem(subMaterial, materialInfo.Count);
                            }

                            if (ncgBalanceExist && subRow.RequiredGold > 0)
                            {
                                state = state.MintAsset(
                                    context,
                                    _agentAddress,
                                    subRow.RequiredGold * state.GetGoldCurrency());
                            }
                        }
                    }

                    state = state.SetAvatarState(_avatarAddress, _avatarState);

                    if (!slotUnlock)
                    {
                        var allSlotState = new AllCombinationSlotState();
                        var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
                        allSlotState.AddSlot(addr);
                        var slotState = allSlotState.GetSlot(0);
                        slotState.Update(null, 0, blockIndex + 1);

                        state = state
                            .SetCombinationSlotState(_avatarAddress, allSlotState);
                    }
                }
            }

            var expectedCrystal = 0;
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
                        .SetLegacyState(previousCostAddress, previousCostState.Serialize())
                        .SetLegacyState(beforePreviousCostAddress, beforePreviousCostState.Serialize());
                }

                expectedCrystal = crystalBalance;
                state = state.MintAsset(context, _agentAddress, expectedCrystal * CrystalCalculator.CRYSTAL);
            }

            var dailyCostAddress =
                Addresses.GetDailyCrystalCostAddress((int)(blockIndex / CrystalCostState.DailyIntervalIndex));
            var weeklyInterval = _tableSheets.CrystalFluctuationSheet.Values.First(
                r =>
                    r.Type == CrystalFluctuationSheet.ServiceType.Combination).BlockInterval;
            var weeklyCostAddress = Addresses.GetWeeklyCrystalCostAddress((int)(blockIndex / weeklyInterval));

            Assert.Null(state.GetLegacyState(dailyCostAddress));
            Assert.Null(state.GetLegacyState(weeklyCostAddress));

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = payByCrystal,
                useHammerPoint = false,
            };

            if (exc is null)
            {
                var nextState = action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        BlockIndex = blockIndex,
                        RandomSeed = _random.Seed,
                    });

                var currency = nextState.GetGoldCurrency();
                Assert.Equal(0 * currency, nextState.GetBalance(_agentAddress, currency));

                var allSlotState = nextState.GetAllCombinationSlotState(_avatarAddress);
                var slotState = allSlotState.GetSlot(0);
                Assert.NotNull(slotState.Result);
                Assert.NotNull(slotState.Result.itemUsable);

                var equipment = (Equipment)slotState.Result.itemUsable;
                var expectedActionPoint = DailyReward.ActionPointMax - _tableSheets
                    .EquipmentItemRecipeSheet[recipeId]
                    .RequiredActionPoint;
                if (subRecipeId.HasValue)
                {
                    Assert.True(equipment.optionCountFromCombination > 0);

                    if (ncgBalanceExist)
                    {
                        var arenaSheet = _tableSheets.ArenaSheet;
                        var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);
                        var feeStoreAddress = ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
                        Assert.Equal(450 * currency, nextState.GetBalance(feeStoreAddress, currency));
                    }

                    expectedActionPoint -= _tableSheets
                        .EquipmentItemSubRecipeSheetV2[subRecipeId.Value]
                        .RequiredActionPoint;
                }
                else
                {
                    Assert.Equal(0, equipment.optionCountFromCombination);
                }

                var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
                var mail = nextAvatarState.mailBox.OfType<CombinationMail>().First();

                Assert.Equal(equipment, mail.attachment.itemUsable);
                Assert.Equal(payByCrystal, !(nextState.GetLegacyState(dailyCostAddress) is null));
                Assert.Equal(payByCrystal, !(nextState.GetLegacyState(weeklyCostAddress) is null));

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
                Assert.Equal(expectedActionPoint, nextState.GetActionPoint(_avatarAddress));
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Signer = _agentAddress,
                            BlockIndex = blockIndex,
                            RandomSeed = _random.Seed,
                        }));
            }
        }

        [Theory]
        [InlineData(null, false, 1, 1)]
        [InlineData(null, false, 0, 1)]
        [InlineData(typeof(NotEnoughFungibleAssetValueException), true, 1, 1)]
        [InlineData(null, true, 1, 1)]
        [InlineData(typeof(NotEnoughHammerPointException), true, 1, 1)]
        public void ExecuteWithCheckingHammerPointState(
            Type exc,
            bool doSuperCraft,
            int subRecipeIndex,
            int recipeId)
        {
            var context = new ActionContext();
            var state = _initialState;
            var unlockIds = List.Empty.Add(1.Serialize());
            for (var i = 2; i < recipeId + 1; i++)
            {
                unlockIds = unlockIds.Add(i.Serialize());
            }

            state = state.SetLegacyState(_avatarAddress.Derive("recipe_ids"), unlockIds);
            state = state.SetAgentState(_agentAddress, _agentState);
            _avatarState.worldInformation = new WorldInformation(0, _tableSheets.WorldSheet, 200);
            var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem(material, row.MaterialCount);
            int? subRecipeId = row.SubRecipeIds[subRecipeIndex];
            if (exc?.FullName?.Contains(nameof(ArgumentException)) ?? false)
            {
                subRecipeId = row.SubRecipeIds.Last();
            }

            var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
            foreach (var materialInfo in subRow.Materials)
            {
                var subMaterial = ItemFactory.CreateItem(
                    _tableSheets.MaterialItemSheet[materialInfo.Id],
                    _random);
                _avatarState.inventory.AddItem(subMaterial, materialInfo.Count);
            }

            if (subRow.RequiredGold > 0)
            {
                state = state.MintAsset(
                    context,
                    _agentAddress,
                    subRow.RequiredGold * state.GetGoldCurrency());
            }

            state = state
                .SetAvatarState(_avatarAddress, _avatarState);
            var hammerPointAddress =
                Addresses.GetHammerPointStateAddress(_avatarAddress, recipeId);
            if (doSuperCraft)
            {
                var hammerPointState = new HammerPointState(hammerPointAddress, recipeId);
                var hammerPointSheet = _tableSheets.CrystalHammerPointSheet;
                hammerPointState.AddHammerPoint(
                    hammerPointSheet[recipeId].MaxPoint,
                    hammerPointSheet);
                state = state.SetLegacyState(hammerPointAddress, hammerPointState.Serialize());
                if (exc is null)
                {
                    var costCrystal = CrystalCalculator.CRYSTAL *
                        hammerPointSheet[recipeId].CRYSTAL;
                    state = state.MintAsset(
                        context,
                        _agentAddress,
                        costCrystal);
                }
                else if (exc.FullName!.Contains(nameof(NotEnoughHammerPointException)))
                {
                    hammerPointState.ResetHammerPoint();
                    state = state.SetLegacyState(hammerPointAddress, hammerPointState.Serialize());
                }
            }

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = false,
                useHammerPoint = doSuperCraft,
            };
            if (exc is null)
            {
                var nextState = action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        BlockIndex = 1,
                        RandomSeed = _random.Seed,
                    });

                Assert.True(nextState.TryGetLegacyState(hammerPointAddress, out List serialized));
                var hammerPointState = new HammerPointState(hammerPointAddress, serialized);
                if (!doSuperCraft)
                {
                    Assert.Equal(subRow.RewardHammerPoint, hammerPointState.HammerPoint);
                }
                else
                {
                    Assert.Equal(0, hammerPointState.HammerPoint);
                    var allSlotState = nextState.GetAllCombinationSlotState(_avatarAddress);
                    var slotState = allSlotState.GetSlot(0);
                    Assert.NotNull(slotState.Result);
                    Assert.NotNull(slotState.Result.itemUsable);
                    Assert.NotEmpty(slotState.Result.itemUsable.Skills);
                }
            }
            else
            {
                Assert.Throws(
                    exc,
                    () =>
                    {
                        action.Execute(
                            new ActionContext
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                BlockIndex = 1,
                                RandomSeed = _random.Seed,
                            });
                    });
            }
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
                null,
                equipment,
                _random,
                subRecipe,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.PetOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(equipment.optionCountFromCombination > 0);
        }
    }
}
