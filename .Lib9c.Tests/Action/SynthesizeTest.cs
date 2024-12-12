namespace Lib9c.Tests.Action;

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.EnumType;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.Helper;
using Xunit;

using Sheets = System.Collections.Generic.Dictionary<System.Type, (Libplanet.Crypto.Address, Nekoyume.TableData.ISheet)>;

public class SynthesizeTest
{
    private static readonly Dictionary<string, string> Sheets =
        TableSheetsImporter.ImportSheets();

    private static readonly TableSheets TableSheets = new (Sheets);

    private static int _randomSeed;

#pragma warning disable CS0618
    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
    private readonly Currency _goldCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

    /// <summary>
    /// Initializes the game state for testing.
    /// Sets up an initial agent, avatar, and world state with relevant table sheets.
    /// </summary>
    /// <param name="agentAddress">The address of the agent to be created.</param>
    /// <param name="avatarAddress">The address of the avatar to be created.</param>
    /// <param name="blockIndex">The initial block index.</param>
    /// <returns>The initialized world state.</returns>
    public IWorld Init(out Address agentAddress, out Address avatarAddress, out long blockIndex)
    {
        agentAddress = new PrivateKey().Address;
        avatarAddress = new PrivateKey().Address;
        blockIndex = TableSheets.ArenaSheet.Values.First().Round
            .OrderBy(x => x.StartBlockIndex)
            .First()
            .StartBlockIndex;

        var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
        var state = new World(MockUtil.MockModernWorldState)
            .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
            .SetAgentState(agentAddress, new AgentState(agentAddress));

        foreach (var (key, value) in Sheets)
        {
            state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
        }

        var gameConfigState = new GameConfigState(Sheets[nameof(GameConfigSheet)]);
        var avatarState = AvatarState.Create(
            avatarAddress,
            agentAddress,
            0,
            TableSheets.GetAvatarSheets(),
            default
        );
        state = state.SetAvatarState(avatarAddress, avatarState);
        return state.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
    }

    /// <summary>
    /// Tests the synthesis process for a single item.
    /// Verifies that the resulting item matches the expected grade and type.
    /// </summary>
    /// <param name="grade">The grade of the material used in synthesis.</param>
    /// <param name="itemSubType">The subtype of the item to synthesize.</param>
    [Theory]
    [InlineData((Grade)3, ItemSubType.FullCostume)]
    [InlineData((Grade)4, ItemSubType.FullCostume)]
    [InlineData((Grade)5, ItemSubType.FullCostume)]
    [InlineData((Grade)3, ItemSubType.Title)]
    [InlineData((Grade)4, ItemSubType.Title)]
    [InlineData((Grade)5, ItemSubType.Title)]
    [InlineData((Grade)3, ItemSubType.Grimoire)]
    [InlineData((Grade)4, ItemSubType.Grimoire)]
    [InlineData((Grade)5, ItemSubType.Grimoire)]
    [InlineData((Grade)6, ItemSubType.Grimoire)]
    [InlineData((Grade)1, ItemSubType.Aura)]
    [InlineData((Grade)2, ItemSubType.Aura)]
    [InlineData((Grade)3, ItemSubType.Aura)]
    [InlineData((Grade)4, ItemSubType.Aura)]
    [InlineData((Grade)5, ItemSubType.Aura)]
    [InlineData((Grade)6, ItemSubType.Aura)]
    public void ExecuteSingle(Grade grade, ItemSubType itemSubType)
    {
        var itemSubTypes = GetSubTypeArray(itemSubType, GetSucceededMaterialCount(itemSubType, grade));

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);
        state = state.SetActionPoint(avatarAddress, 120);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
            ChargeAp = false,
            MaterialGradeId = (int)grade,
            MaterialItemSubTypeId = (int)itemSubType,
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        state = action.Execute(ctx);
        var inventory = state.GetInventoryV2(avatarAddress);

        // Tests - result inventory should have only one item in this case.
        Assert.Single(inventory.Items);
        var firstItem = inventory.Items.First().item;
        Assert.True(firstItem.ItemSubType == itemSubTypes[0]);

        var subType = firstItem.ItemSubType;
        var expectedGrade = Grade.Normal;
        var resultGrade = (Grade)firstItem.Grade;
        switch (subType)
        {
            case ItemSubType.FullCostume:
            case ItemSubType.Title:
                if (firstItem is not Costume costume)
                {
                    throw new InvalidCastException("Invalid item type, is not Costume");
                }

                resultGrade = (Grade)costume.Grade;
                expectedGrade = SynthesizeSimulator.GetUpgradeGrade(grade, subType, TableSheets.CostumeItemSheet);
                break;
            case ItemSubType.Aura:
            case ItemSubType.Grimoire:
                if (firstItem is not ItemUsable itemUsable)
                {
                    throw new InvalidCastException("Invalid item type, is not ItemUsable");
                }

                resultGrade = (Grade)itemUsable.Grade;
                expectedGrade = SynthesizeSimulator.GetUpgradeGrade(grade, subType, TableSheets.EquipmentItemSheet);
                break;
        }

        var inputData = new SynthesizeSimulator.InputData()
        {
            Grade = grade,
            ItemSubType = itemSubType,
            MaterialCount = itemSubTypes.Length,
            SynthesizeSheet = TableSheets.SynthesizeSheet,
            SynthesizeWeightSheet = TableSheets.SynthesizeWeightSheet,
            CostumeItemSheet = TableSheets.CostumeItemSheet,
            EquipmentItemSheet = TableSheets.EquipmentItemSheet,
            EquipmentItemRecipeSheet = TableSheets.EquipmentItemRecipeSheet,
            EquipmentItemSubRecipeSheetV2 = TableSheets.EquipmentItemSubRecipeSheetV2,
            EquipmentItemOptionSheet = TableSheets.EquipmentItemOptionSheet,
            SkillSheet = TableSheets.SkillSheet,
            RandomObject = new TestRandom(),
        };

        var result = SynthesizeSimulator.Simulate(inputData)[0];
        if (result.IsSuccess)
        {
            Assert.Equal(expectedGrade, resultGrade);
        }
        else
        {
            Assert.Equal(resultGrade, grade);
        }
    }

    /// <summary>
    /// Tests the synthesis process for multiple items.
    /// Verifies that the resulting items match the expected grades and types.
    /// The test case also checks whether the equipment has a recipe.
    /// </summary>
    /// <param name="grade">The grade of the material used in synthesis.</param>
    /// <param name="itemSubType">The subtype of the items to synthesize.</param>
    [Theory]
    [InlineData((Grade)3, ItemSubType.FullCostume)]
    [InlineData((Grade)4, ItemSubType.FullCostume)]
    [InlineData((Grade)5, ItemSubType.FullCostume)]
    [InlineData((Grade)3, ItemSubType.Title)]
    [InlineData((Grade)4, ItemSubType.Title)]
    [InlineData((Grade)5, ItemSubType.Title)]
    [InlineData((Grade)3, ItemSubType.Grimoire)]
    [InlineData((Grade)4, ItemSubType.Grimoire)]
    [InlineData((Grade)5, ItemSubType.Grimoire)]
    [InlineData((Grade)6, ItemSubType.Grimoire)]
    [InlineData((Grade)1, ItemSubType.Aura)]
    [InlineData((Grade)2, ItemSubType.Aura)]
    [InlineData((Grade)3, ItemSubType.Aura)]
    [InlineData((Grade)4, ItemSubType.Aura)]
    [InlineData((Grade)5, ItemSubType.Aura)]
    [InlineData((Grade)6, ItemSubType.Aura)]
    public void ExecuteMultiple(Grade grade, ItemSubType itemSubType)
    {
        var testCount = 100;
        var itemSubTypes = GetSubTypeArray(itemSubType, testCount * GetSucceededMaterialCount(itemSubType, grade));

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);
        state = state.SetActionPoint(avatarAddress, 120);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
            ChargeAp = false,
            MaterialGradeId = (int)grade,
            MaterialItemSubTypeId = (int)itemSubType,
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        action.Execute(ctx);
        var inputData = new SynthesizeSimulator.InputData()
        {
            Grade = grade,
            ItemSubType = itemSubType,
            MaterialCount = itemSubTypes.Length,
            SynthesizeSheet = TableSheets.SynthesizeSheet,
            SynthesizeWeightSheet = TableSheets.SynthesizeWeightSheet,
            CostumeItemSheet = TableSheets.CostumeItemSheet,
            EquipmentItemSheet = TableSheets.EquipmentItemSheet,
            EquipmentItemRecipeSheet = TableSheets.EquipmentItemRecipeSheet,
            EquipmentItemSubRecipeSheetV2 = TableSheets.EquipmentItemSubRecipeSheetV2,
            EquipmentItemOptionSheet = TableSheets.EquipmentItemOptionSheet,
            SkillSheet = TableSheets.SkillSheet,
            RandomObject = new TestRandom(),
        };

        var resultList = SynthesizeSimulator.Simulate(inputData);
        foreach (var result in resultList)
        {
            // Check Grade
            if (result.IsSuccess)
            {
                Assert.Equal((int)grade + 1, result.ItemBase.Grade);

                var weightSheet = TableSheets.SynthesizeWeightSheet;
                var weightRow = weightSheet.Values.FirstOrDefault(r => r.ItemId == result.ItemBase.Id);

                if (weightRow != null)
                {
                    Assert.True(weightRow.Weight != 0);
                }
            }
            else
            {
                Assert.Equal((int)grade, result.ItemBase.Grade);
            }

            if (result.IsEquipment)
            {
                Assert.True(result.RecipeId != 0);
                Assert.True(result.SubRecipeId != 0);
            }
        }
    }

    /// <summary>
    /// Tests the synthesis action when there are not enough action points.
    /// Verifies that the action throws a NotEnoughActionPointException.
    /// </summary>
    [Fact]
    public void ExecuteNotEnoughActionPoint()
    {
        var grade = Grade.Rare;
        var itemSubType = ItemSubType.FullCostume;
        var itemSubTypes = GetSubTypeArray(itemSubType, GetSucceededMaterialCount(itemSubType, grade));

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
            ChargeAp = false,
            MaterialGradeId = (int)grade,
            MaterialItemSubTypeId = (int)itemSubType,
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        Assert.Throws<NotEnoughActionPointException>(() => action.Execute(ctx));
    }

    /// <summary>
    /// Tests the synthesis of multiple items of the same type.
    /// Verifies that the resulting items are of the correct type and grade.
    /// </summary>
    /// <param name="testCount">The number of items to synthesize.</param>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(12)]
    public void ExecuteMultipleSameType(int testCount)
    {
        var grade = Grade.Rare;
        var itemSubType = ItemSubType.FullCostume;
        var materialCount = GetSucceededMaterialCount(itemSubType, grade) * testCount;
        var itemSubTypes = GetSubTypeArray(itemSubType, materialCount);

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);
        state = state.SetActionPoint(avatarAddress, 120);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
            ChargeAp = false,
            MaterialGradeId = (int)grade,
            MaterialItemSubTypeId = (int)itemSubType,
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        state = action.Execute(ctx);
        var inventory = state.GetInventoryV2(avatarAddress);

        // Assert
        Assert.Equal(testCount, inventory.Items.Count);
        foreach (var item in inventory.Items.Select(i => i.item))
        {
            Assert.Equal(itemSubType, item.ItemSubType);
            var expectedGrade = SynthesizeSimulator.GetTargetGrade((int)grade);
            Assert.True(item.Grade == expectedGrade || item.Grade == (int)grade);
        }
    }

    /// <summary>
    /// Tests the synthesis action with invalid material combinations.
    /// Verifies that the action throws an InvalidMaterialException.
    /// </summary>
    /// <param name="grade">The grade of the material used in synthesis.</param>
    /// <param name="itemSubTypes">An array of invalid item subtypes to use in synthesis.</param>
    [Theory]
    [InlineData((Grade)3, new[] { ItemSubType.Aura, ItemSubType.FullCostume, ItemSubType.FullCostume })]
    [InlineData((Grade)3, new[] { ItemSubType.Title, ItemSubType.Grimoire, ItemSubType.Title })]
    [InlineData((Grade)3, new[] { ItemSubType.Grimoire, ItemSubType.Title, ItemSubType.Grimoire })]
    [InlineData((Grade)3, new[] { ItemSubType.Aura, ItemSubType.Aura, ItemSubType.Grimoire })]
    public void ExecuteInvalidMaterial(Grade grade, ItemSubType[] itemSubTypes)
    {
        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);
        state = state.SetActionPoint(avatarAddress, 120);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
            ChargeAp = false,
            MaterialGradeId = (int)grade,
            MaterialItemSubTypeId = (int)itemSubTypes[0],
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        Assert.Throws<InvalidMaterialException>(() => action.Execute(ctx));
    }

    private static (IWorld, List<ItemBase>) UpdateItemsFromSubType(Grade grade, ItemSubType[] itemSubTypes, IWorld state, Address avatarAddress)
    {
        var avatarState = state.GetAvatarState(avatarAddress);
        var items = new List<ItemBase>();
        foreach (var subType in itemSubTypes)
        {
            var item = GetItem(grade, subType);
            avatarState.inventory.AddItem(item);

            switch (item)
            {
                case Costume costume:
                    items.Add(costume);
                    break;
                case ItemUsable itemUsable:
                    items.Add(itemUsable);
                    break;
                default:
                    throw new ArgumentException($"Unexpected item type: {item.GetType()}", nameof(item));
            }
        }

        return (state.SetInventory(avatarAddress, avatarState.inventory), items);
    }

    private static ItemBase GetItem(Grade grade, ItemSubType type)
    {
        switch (type)
        {
            case ItemSubType.FullCostume:
                return GetFirstCostume(grade);
            case ItemSubType.Title:
                return GetFirstTitle(grade);
            case ItemSubType.Aura:
                return GetFirstAura(grade);
            case ItemSubType.Grimoire:
                return GetFirstGrimoire(grade);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid ItemSubType");
        }
    }

    private static ItemBase GetFirstCostume(Grade grade)
    {
        var sheet = TableSheets.CostumeItemSheet;
        var row = sheet
            .Where(r => (Grade)r.Value.Grade == grade)
            .FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.FullCostume).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstTitle(Grade grade)
    {
        var sheet = TableSheets.CostumeItemSheet;
        var row = sheet
            .Where(r => (Grade)r.Value.Grade == grade)
            .FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Title).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstAura(Grade grade)
    {
        var sheet = TableSheets.EquipmentItemSheet;
        var row = sheet
            .Where(r => (Grade)r.Value.Grade == grade)
            .FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Aura).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstGrimoire(Grade grade)
    {
        var sheet = TableSheets.EquipmentItemSheet;
        var row = sheet
            .Where(r => (Grade)r.Value.Grade == grade)
            .FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Grimoire).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemSubType[] GetSubTypeArray(ItemSubType subtype, int count)
    {
        var subTypes = new ItemSubType[count];
        for (var i = 0; i < count; i++)
        {
            subTypes[i] = subtype;
        }

        return subTypes;
    }

    private static int GetSucceededMaterialCount(ItemSubType itemSubType, Grade grade)
    {
        var synthesizeSheet = TableSheets.SynthesizeSheet;
        var row = synthesizeSheet.Values.First(r => (Grade)r.GradeId == grade);
        return row.RequiredCountDict[itemSubType].RequiredCount;
    }
}
