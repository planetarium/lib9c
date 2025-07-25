namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Lib9c.Tests.Fixtures.TableCSV.Item;
    using Lib9c.Tests.Fixtures.TableCSV.Quest;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RapidCombinationTest
    {
        private readonly IWorld _initialState;

        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public RapidCombinationTest()
        {
            _initialState = new World(MockUtil.MockModernWorldState);
            Dictionary<string, string> sheets;
            (_initialState, sheets) = InitializeUtil.InitializeTableSheets(
                _initialState,
                sheetsOverride: new Dictionary<string, string>
                {
                    {
                        "EquipmentItemRecipeSheet",
                        EquipmentItemRecipeSheetFixtures.Default
                    },
                    {
                        "EquipmentItemSubRecipeSheet",
                        EquipmentItemSubRecipeSheetFixtures.V1
                    },
                    {
                        "GameConfigSheet",
                        GameConfigSheetFixtures.Default
                    },
                    {
                        nameof(CombinationEquipmentQuestSheet),
                        CombinationEquipmentQuestSheetFixtures.Default
                    },
                });
            _tableSheets = new TableSheets(sheets);
            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = new PrivateKey().Address;
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetLegacyState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState);
        }

        [Fact]
        public void Execute()
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 83);
            avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), 100);
            Assert.True(avatarState.inventory.HasFungibleItem(row.ItemId, 0, 183));

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 200;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update2(mail);

            var allSlotState = new AllCombinationSlotState();
            allSlotState.AddSlot(_avatarAddress);
            var slotState = allSlotState.GetSlot(0);
            slotState.Update(result, 0, requiredBlockIndex);

            var tempState = _initialState
                .SetCombinationSlotState(_avatarAddress, allSlotState)
                .SetAvatarState(_avatarAddress, avatarState);

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = tempState,
                    Signer = _agentAddress,
                    BlockIndex = 51,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(equipment.ItemId, item.ItemId);
            Assert.Equal(51, item.RequiredBlockIndex);
        }

        [Fact]
        public void Execute_Many()
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.Hourglass);

            var numOfHourglass = 83 * AvatarState.DefaultCombinationSlotCount;
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), numOfHourglass);

            var numOfTradableHourglass = 100 * AvatarState.DefaultCombinationSlotCount;
            avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), numOfTradableHourglass);

            Assert.True(avatarState.inventory.HasFungibleItem(row.ItemId, 0, numOfHourglass + numOfTradableHourglass));

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 200;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

            var targetState = _initialState;
            var allSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; ++i)
            {
                var result = new CombinationConsumable5.ResultModel
                {
                    actionPoint = 0,
                    gold = 0,
                    materials = new Dictionary<Material, int>(),
                    itemUsable = equipment,
                    recipeId = 0,
                    itemType = ItemType.Equipment,
                };

                var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
                result.id = mail.id;
                avatarState.Update2(mail);

                targetState = targetState
                    .SetAvatarState(_avatarAddress, avatarState);

                allSlotState.AddSlot(_avatarAddress, i);
                var slotState = allSlotState.GetSlot(i);
                slotState.Update(result, 0, requiredBlockIndex);
            }

            targetState = targetState
                .SetCombinationSlotState(_avatarAddress, allSlotState);

            var slotIndexList = Enumerable.Range(0, AvatarState.DefaultCombinationSlotCount).ToList();
            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = slotIndexList,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = targetState,
                    Signer = _agentAddress,
                    BlockIndex = 51,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(equipment.ItemId, item.ItemId);
            Assert.Equal(51, item.RequiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_CombinationSlotResultNullException()
        {
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0));
            var slotState = new CombinationSlotState(slotAddress, 0);
            slotState.Update(null, 0, 0);

            var tempState = _initialState
                .SetLegacyState(slotAddress, slotState.Serialize());

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            Assert.Throws<CombinationSlotResultNullException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = tempState,
                        Signer = _agentAddress,
                        BlockIndex = 1,
                    }));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 100)]
        public void Execute_Throw_RequiredBlockIndexException(int itemRequiredBlockIndex, int contextBlockIndex)
        {
            const int avatarClearedStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                avatarClearedStage);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                itemRequiredBlockIndex);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var allSlotState = new AllCombinationSlotState();
            var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
            allSlotState.AddSlot(addr);
            var slotState = allSlotState.GetSlot(0);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState);

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            Assert.Throws<RequiredBlockIndexException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = tempState,
                        Signer = _agentAddress,
                        BlockIndex = contextBlockIndex,
                    }));
        }

        [Theory]
        [InlineData(0, 0, 0, 40)]
        [InlineData(0, 1, 2, 40)]
        [InlineData(22, 0, 0, 40)]
        [InlineData(0, 22, 0, 40)]
        [InlineData(0, 22, 2, 40)]
        [InlineData(2, 10, 2, 40)]
        public void Execute_Throw_NotEnoughMaterialException(int materialCount, int tradableCount, long blockIndex, int requiredCount)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            if (materialCount > 0)
            {
                avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), materialCount);
            }

            if (tradableCount > 0)
            {
                var material = ItemFactory.CreateTradableMaterial(row);
                material.RequiredBlockIndex = blockIndex;
                avatarState.inventory.AddItem(material, tradableCount);
            }

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * requiredCount;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update2(mail);

            var allSlotState = new AllCombinationSlotState();
            var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
            allSlotState.AddSlot(addr);
            var slotState = allSlotState.GetSlot(0);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState);

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            Assert.Throws<NotEnoughMaterialException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = tempState,
                        Signer = _agentAddress,
                        BlockIndex = 51,
                    }));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ResultModelDeterministic(int? subRecipeId)
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var row2 = _tableSheets.MaterialItemSheet.Values.Last();

            Assert.True(row.Id < row2.Id);

            var material = ItemFactory.CreateMaterial(row);
            var material2 = ItemFactory.CreateMaterial(row2);

            var itemUsable = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.Values.First(), default, 0);
            var r = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>
                {
                    [material] = 1,
                    [material2] = 1,
                },
                itemUsable = itemUsable,
            };
            var result = new RapidCombination0.ResultModel((Dictionary)r.Serialize())
            {
                cost = new Dictionary<Material, int>
                {
                    [material] = 1,
                    [material2] = 1,
                },
            };

            var r2 = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>
                {
                    [material2] = 1,
                    [material] = 1,
                },
                itemUsable = itemUsable,
            };

            var result2 = new RapidCombination0.ResultModel((Dictionary)r2.Serialize())
            {
                cost = new Dictionary<Material, int>
                {
                    [material2] = 1,
                    [material] = 1,
                },
            };

            Assert.Equal(result.Serialize(), result2.Serialize());
        }

        [Fact]
        public void Execute_Throw_RequiredAppraiseBlockException()
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 22);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 40;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);

            var allSlotState = new AllCombinationSlotState();
            var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
            allSlotState.AddSlot(addr);
            var slotState = allSlotState.GetSlot(0);
            slotState.Update(result, 10, 50);

            var tempState = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState);

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            Assert.Throws<AppraiseBlockNotReachedException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = tempState,
                        Signer = _agentAddress,
                        BlockIndex = 1,
                    }));
        }

        [Theory]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        public void Execute_NotThrow_InvalidOperationException_When_TargetSlotCreatedBy(
            int itemEnhancementResultModelNumber)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 83);
            avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), 100);
            Assert.True(avatarState.inventory.HasFungibleItem(row.ItemId, 0, 183));

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet
                .OrderedList.First(e => e.Grade >= 1);
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 200;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            var materialEquipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);
            avatarState.inventory.AddItem(materialEquipment);

            AttachmentActionResult resultModel = null;
            var random = new TestRandom();
            var mailId = random.GenerateRandomGuid();
            var preItemUsable = new Equipment(equipment.Serialize());
            switch (itemEnhancementResultModelNumber)
            {
                case 7:
                {
                    equipment = ItemEnhancement7.UpgradeEquipment(equipment);
                    resultModel = new ItemEnhancement7.ResultModel
                    {
                        id = mailId,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId, },
                    };

                    break;
                }

                case 9:
                {
                    Assert.True(
                        ItemEnhancement9.TryGetRow(
                            equipment,
                            _tableSheets.EnhancementCostSheetV2,
                            out var costRow));
                    var equipmentResult = ItemEnhancement9.GetEnhancementResult(costRow, random);
                    equipment.LevelUp(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement9.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement9.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId, },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement9.EnhancementResult.GreatSuccess,
                    };

                    break;
                }

                case 10:
                {
                    Assert.True(
                        ItemEnhancement10.TryGetRow(
                            equipment,
                            _tableSheets.EnhancementCostSheetV2,
                            out var costRow));
                    var equipmentResult = ItemEnhancement10.GetEnhancementResult(costRow, random);
                    equipment.LevelUp(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement10.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement10.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId, },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement10.EnhancementResult.GreatSuccess,
                    };

                    break;
                }

                case 11:
                {
                    Assert.True(
                        ItemEnhancement11.TryGetRow(
                            equipment,
                            _tableSheets.EnhancementCostSheetV2,
                            out var costRow));
                    var equipmentResult = ItemEnhancement11.GetEnhancementResult(costRow, random);
                    equipment.LevelUp(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement11.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement11.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId, },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement11.EnhancementResult.GreatSuccess,
                        CRYSTAL = 0 * CrystalCalculator.CRYSTAL,
                    };

                    break;
                }

                default:
                    break;
            }

            // NOTE: Do not update `mail`, because this test assumes that the `mail` was removed.
            {
                // var mail = new ItemEnhanceMail(resultModel, 0, random.GenerateRandomGuid(), requiredBlockIndex);
                // avatarState.Update(mail);
            }

            var allSlotState = new AllCombinationSlotState();
            var addr = CombinationSlotState.DeriveAddress(_avatarAddress, 0);
            allSlotState.AddSlot(addr);
            var slotState = allSlotState.GetSlot(0);
            slotState.Update(resultModel, 0, requiredBlockIndex);

            var tempState = _initialState
                .SetCombinationSlotState(_avatarAddress, allSlotState)
                .SetAvatarState(_avatarAddress, avatarState);

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndexList = new List<int> { 0, },
            };

            action.Execute(
                new ActionContext
                {
                    PreviousState = tempState,
                    Signer = _agentAddress,
                    BlockIndex = 51,
                });
        }
    }
}
