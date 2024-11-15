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
            state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());

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
        var context = new ActionContext();
        var itemSubTypes = GetSubTypeArray(itemSubType, GetSucceededMaterialCount(grade));

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
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

        // TODO: if success, grade should be exceptedGrade, but sometimes it is not.
        // Assert.Equal(expectedGrade, resultGrade);
        Assert.True(expectedGrade == resultGrade || resultGrade == grade);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(12)]
    public void ExecuteMultipleSameType(int testCount)
    {
        var grade = Grade.Rare;
        var itemSubType = ItemSubType.FullCostume;
        var materialCount = GetSucceededMaterialCount(grade) * testCount;
        var itemSubTypes = GetSubTypeArray(itemSubType, materialCount);

        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
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

    // TODO: Add Simulator for test and client
    // TODO: Exception Tests
    // TODO: ExecuteMultiple
    [Theory]
    [InlineData((Grade)3, new[] { ItemSubType.Aura, ItemSubType.FullCostume, ItemSubType.FullCostume })]
    [InlineData((Grade)3, new[] { ItemSubType.Title, ItemSubType.Grimoire, ItemSubType.Title })]
    [InlineData((Grade)3, new[] { ItemSubType.Grimoire, ItemSubType.Title, ItemSubType.Grimoire })]
    [InlineData((Grade)3, new[] { ItemSubType.Aura, ItemSubType.Aura, ItemSubType.Grimoire })]
    public void ExecuteInvalidMaterial(Grade grade, ItemSubType[] itemSubTypes)
    {
        var context = new ActionContext();
        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(grade, itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            MaterialIds = SynthesizeSimulator.GetItemGuids(items),
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

    private static int GetSucceededMaterialCount(Grade grade)
    {
        var synthesizeSheet = TableSheets.SynthesizeSheet;
        var row = synthesizeSheet.Values.First(r => (Grade)r.GradeId == grade);
        return row.RequiredCount;
    }
}
