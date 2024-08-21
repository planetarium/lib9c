namespace Lib9c.Tests.Action;

using System.Collections.Generic;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Xunit;

public class UnlockCombinationSlotTest
{
    public const int GoldenDustId = 600201;
    public const int RubyDustId = 600202;

    private static readonly Dictionary<string, string> Sheets =
        TableSheetsImporter.ImportSheets();

    private static readonly TableSheets TableSheets = new (Sheets);

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
        var avatarState = new AvatarState(
            avatarAddress,
            agentAddress,
            0,
            TableSheets.GetAvatarSheets(),
            default
        );
        state = state.SetAvatarState(avatarAddress, avatarState);
        return state.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
    }

    public IWorld MintAssetForCost(IWorld state, int slotIndex, ActionContext context, Address agentAddress, Address avatarAddress)
    {
        var sheets = state.GetSheets(
            new[]
            {
                typeof(MaterialItemSheet),
                typeof(UnlockCombinationSlotCostSheet),
            });
        var costSheet = sheets.GetSheet<UnlockCombinationSlotCostSheet>();

        if (!costSheet.ContainsKey(slotIndex))
        {
            throw new InvalidSlotIndexException($"[{nameof(UnlockRuneSlot)}] Index On Sheet : {slotIndex}");
        }

        var price = costSheet[slotIndex];
        var useMaterial = false;

        Inventory inventory = null;

        // Use Crystal
        if (price.CrystalPrice > 0)
        {
            var currency = Currencies.Crystal;
            state = state.MintAsset(context, agentAddress, price.CrystalPrice * currency);
        }

        // Use GoldenDust
        if (price.GoldenDustPrice > 0)
        {
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            inventory = state.GetInventoryV2(avatarAddress);
            var goldenDust =
                ItemFactory.CreateMaterial(
                    materialSheet.Values.First(row => row.Id == GoldenDustId));
            inventory.AddItem(goldenDust, price.GoldenDustPrice);

            useMaterial = true;
        }

        // Use RubyDust
        if (price.RubyDustPrice > 0)
        {
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            inventory ??= state.GetInventoryV2(avatarAddress);
            var rubyDust =
                ItemFactory.CreateMaterial(materialSheet.Values.First(row => row.Id == RubyDustId));
            inventory.AddItem(rubyDust, price.RubyDustPrice);

            useMaterial = true;
        }

        // Use NCG
        if (price.NcgPrice > 0)
        {
            var currency = state.GetGoldCurrency();
            state = state.MintAsset(context, agentAddress, price.NcgPrice * currency);
        }

        // useMaterial이 true일 경우에만 inventory를 업데이트한다.
        if (useMaterial)
        {
            state = state.SetInventory(avatarAddress, inventory);
        }

        return state;
    }

    /// <summary>
    /// Unit test for validating the behavior of the UnlockCombinationSlot action.<br/>
    /// check 'IsUnlock', 'Index' property of slot, and used material count
    /// </summary>
    /// <param name="slotIndex">The index of the combination slot to be unlocked.</param>
    /// <remarks>
    /// This test initializes the game state, mints assets, and then executes the UnlockCombinationSlot action.
    /// It verifies that the currency balances are correctly updated and ensures that the specified combination slot is unlocked.
    /// Additionally, it checks that certain items (GoldenDust and RubyDust) are not present in the avatar's inventory after the action.
    /// </remarks>
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void Execute(int slotIndex)
    {
        var context = new ActionContext();
        var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
        state = MintAssetForCost(state, slotIndex, context, agentAddress, avatarAddress);
        var action = new UnlockCombinationSlot()
        {
            AvatarAddress = avatarAddress,
            SlotIndex = slotIndex,
        };

        var ctx = new ActionContext
        {
            BlockIndex = blockIndex,
            PreviousState = state,
            RandomSeed = 0,
            Signer = agentAddress,
        };

        state = action.Execute(ctx);

        // Check Items
        var ncgCurrency = state.GetGoldCurrency();
        var ncgBalance = state.GetBalance(agentAddress, ncgCurrency);
        var crystalBalance = state.GetBalance(agentAddress, Currencies.Crystal);
        var inventory = state.GetInventoryV2(avatarAddress);
        Assert.Equal("0", ncgBalance.GetQuantityString());
        Assert.Equal("0", crystalBalance.GetQuantityString());
        Assert.False(inventory.HasItem(GoldenDustId));
        Assert.False(inventory.HasItem(RubyDustId));

        // Check Slot
        var combinationSlotState = state.GetAllCombinationSlotState(avatarAddress);
        var slotState = combinationSlotState.GetSlot(slotIndex);
        Assert.True(slotState.IsUnlocked);
        Assert.True(slotState.Index == slotIndex);
    }
}
