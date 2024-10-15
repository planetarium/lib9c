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
using Nekoyume.Module;
using Nekoyume.TableData;
using Xunit;

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
    [InlineData(new[] { ItemSubType.FullCostume, ItemSubType.FullCostume, ItemSubType.FullCostume })]
    [InlineData(new[] { ItemSubType.Title, ItemSubType.Title, ItemSubType.Title })]
    [InlineData(new[] { ItemSubType.Grimoire, ItemSubType.Grimoire, ItemSubType.Grimoire })]
    [InlineData(new[] { ItemSubType.Aura, ItemSubType.Aura, ItemSubType.Aura })]
    public void Execute(ItemSubType[] itemSubTypes)
    {
        var context = new ActionContext();
        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            ItemSubTypeValue = (int)itemSubTypes[0],
            MaterialIds = items,
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

        // 현재 아이템 3개를 넣어 1개가 나오는 구조라 이런 형태로 체크, 추후 변경될 수 있음
        Assert.Single(inventory.Items);
        Assert.True(inventory.Items.First().item.ItemSubType == itemSubTypes[0]);
    }

    [Theory]
    [InlineData(new[] { ItemSubType.Aura, ItemSubType.FullCostume, ItemSubType.FullCostume })]
    [InlineData(new[] { ItemSubType.Title, ItemSubType.Grimoire, ItemSubType.Title })]
    [InlineData(new[] { ItemSubType.Grimoire, ItemSubType.Title, ItemSubType.Grimoire })]
    [InlineData(new[] { ItemSubType.Aura, ItemSubType.Aura, ItemSubType.Grimoire })]
    public void ExecuteMixedSubTypes(ItemSubType[] itemSubTypes)
    {
        var context = new ActionContext();
        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        (state, var items) = UpdateItemsFromSubType(itemSubTypes, state, avatarAddress);

        var action = new Synthesize()
        {
            AvatarAddress = avatarAddress,
            ItemSubTypeValue = (int)itemSubTypes[0],
            MaterialIds = items,
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

    private static (IWorld, List<Guid>) UpdateItemsFromSubType(ItemSubType[] itemSubTypes, IWorld state, Address avatarAddress)
    {
        var avatarState = state.GetAvatarState(avatarAddress);
        var items = new List<Guid>();
        foreach (var subType in itemSubTypes)
        {
            var item = GetItem(subType);
            avatarState.inventory.AddItem(item);

            switch (item)
            {
                case Costume costume:
                    items.Add(costume.ItemId);
                    break;
                case ItemUsable itemUsable:
                    items.Add(itemUsable.ItemId);
                    break;
                default:
                    throw new ArgumentException($"Unexpected item type: {item.GetType()}", nameof(item));
            }
        }

        return (state.SetInventory(avatarAddress, avatarState.inventory), items);
    }

    private static ItemBase GetItem(ItemSubType type)
    {
        switch (type)
        {
            case ItemSubType.FullCostume:
                return GetFirstCostume();
            case ItemSubType.Title:
                return GetFirstTitle();
            case ItemSubType.Aura:
                return GetFirstAura();
            case ItemSubType.Grimoire:
                return GetFirstGrimoire();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static ItemBase GetFirstCostume()
    {
        var row = TableSheets.CostumeItemSheet.FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.FullCostume).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstTitle()
    {
        var row = TableSheets.CostumeItemSheet.FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Title).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstAura()
    {
        var row = TableSheets.EquipmentItemSheet.FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Aura).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }

    private static ItemBase GetFirstGrimoire()
    {
        var row = TableSheets.EquipmentItemSheet.FirstOrDefault(r => r.Value.ItemSubType == ItemSubType.Grimoire).Value;
        Assert.NotNull(row);

        return ItemFactory.CreateItem(row, new TestRandom(_randomSeed++));
    }
}
