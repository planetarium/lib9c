namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV.Cost;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class ItemEnhancementTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private IWorld _initialState;

        public ItemEnhancementTest()
        {
            _initialState = new World(MockUtil.MockModernWorldState);
            Dictionary<string, string> sheets;
            (_initialState, sheets) = InitializeUtil.InitializeTableSheets(
                _initialState,
                sheetsOverride: new Dictionary<string, string>
                {
                    {
                        "EnhancementCostSheetV3",
                        EnhancementCostSheetFixtures.V4
                    },
                });
            _tableSheets = new TableSheets(sheets);
            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var gold = new GoldCurrencyState(_currency);

            var allSlotState = new AllCombinationSlotState();
            var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
            allSlotState.AddSlot(addr);

            var context = new ActionContext();
            _initialState = _initialState
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState)
                .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(context, GoldCurrencyState.Address, gold.Currency * 100_000_000_000)
                .TransferAsset(
                    context,
                    Addresses.GoldCurrency,
                    _agentAddress,
                    gold.Currency * 3_000_000
                );

            Assert.Equal(
                gold.Currency * 99_997_000_000,
                _initialState.GetBalance(Addresses.GoldCurrency, gold.Currency)
            );
            Assert.Equal(
                gold.Currency * 3_000_000,
                _initialState.GetBalance(_agentAddress, gold.Currency)
            );
        }

        [Theory]
        // from 0 to 0 using one level 0 material
        [InlineData(0, false, 0, false, 1)]
        [InlineData(0, false, 0, true, 1)]
        [InlineData(0, true, 0, false, 1)]
        [InlineData(0, true, 0, true, 1)]
        // from 0 to 1 using two level 0 material
        [InlineData(0, false, 0, false, 3)]
        [InlineData(0, false, 0, true, 3)]
        [InlineData(0, true, 0, false, 3)]
        [InlineData(0, true, 0, true, 3)]
        // // Duplicated > from 0 to 0
        [InlineData(0, false, 0, false, 3, true)]
        [InlineData(0, false, 0, true, 3, true)]
        [InlineData(0, true, 0, false, 3, true)]
        [InlineData(0, true, 0, true, 3, true)]
        // from 0 to N using multiple level 0 materials
        [InlineData(0, false, 0, false, 7)]
        [InlineData(0, false, 0, false, 31)]
        [InlineData(0, false, 0, true, 7)]
        [InlineData(0, false, 0, true, 31)]
        [InlineData(0, true, 0, false, 7)]
        [InlineData(0, true, 0, false, 31)]
        [InlineData(0, true, 0, true, 7)]
        [InlineData(0, true, 0, true, 31)]
        // // Duplicated > from 0 to 0
        [InlineData(0, false, 0, false, 7, true)]
        [InlineData(0, false, 0, false, 31, true)]
        [InlineData(0, false, 0, true, 7, true)]
        [InlineData(0, false, 0, true, 31, true)]
        [InlineData(0, true, 0, false, 7, true)]
        [InlineData(0, true, 0, false, 31, true)]
        [InlineData(0, true, 0, true, 7, true)]
        [InlineData(0, true, 0, true, 31, true)]
        // from K to K with material(s). Check requiredBlock == 0
        [InlineData(10, false, 0, false, 1)]
        [InlineData(10, false, 0, true, 1)]
        [InlineData(10, true, 0, false, 1)]
        [InlineData(10, true, 0, true, 1)]
        // from K to N using one level X material
        [InlineData(5, false, 6, false, 1)]
        [InlineData(5, false, 6, true, 1)]
        [InlineData(5, true, 6, false, 1)]
        [InlineData(5, true, 6, true, 1)]
        // from K to N using multiple materials
        [InlineData(5, false, 4, false, 6)]
        [InlineData(5, false, 7, false, 5)]
        [InlineData(5, false, 4, true, 6)]
        [InlineData(5, false, 7, true, 5)]
        [InlineData(5, true, 4, false, 6)]
        [InlineData(5, true, 7, false, 5)]
        [InlineData(5, true, 4, true, 6)]
        [InlineData(5, true, 7, true, 5)]
        // // Duplicated: from K to K
        [InlineData(5, true, 4, true, 6, true)]
        [InlineData(5, true, 7, true, 5, true)]
        [InlineData(5, true, 4, false, 6, true)]
        [InlineData(5, true, 7, false, 5, true)]
        [InlineData(5, false, 4, true, 6, true)]
        [InlineData(5, false, 7, true, 5, true)]
        [InlineData(5, false, 4, false, 6, true)]
        [InlineData(5, false, 7, false, 5, true)]
        // from 20 to 21 (just to reach level 21 exp)
        [InlineData(20, false, 20, false, 1)]
        [InlineData(20, false, 20, true, 1)]
        [InlineData(20, true, 20, false, 1)]
        [InlineData(20, true, 20, true, 1)]
        // from 20 to 21 (over level 21)
        [InlineData(20, false, 20, false, 2)]
        [InlineData(20, false, 20, true, 2)]
        [InlineData(20, true, 20, false, 2)]
        [InlineData(20, true, 20, true, 2)]
        // from 21 to 21 (no level up)
        [InlineData(21, false, 1, false, 1)]
        [InlineData(21, false, 21, false, 1)]
        [InlineData(21, false, 1, true, 1)]
        [InlineData(21, false, 21, true, 1)]
        [InlineData(21, true, 1, false, 1)]
        [InlineData(21, true, 21, false, 1)]
        [InlineData(21, true, 1, true, 1)]
        [InlineData(21, true, 21, true, 1)]
        public void Execute(
            int startLevel,
            bool oldStart,
            int materialLevel,
            bool oldMaterial,
            int materialCount,
            bool duplicated = false
        )
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Id == 10110000);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, startLevel);
            if (startLevel == 0)
            {
                equipment.Exp = (long)row.Exp!;
            }
            else
            {
                equipment.Exp = _tableSheets.EnhancementCostSheetV3.OrderedList.First(r =>
                    r.ItemSubType == equipment.ItemSubType && r.Grade == equipment.Grade &&
                    r.Level == equipment.level).Exp;
            }

            var startExp = equipment.Exp;
            if (oldStart)
            {
                equipment.Exp = 0L;
            }

            _avatarState.inventory.AddItem(equipment, 1);

            var startRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(r =>
                r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                r.Level == startLevel);
            var expectedExpIncrement = 0L;
            var materialIds = new List<Guid>();
            var duplicatedGuid = Guid.NewGuid();
            for (var i = 0; i < materialCount; i++)
            {
                var materialId = duplicated ? duplicatedGuid : Guid.NewGuid();
                materialIds.Add(materialId);
                var material =
                    (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, materialLevel);
                if (materialLevel == 0)
                {
                    material.Exp = (long)row.Exp!;
                }
                else
                {
                    material.Exp = _tableSheets.EnhancementCostSheetV3.OrderedList.First(r =>
                        r.ItemSubType == material.ItemSubType && r.Grade == material.Grade &&
                        r.Level == material.level).Exp;
                }

                if (!(duplicated && i > 0))
                {
                    expectedExpIncrement += material.Exp;
                }

                if (oldMaterial)
                {
                    material.Exp = 0L;
                }

                _avatarState.inventory.AddItem(material, 1);
            }

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };
            var preItemUsable = new Equipment((Dictionary)equipment.Serialize());

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update(mail);
            }

            _avatarState.worldInformation.ClearStage(
                1,
                1,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            Assert.Equal(startLevel, equipment.level);

            _initialState = _initialState.SetAvatarState(_avatarAddress, _avatarState);

            var action = new ItemEnhancement
            {
                itemId = default,
                materialIds = materialIds,
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                RandomSeed = 0,
            });

            var allSlotState = nextState.GetAllCombinationSlotState(_avatarAddress);
            var slotState = allSlotState.GetSlot(0);

            var resultEquipment = (Equipment)slotState.Result.itemUsable;
            var level = resultEquipment.level;
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var expectedTargetRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(
                r => r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                    r.Level == level);
            var expectedCost = (expectedTargetRow?.Cost ?? 0) - (startRow?.Cost ?? 0);
            var expectedBlockIndex =
                (expectedTargetRow?.RequiredBlockIndex ?? 0) - (startRow?.RequiredBlockIndex ?? 0);
            Assert.Equal(default, resultEquipment.ItemId);
            Assert.Equal(startExp + expectedExpIncrement, resultEquipment.Exp);
            Assert.Equal(
                (3_000_000 - expectedCost) * _currency,
                nextState.GetBalance(_agentAddress, _currency)
            );

            var arenaSheet = _tableSheets.ArenaSheet;
            var arenaData = arenaSheet.GetRoundByBlockIndex(1);
            var feeStoreAddress =
                ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
            Assert.Equal(
                expectedCost * _currency,
                nextState.GetBalance(feeStoreAddress, _currency)
            );
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            var slotResult = (ItemEnhancement13.ResultModel)slotState.Result;
            if (startLevel != level)
            {
                var baseMinAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var baseMaxAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var extraMinAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;
                var extraMaxAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;

                for (var i = startLevel + 1; i <= level; i++)
                {
                    var currentRow = _tableSheets.EnhancementCostSheetV3.OrderedList
                        .First(x =>
                            x.Grade == 1 && x.ItemSubType == equipment.ItemSubType && x.Level == i);

                    baseMinAtk *= currentRow.BaseStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    baseMaxAtk *= currentRow.BaseStatGrowthMax.NormalizeFromTenThousandths() + 1;
                    extraMinAtk *= currentRow.ExtraStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    extraMaxAtk *= currentRow.ExtraStatGrowthMax.NormalizeFromTenThousandths() + 1;
                }

                Assert.InRange(
                    resultEquipment.StatsMap.ATK,
                    baseMinAtk + extraMinAtk,
                    baseMaxAtk + extraMaxAtk + 1
                );
            }

            Assert.Equal(
                expectedBlockIndex + 1, // +1 for execution
                resultEquipment.RequiredBlockIndex
            );
            Assert.Equal(preItemUsable.ItemId, slotResult.preItemUsable.ItemId);
            Assert.Equal(preItemUsable.ItemId, resultEquipment.ItemId);
            Assert.Equal(expectedCost, slotResult.gold);
        }

        [Fact]
        public void LoadPlainValue()
        {
            var materialId = Guid.NewGuid();
            var avatarAddress = new PrivateKey().Address;
            var action = new ItemEnhancement
            {
                slotIndex = 1,
                materialIds = new List<Guid>
                {
                    materialId,
                },
                avatarAddress = avatarAddress,
                hammers = new Dictionary<int, int>
                {
                    [1] = 100,
                },
            };
            var plainValue = action.PlainValue;
            var newAction = new ItemEnhancement();
            newAction.LoadPlainValue(plainValue);
            Assert.Equal(action.Id, newAction.Id);
            Assert.Equal(action.avatarAddress, newAction.avatarAddress);
            Assert.Equal(action.slotIndex, newAction.slotIndex);
            var guid = Assert.Single(newAction.materialIds);
            Assert.Equal(materialId, guid);
            Assert.Equal(100, newAction.hammers[1]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_With_Hammer(bool oldStart)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Id == 10110000);
            var startLevel = 0;
            var materialCount = 1;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, startLevel);
            equipment.Exp = (long)row.Exp!;
            var hammerId = ItemEnhancement.HammerIds.First();

            var startExp = equipment.Exp;
            if (oldStart)
            {
                equipment.Exp = 0L;
            }

            _avatarState.inventory.AddItem(equipment, 1);

            var startRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(r =>
                r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                r.Level == startLevel);
            var expectedExpIncrement = 0L;
            var materialIds = new List<Guid>();
            for (var i = 0; i < materialCount; i++)
            {
                var materialId = Guid.NewGuid();
                materialIds.Add(materialId);
                var material =
                    (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, 0);
                material.Exp = (long)row.Exp!;
                expectedExpIncrement += material.Exp;
                _avatarState.inventory.AddItem(material, 1);
            }

            _avatarState.inventory.AddItem(
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet[hammerId]), 3);

            var hammerExp = _tableSheets.EnhancementCostSheetV3.GetHammerExp(hammerId) * 3;

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };
            var preItemUsable = new Equipment((Dictionary)equipment.Serialize());

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update(mail);
            }

            _avatarState.worldInformation.ClearStage(
                1,
                1,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            Assert.Equal(startLevel, equipment.level);

            _initialState = _initialState.SetAvatarState(_avatarAddress, _avatarState);

            var action = new ItemEnhancement
            {
                itemId = default,
                materialIds = materialIds,
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                hammers = new Dictionary<int, int>
                {
                    [hammerId] = 3,
                },
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                RandomSeed = 0,
            });

            var allSlotState = nextState.GetAllCombinationSlotState(_avatarAddress);
            var slotState = allSlotState.GetSlot(0);
            var slotResult = (ItemEnhancement13.ResultModel)slotState.Result;
            var resultEquipment = (Equipment)slotResult.itemUsable;
            var level = resultEquipment.level;
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var expectedTargetRow = _tableSheets.EnhancementCostSheetV3.OrderedList.FirstOrDefault(
                r => r.Grade == equipment.Grade && r.ItemSubType == equipment.ItemSubType &&
                    r.Level == level);
            var expectedCost = (expectedTargetRow?.Cost ?? 0) - (startRow?.Cost ?? 0);
            var expectedBlockIndex =
                (expectedTargetRow?.RequiredBlockIndex ?? 0) - (startRow?.RequiredBlockIndex ?? 0);
            Assert.Equal(default, resultEquipment.ItemId);
            Assert.Equal(startExp + expectedExpIncrement + hammerExp, resultEquipment.Exp);
            Assert.Equal(
                (3_000_000 - expectedCost) * _currency,
                nextState.GetBalance(_agentAddress, _currency)
            );

            var arenaSheet = _tableSheets.ArenaSheet;
            var arenaData = arenaSheet.GetRoundByBlockIndex(1);
            var feeStoreAddress =
                ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
            Assert.Equal(
                expectedCost * _currency,
                nextState.GetBalance(feeStoreAddress, _currency)
            );
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            if (startLevel != level)
            {
                var baseMinAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var baseMaxAtk = (decimal)preItemUsable.StatsMap.BaseATK;
                var extraMinAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;
                var extraMaxAtk = (decimal)preItemUsable.StatsMap.AdditionalATK;

                for (var i = startLevel + 1; i <= level; i++)
                {
                    var currentRow = _tableSheets.EnhancementCostSheetV3.OrderedList
                        .First(x =>
                            x.Grade == 1 && x.ItemSubType == equipment.ItemSubType && x.Level == i);

                    baseMinAtk *= currentRow.BaseStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    baseMaxAtk *= currentRow.BaseStatGrowthMax.NormalizeFromTenThousandths() + 1;
                    extraMinAtk *= currentRow.ExtraStatGrowthMin.NormalizeFromTenThousandths() + 1;
                    extraMaxAtk *= currentRow.ExtraStatGrowthMax.NormalizeFromTenThousandths() + 1;
                }

                Assert.InRange(
                    resultEquipment.StatsMap.ATK,
                    baseMinAtk + extraMinAtk,
                    baseMaxAtk + extraMaxAtk + 1
                );
            }

            Assert.Equal(
                expectedBlockIndex + 1, // +1 for execution
                resultEquipment.RequiredBlockIndex
            );
            Assert.Equal(preItemUsable.ItemId, slotResult.preItemUsable.ItemId);
            Assert.Equal(preItemUsable.ItemId, resultEquipment.ItemId);
            Assert.Equal(expectedCost, slotResult.gold);
        }
    }
}
