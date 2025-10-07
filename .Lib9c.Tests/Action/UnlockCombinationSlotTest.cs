namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Action.Guild.Migration.LegacyModels;
    using Lib9c.Arena;
    using Lib9c.Extensions;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Lib9c.TableData.Item;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
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
                return state;
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
        /// check 'IsUnlock', 'Index' property of slot, and used material count.
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
            state = state.SetDelegationMigrationHeight(0);
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
            var costSheet = state.GetSheet<UnlockCombinationSlotCostSheet>();
            var costRow = costSheet[slotIndex];
            var ncgCurrency = state.GetGoldCurrency();
            var ncgBalance = state.GetBalance(agentAddress, ncgCurrency);
            var crystalBalance = state.GetBalance(agentAddress, Currencies.Crystal);
            var inventory = state.GetInventoryV2(avatarAddress);
            Assert.Equal("0", ncgBalance.GetQuantityString());
            Assert.Equal("0", crystalBalance.GetQuantityString());
            if (costRow.CrystalPrice > 0)
            {
                Assert.True(state.GetBalance(Addresses.RewardPool, Currencies.Crystal) > 0 * Currencies.Crystal);
            }

            if (costRow.NcgPrice > 0)
            {
                Assert.True(state.GetBalance(ArenaHelper.DeriveArenaAddress(0, 0), ncgCurrency) > 0 * ncgCurrency);
            }

            Assert.False(inventory.HasItem(GoldenDustId));
            Assert.False(inventory.HasItem(RubyDustId));

            // Check Slot
            var combinationSlotState = state.GetAllCombinationSlotState(avatarAddress);
            var slotState = combinationSlotState.GetSlot(slotIndex);
            Assert.True(slotState.IsUnlocked);
            Assert.True(slotState.Index == slotIndex);
        }

        /// <summary>
        /// Unit test for validating the unlocking of all combination slots for an avatar.
        /// </summary>
        /// <remarks>
        /// This test initializes the game state, retrieves all combination slots, and iterates through each slot.
        /// For locked slots, it mints the necessary assets and executes the UnlockCombinationSlot action to unlock them.
        /// Finally, it verifies that all slots are unlocked successfully.
        /// </remarks>
        [Fact]
        public void Execute_All()
        {
            var context = new ActionContext();
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var allCombinationSlotState = state.GetAllCombinationSlotState(avatarAddress);

            foreach (var slotState in allCombinationSlotState)
            {
                if (slotState.IsUnlocked)
                {
                    continue;
                }

                state = MintAssetForCost(state, slotState.Index, context, agentAddress, avatarAddress);

                var action = new UnlockCombinationSlot()
                {
                    AvatarAddress = avatarAddress,
                    SlotIndex = slotState.Index,
                };

                var ctx = new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousState = state,
                    RandomSeed = 0,
                    Signer = agentAddress,
                };

                state = action.Execute(ctx);
            }

            allCombinationSlotState = state.GetAllCombinationSlotState(avatarAddress);
            foreach (var slotState in allCombinationSlotState)
            {
                Assert.True(slotState.IsUnlocked);
            }
        }

        /// <summary>
        /// Unit test for validating the behavior when attempting to unlock a default combination slot that is already unlocked.
        /// </summary>
        /// <param name="slotIndex">The index of the combination slot to be tested.</param>
        /// <remarks>
        /// This test initializes the game state, mints the necessary assets for the given slot, and attempts to execute the UnlockCombinationSlot action.
        /// It verifies that the action throws a SlotAlreadyUnlockedException, indicating that the specified slot is already unlocked by default.
        /// </remarks>
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void Execute_DefaultSlot(int slotIndex)
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

            Assert.Throws<SlotAlreadyUnlockedException>(() => action.Execute(ctx));
        }

        /// <summary>
        /// Unit test for validating the behavior of the UnlockCombinationSlot action when provided with an invalid slot index.
        /// </summary>
        /// <param name="slotIndex">The invalid index of the combination slot to be tested.</param>
        /// <remarks>
        /// This test initializes the game state, attempts to mint the necessary assets for an invalid slot index, and then tries to execute the UnlockCombinationSlot action.
        /// It verifies that the action throws an InvalidSlotIndexException, indicating that the specified slot index is out of the valid range.
        /// </remarks>
        [Theory]
        [InlineData(-1)]
        [InlineData(0x5f5f)]
        public void Execute_ValidateSlotIndex(int slotIndex)
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

            Assert.Throws<InvalidSlotIndexException>(() => action.Execute(ctx));
        }

        /// <summary>
        /// Unit test for validating the behavior of the UnlockCombinationSlot action when there are insufficient assets.
        /// </summary>
        /// <param name="slotIndex">The index of the combination slot to be tested.</param>
        /// <remarks>
        /// This test initializes the game state and attempts to execute the UnlockCombinationSlot action with different slot indices.
        /// Depending on the slot index, it expects specific exceptions to be thrown, such as InsufficientBalanceException or NotEnoughMaterialException,
        /// indicating that the action cannot be completed due to a lack of necessary resources.
        /// </remarks>
        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Execute_InsufficientAsset(int slotIndex)
        {
            var context = new ActionContext();
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
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

            var sheets = state.GetSheets(
                new[]
                {
                    typeof(MaterialItemSheet),
                    typeof(UnlockCombinationSlotCostSheet),
                });
            var costSheet = sheets.GetSheet<UnlockCombinationSlotCostSheet>();

            if (!costSheet.ContainsKey(slotIndex))
            {
                return;
            }

            var price = costSheet[slotIndex];
            var useMaterial = price.GoldenDustPrice > 0 || price.RubyDustPrice > 0;
            var needCheckBalance = price.CrystalPrice > 0 || price.NcgPrice > 0;

            if (useMaterial)
            {
                Assert.ThrowsAny<NotEnoughMaterialException>(() => action.Execute(ctx));
            }

            if (needCheckBalance)
            {
                Assert.ThrowsAny<InsufficientBalanceException>(() => action.Execute(ctx));
            }
        }
    }
}
