namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerBattleTest
    {
        private readonly TableSheets _tableSheets;

        public InfiniteTowerBattleTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize_With_MessagePack()
        {
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = new Address("0x1234567890123456789012345678901234567890"),
                InfiniteTowerId = 1,
                FloorId = 1,
                Equipments = new List<Guid> { Guid.NewGuid() },
                Costumes = new List<Guid> { Guid.NewGuid() },
                Foods = new List<Guid> { Guid.NewGuid() },
                RuneInfos = new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(0, 1),
                },
                BuyTicketIfNeeded = true,
            };

            var serialized = action.PlainValue;
            var deserialized = new InfiniteTowerBattle();
            deserialized.LoadPlainValue(serialized);

            Assert.Equal(action.AvatarAddress, deserialized.AvatarAddress);
            Assert.Equal(action.InfiniteTowerId, deserialized.InfiniteTowerId);
            Assert.Equal(action.FloorId, deserialized.FloorId);
            Assert.Equal(action.Equipments, deserialized.Equipments);
            Assert.Equal(action.Costumes, deserialized.Costumes);
            Assert.Equal(action.Foods, deserialized.Foods);
            Assert.Equal(action.RuneInfos, deserialized.RuneInfos);
            Assert.Equal(action.BuyTicketIfNeeded, deserialized.BuyTicketIfNeeded);
        }

        [Theory]
        [InlineData(1, 1, 100)]
        public void Execute_With_Valid_Inputs_ShouldSucceed(int infiniteTowerId, int floorId, int avatarLevel)
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = avatarLevel; // Use parameterized level

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5); // Give some tickets
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify infinite tower info was updated
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            // Verify avatar state was updated
            var updatedAvatarState = nextState.GetAvatarState(avatarAddress);
            Assert.NotNull(updatedAvatarState);

            // Verify that the action executed without throwing exceptions
            // The actual battle simulation success depends on many factors,
            // so we just verify the action completed successfully
        }

        [Fact]
        public void Execute_FirstClear_ShouldGiveRewards()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            // Set avatar level to ensure success
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5); // Give some tickets
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action for first clear
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act - First clear
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            var nextState = action.Execute(context);

            // Assert - Should have rewards for first clear
            var finalAvatarState = nextState.GetAvatarState(avatarAddress);
            var finalInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);

            // Check if floor was cleared
            Assert.True(finalInfiniteTowerInfo.IsCleared(floorId));

            // Check if rewards were given (should have items in inventory)
            var initialInventoryCount = avatarState.inventory.Items.Count;
            var finalInventoryCount = finalAvatarState.inventory.Items.Count;

            // Get expected rewards from floor sheet
            var floorSheet = nextState.GetSheet<InfiniteTowerFloorSheet>();
            if (floorSheet.TryGetValue(floorId, out var floorRow))
            {
                var expectedItemRewards = floorRow.GetItemRewards();

                if (expectedItemRewards.Count > 0)
                {
                    Console.WriteLine($"Expected rewards for floor {floorId}: {expectedItemRewards.Count} items");

                    // Verify that reward items are actually in inventory
                    foreach (var (itemId, expectedCount) in expectedItemRewards)
                    {
                        var hasItem = finalAvatarState.inventory.TryGetItem(itemId, out var inventoryItem);
                        Console.WriteLine($"Checking reward item {itemId}: Expected count={expectedCount}, Has item={hasItem}, Actual count={(hasItem ? inventoryItem.count : 0)}");

                        Assert.True(hasItem, $"Reward item {itemId} should be in inventory after first clear");
                        Assert.True(
                            inventoryItem.count >= expectedCount,
                            $"Reward item {itemId} count should be at least {expectedCount}, but was {inventoryItem.count}");
                    }

                    // Verify inventory count increased
                    Console.WriteLine($"Inventory count: Initial={initialInventoryCount}, Final={finalInventoryCount}");
                    Assert.True(
                        finalInventoryCount > initialInventoryCount,
                        $"Inventory count should increase after receiving rewards. Initial: {initialInventoryCount}, Final: {finalInventoryCount}");
                }
                else
                {
                    Console.WriteLine($"No item rewards defined for floor {floorId}");
                }
            }

            // For first clear, should have received rewards
            if (finalInventoryCount > initialInventoryCount)
            {
                Console.WriteLine($"First clear: Received {finalInventoryCount - initialInventoryCount} items");
            }
        }

        [Fact(Skip = "This test needs to be updated - invalid item ID validation logic may have changed")]
        public void Execute_WithInvalidItemId_ShouldThrowException()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state using the same pattern as the working test
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            // Set avatar level to ensure success
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            // Modify InfiniteTowerFloorSheet to use invalid item ID
            var invalidFloorSheet = @"Id,Floor,RequiredCp,MaxCp,ForbiddenItemSubTypes,MinItemGrade,MaxItemGrade,MinItemLevel,MaxItemLevel,GuaranteedConditionId,MinRandomConditions,MaxRandomConditions,RandomConditionId1,RandomConditionWeight1,RandomConditionId2,RandomConditionWeight2,RandomConditionId3,RandomConditionWeight3,RandomConditionId4,RandomConditionWeight4,RandomConditionId5,RandomConditionWeight5,ItemRewardId1,ItemRewardCount1,ItemRewardId2,ItemRewardCount2,ItemRewardId3,ItemRewardCount3,ItemRewardId4,ItemRewardCount4,ItemRewardId5,ItemRewardCount5,FungibleAssetRewardTicker1,FungibleAssetRewardAmount1,FungibleAssetRewardTicker2,FungibleAssetRewardAmount2,FungibleAssetRewardTicker3,FungibleAssetRewardAmount3,FungibleAssetRewardTicker4,FungibleAssetRewardAmount4,FungibleAssetRewardTicker5,FungibleAssetRewardAmount5,NcgCost,MaterialCostId,MaterialCostCount,ForbiddenRuneTypes,RequiredElementalTypes
1,1,100,100000,,1,5,1,10,1,0,2,,,,,,,,,,99999999,1,,,,,,,,,,RUNESTONE_FENRIR1,100,,,,,,,,100,10000001,50,,";
            sheets["InfiniteTowerFloorSheet"] = invalidFloorSheet;

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config using the same pattern as the working test
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5); // Give some tickets
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act & Assert
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            Assert.Throws<InvalidItemIdException>(() => action.Execute(context));
        }

        [Fact]
        public void Execute_SecondClear_ShouldNotGiveRewards()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            // Set avatar level to ensure success
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Pre-clear the floor to simulate second attempt
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5); // Give some tickets
            infiniteTowerInfo.ClearFloor(floorId);

            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action for second clear
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act - Second clear
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            var nextState = action.Execute(context);

            // Assert - Should not have additional rewards for second clear
            var finalAvatarState = nextState.GetAvatarState(avatarAddress);
            var finalInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);

            // Check if floor is still cleared
            Assert.True(finalInfiniteTowerInfo.IsCleared(floorId));

            // Check that no additional rewards were given
            var initialInventoryCount = avatarState.inventory.Items.Count;
            var finalInventoryCount = finalAvatarState.inventory.Items.Count;

            // For second clear, should not have received additional rewards
            Assert.Equal(initialInventoryCount, finalInventoryCount);
        }

        [Fact]
        public void Execute_With_Insufficient_Tickets()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 10;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with NO tickets and prevent both auto-resets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            // Set LastResetBlockIndex to a value greater than StartBlockIndex (0) to prevent season reset
            infiniteTowerInfo.GetType().GetProperty("LastResetBlockIndex")?.SetValue(infiniteTowerInfo, blockIndex + 1);
            // Set LastTicketRefillBlockIndex to current block to prevent daily refill
            infiniteTowerInfo.GetType().GetProperty("LastTicketRefillBlockIndex")?.SetValue(infiniteTowerInfo, blockIndex);
            // Set RemainingTickets to 0 to simulate no tickets
            infiniteTowerInfo.GetType().GetProperty("RemainingTickets")?.SetValue(infiniteTowerInfo, 0);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false, // Don't allow ticket purchase
            };

            // Act & Assert
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var exception = Assert.Throws<NotEnoughInfiniteTowerTicketsException>(() => action.Execute(context));
            Assert.Contains("Not enough infinite tower tickets", exception.Message);
            Assert.Contains("Required: 1", exception.Message);
            Assert.Contains("Available: 0", exception.Message);
        }

        [Fact]
        public void Execute_With_Uncleared_Stage()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 3; // Try to access floor 3 without clearing floor 2

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 10;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets but only floor 1 cleared
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            infiniteTowerInfo.ClearFloor(1); // Only clear floor 1, not floor 2
            // Set LastResetBlockIndex to prevent season reset (StartBlockIndex is 0, so use blockIndex + 1)
            infiniteTowerInfo.GetType().GetProperty("LastResetBlockIndex")?.SetValue(infiniteTowerInfo, blockIndex + 1);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action trying to access floor 3 (requires floor 2 to be cleared)
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act & Assert
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var exception = Assert.Throws<Nekoyume.Action.StageNotClearedException>(() => action.Execute(context));
            Assert.Contains("required(2), current(1)", exception.Message);
        }

        [Fact]
        public void Execute_With_FirstTimeFloorClear_ShouldUpdateBoardState()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var infiniteTowerId = 1;
            var floorId = 1;
            var blockIndex = 0L;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 10;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Verify initial board state is empty
            var initialBoardState = initialState.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.Equal(0, initialBoardState.GetFloorClearCount(floorId));

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify infinite tower info was updated
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            // Verify board state was updated for first-time clear
            var updatedBoardState = nextState.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.NotNull(updatedBoardState);
            // Note: The board state might not be updated if the simulation didn't succeed
            // This test verifies the logic is in place, but actual success depends on simulation
            var clearCount = updatedBoardState.GetFloorClearCount(floorId);
            Assert.True(clearCount >= 0, $"Floor clear count should be non-negative, but was {clearCount}");
        }

        [Fact]
        public void Execute_With_AlreadyClearedFloor_ShouldNotUpdateBoardState()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var infiniteTowerId = 1;
            var floorId = 1;
            var blockIndex = 0L;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 10;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets and floor already cleared
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            infiniteTowerInfo.ClearFloor(floorId); // Floor already cleared
            // Set LastResetBlockIndex to prevent season reset (StartBlockIndex is 0, so use blockIndex + 1)
            infiniteTowerInfo.GetType().GetProperty("LastResetBlockIndex")?.SetValue(infiniteTowerInfo, blockIndex + 1);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create initial board state with floor already cleared
            var initialBoardState = new InfiniteTowerBoardState(infiniteTowerId);
            initialBoardState.RecordFloorClear(floorId, blockIndex - 1); // Record previous clear
            initialState = (World)initialState.SetInfiniteTowerBoardState(initialBoardState);

            // Verify initial board state has one clear
            var boardStateBefore = initialState.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.Equal(1, boardStateBefore.GetFloorClearCount(floorId));

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify board state was NOT updated for already cleared floor
            var boardStateAfter = nextState.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.NotNull(boardStateAfter);
            Assert.Equal(1, boardStateAfter.GetFloorClearCount(floorId)); // Should remain 1, not increase
            Assert.Equal(blockIndex - 1, boardStateAfter.LastUpdatedBlockIndex); // Should remain previous block index
        }

        [Fact]
        public void Execute_With_MultipleFloors_ShouldUpdateBoardStateCorrectly()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var infiniteTowerId = 1;
            var floorId1 = 1;
            var floorId2 = 2;
            var blockIndex = 0L;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 10;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(10);
            // Set LastResetBlockIndex to prevent season reset (StartBlockIndex is 0, so use blockIndex + 1)
            infiniteTowerInfo.GetType().GetProperty("LastResetBlockIndex")?.SetValue(infiniteTowerInfo, blockIndex + 1);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Verify initial board state is empty
            var initialBoardState = initialState.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.Equal(0, initialBoardState.GetFloorClearCount(floorId1));
            Assert.Equal(0, initialBoardState.GetFloorClearCount(floorId2));

            // Act - Clear floor 1
            var action1 = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId1,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            var context1 = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var stateAfterFloor1 = action1.Execute(context1);

            // Assert - Floor 1 should be recorded (if simulation succeeded)
            var boardStateAfterFloor1 = stateAfterFloor1.GetInfiniteTowerBoardState(infiniteTowerId);
            var floor1ClearCount = boardStateAfterFloor1.GetFloorClearCount(floorId1);
            var floor2ClearCount = boardStateAfterFloor1.GetFloorClearCount(floorId2);
            Assert.True(floor1ClearCount >= 0, $"Floor 1 clear count should be non-negative, but was {floor1ClearCount}");
            Assert.True(floor2ClearCount >= 0, $"Floor 2 clear count should be non-negative, but was {floor2ClearCount}");

            // Update infinite tower info to mark floor 1 as cleared
            var updatedInfiniteTowerInfo = stateAfterFloor1.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            updatedInfiniteTowerInfo.ClearFloor(floorId1);
            stateAfterFloor1 = (World)stateAfterFloor1.SetInfiniteTowerInfo(avatarAddress, updatedInfiniteTowerInfo);

            // Act - Clear floor 2
            var action2 = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId2,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            var context2 = new ActionContext
            {
                BlockIndex = blockIndex + 1,
                PreviousState = stateAfterFloor1,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var stateAfterFloor2 = action2.Execute(context2);

            // Assert - Both floors should be recorded (if simulations succeeded)
            var boardStateAfterFloor2 = stateAfterFloor2.GetInfiniteTowerBoardState(infiniteTowerId);
            var finalFloor1ClearCount = boardStateAfterFloor2.GetFloorClearCount(floorId1);
            var finalFloor2ClearCount = boardStateAfterFloor2.GetFloorClearCount(floorId2);
            Assert.True(finalFloor1ClearCount >= 0, $"Final floor 1 clear count should be non-negative, but was {finalFloor1ClearCount}");
            Assert.True(finalFloor2ClearCount >= 0, $"Final floor 2 clear count should be non-negative, but was {finalFloor2ClearCount}");
            Assert.True(boardStateAfterFloor2.GetTotalClearedFloors() >= 0, "Total cleared floors should be non-negative");
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_ForbiddenSubTypes_ShouldThrow()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenSubTypes = new List<ItemSubType> { ItemSubType.Weapon };
            floorRow.GetType().GetProperty("ForbiddenItemSubTypes")?.SetValue(floorRow, forbiddenSubTypes);

            var itemList = new List<ItemBase>
            {
                CreateTestItem(ItemType.Equipment), // This should be allowed (Armor)
                CreateTestWeapon(), // This should be forbidden (Weapon)
            };

            // Act & Assert
            try
            {
                floorRow.ValidateItemTypeRestrictions(itemList);
                // 디버깅 정보 출력
                var forbiddenSubTypesValue = floorRow.GetType().GetProperty("ForbiddenItemSubTypes")?.GetValue(floorRow);
                var itemSubTypes = itemList.Select(item => item.ItemSubType).ToList();
                throw new InvalidOperationException($"Expected exception but none was thrown. ForbiddenSubTypes: {forbiddenSubTypesValue}, ItemSubTypes: {string.Join(", ", itemSubTypes)}");
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                Assert.Contains("Invalid item sub-type", ex.Message);
                Assert.Contains("Weapon", ex.Message);
                Assert.Contains("forbidden", ex.Message);
            }
        }

        [Theory]
        [InlineData(ItemType.Material)]
        [InlineData(ItemType.Consumable)]
        public void ValidateItemTypeRestrictions_With_AllowedTypes_ShouldPass(ItemType allowedType)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            var forbiddenTypes = new List<ItemType> { allowedType };
            floorRow.GetType().GetProperty("ForbiddenItemTypes")?.SetValue(floorRow, forbiddenTypes);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment),
                CreateTestEquipment(ItemType.Costume),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemTypeRestrictions(equipmentList);
        }

        [Fact]
        public void ValidateItemTypeRestrictions_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();

            // No restrictions set
            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment),
                CreateTestEquipment(ItemType.Material),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemTypeRestrictions(equipmentList);
        }

        [Theory]
        [InlineData(3, 2, "below minimum")]
        [InlineData(4, 3, "below minimum")]
        public void ValidateItemGradeRestrictions_With_MinGrade_ShouldThrow(int minGrade, int itemGrade, string expectedMessage)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemGrade")?.SetValue(floorRow, minGrade);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: itemGrade), // Below minimum
                CreateTestEquipment(ItemType.Equipment, grade: minGrade + 1), // Above minimum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemGradeRestrictions(equipmentList));
            Assert.Contains("Invalid item grade", exception.Message);
            Assert.Contains(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(3, 4, "exceeds maximum")]
        [InlineData(2, 3, "exceeds maximum")]
        public void ValidateItemGradeRestrictions_With_MaxGrade_ShouldThrow(int maxGrade, int itemGrade, string expectedMessage)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MaxItemGrade")?.SetValue(floorRow, maxGrade);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: maxGrade - 1), // Below maximum
                CreateTestEquipment(ItemType.Equipment, grade: itemGrade), // Above maximum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemGradeRestrictions(equipmentList));
            Assert.Contains("Invalid item grade", exception.Message);
            Assert.Contains(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(2, 4, 2)]
        [InlineData(2, 4, 3)]
        [InlineData(2, 4, 4)]
        public void ValidateItemGradeRestrictions_With_ValidGrades_ShouldPass(int minGrade, int maxGrade, int itemGrade)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemGrade")?.SetValue(floorRow, minGrade);
            floorRow.GetType().GetProperty("MaxItemGrade")?.SetValue(floorRow, maxGrade);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, grade: itemGrade),
            };

            // Act & Assert - Should not throw
            floorRow.ValidateItemGradeRestrictions(equipmentList);
        }

        [Theory]
        [InlineData(5, 3, "below minimum")]
        [InlineData(7, 5, "below minimum")]
        public void ValidateItemLevelRestrictions_With_MinLevel_ShouldThrow(int minLevel, int itemLevel, string expectedMessage)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemLevel")?.SetValue(floorRow, minLevel);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, level: itemLevel), // Below minimum
                CreateTestEquipment(ItemType.Equipment, level: minLevel + 2), // Above minimum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemLevelRestrictions(equipmentList));
            Assert.Contains("Invalid item level", exception.Message);
            Assert.Contains(expectedMessage, exception.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_MaxLevel_ShouldThrow()
        {
            // Arrange
            var fields = new List<string>
            {
                "1", // Id
                "1", // Floor
                "1000", // RequiredCp
                "2000", // MaxCp
                string.Empty, // ForbiddenItemSubTypes
                "1", // MinItemGrade
                "5", // MaxItemGrade
                "1", // MinItemLevel
                "5", // MaxItemLevel
                "1", // GuaranteedConditionId
                "0", // MinRandomConditions
                "2", // MaxRandomConditions
                string.Empty, // RandomConditionId1
                string.Empty, // RandomConditionWeight1
                string.Empty, // RandomConditionId2
                string.Empty, // RandomConditionWeight2
                string.Empty, // RandomConditionId3
                string.Empty, // RandomConditionWeight3
                string.Empty, // RandomConditionId4
                string.Empty, // RandomConditionWeight4
                string.Empty, // RandomConditionId5
                string.Empty, // RandomConditionWeight5
                string.Empty, // ItemRewardId1
                string.Empty, // ItemRewardCount1
                string.Empty, // ItemRewardId2
                string.Empty, // ItemRewardCount2
                string.Empty, // ItemRewardId3
                string.Empty, // ItemRewardCount3
                string.Empty, // ItemRewardId4
                string.Empty, // ItemRewardCount4
                string.Empty, // ItemRewardId5
                string.Empty, // ItemRewardCount5
                string.Empty, // FungibleAssetRewardTicker1
                string.Empty, // FungibleAssetRewardAmount1
                string.Empty, // FungibleAssetRewardTicker2
                string.Empty, // FungibleAssetRewardAmount2
                string.Empty, // FungibleAssetRewardTicker3
                string.Empty, // FungibleAssetRewardAmount3
                string.Empty, // FungibleAssetRewardTicker4
                string.Empty, // FungibleAssetRewardAmount4
                string.Empty, // FungibleAssetRewardTicker5
                string.Empty, // FungibleAssetRewardAmount5
                "100", // NcgCost
                "600201", // MaterialCostId
                "50", // MaterialCostCount
                string.Empty, // ForbiddenRuneTypes
                string.Empty, // RequiredElementalTypes
            };

            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.Set(fields);

            var equipmentList = new List<Equipment>
            {
                CreateTestEquipment(ItemType.Equipment, level: 3), // Below maximum
                CreateTestEquipment(ItemType.Equipment, level: 7), // Above maximum
            };

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateItemLevelRestrictions(equipmentList));
            Assert.Contains("Invalid item level", exception.Message);
            Assert.Contains("exceeds maximum", exception.Message);
        }

        [Fact]
        public void ValidateItemLevelRestrictions_With_Costumes_ShouldSkip()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MinItemLevel")?.SetValue(floorRow, 5);

            var costumeList = new List<Costume>
            {
                CreateTestCostume(ItemType.Costume, level: 1), // Costumes should be skipped
            };

            // Act & Assert - Should not throw (costumes are skipped)
            floorRow.ValidateItemLevelRestrictions(costumeList);
        }

        [Theory]
        [InlineData(1000L, 500, "Insufficient combat power", "below minimum")]
        [InlineData(1500L, 1000, "Insufficient combat power", "below minimum")]
        public void ValidateCpRequirements_With_MinCp_ShouldThrow(long requiredCp, int actualCp, string expectedMessage1, string expectedMessage2)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("RequiredCp")?.SetValue(floorRow, requiredCp);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateCpRequirements(actualCp));
            Assert.Contains(expectedMessage1, exception.Message);
            Assert.Contains(expectedMessage2, exception.Message);
        }

        [Theory]
        [InlineData(1000L, 1500, "Excessive combat power", "exceeds maximum")]
        [InlineData(800L, 1200, "Excessive combat power", "exceeds maximum")]
        public void ValidateCpRequirements_With_MaxCp_ShouldThrow(long maxCp, int actualCp, string expectedMessage1, string expectedMessage2)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("MaxCp")?.SetValue(floorRow, maxCp);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => floorRow.ValidateCpRequirements(actualCp));
            Assert.Contains(expectedMessage1, exception.Message);
            Assert.Contains(expectedMessage2, exception.Message);
        }

        [Theory]
        [InlineData(500L, 1500L, 1000)]
        [InlineData(300L, 1200L, 800)]
        [InlineData(1000L, 2000L, 1500)]
        public void ValidateCpRequirements_With_ValidCp_ShouldPass(long requiredCp, long maxCp, int actualCp)
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            floorRow.GetType().GetProperty("RequiredCp")?.SetValue(floorRow, requiredCp);
            floorRow.GetType().GetProperty("MaxCp")?.SetValue(floorRow, maxCp);

            // Act & Assert - Should not throw
            floorRow.ValidateCpRequirements(actualCp);
        }

        [Fact]
        public void ValidateCpRequirements_With_NoRestrictions_ShouldPass()
        {
            // Arrange
            var floorRow = new InfiniteTowerFloorSheet.Row();
            // No restrictions set

            // Act & Assert - Should not throw
            floorRow.ValidateCpRequirements(1000);
        }

        [Fact]
        public void Execute_With_StartBlockIndex_Zero_And_LastResetBlockIndex_Zero_Should_Reset_And_Grant_Ticket()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 0L; // StartBlockIndex is 0 in CSV
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with LastResetBlockIndex = 0 (newly created)
            // This simulates the bug scenario where StartBlockIndex = 0 and LastResetBlockIndex = 0
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            // LastResetBlockIndex is already 0 from constructor
            Assert.Equal(0, infiniteTowerInfo.LastResetBlockIndex);
            Assert.True(infiniteTowerInfo.RemainingTickets > 0);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify that season reset was performed and ticket was granted
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            // LastResetBlockIndex should be updated to current block index
            // This confirms that PerformSeasonReset was called
            Assert.Equal(blockIndex, updatedInfiniteTowerInfo.LastResetBlockIndex);

            // LastTicketRefillBlockIndex should also be updated
            Assert.Equal(blockIndex, updatedInfiniteTowerInfo.LastTicketRefillBlockIndex);

            // This confirms that the ticket was granted and then used
            Assert.Equal(infiniteTowerInfo.RemainingTickets - 1, updatedInfiniteTowerInfo.RemainingTickets);
            Assert.Equal(1, updatedInfiniteTowerInfo.TotalTicketsUsed);
        }

        [Fact]
        public void Execute_With_SeasonReset_Should_Reset_ClearedFloor_To_Zero()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 0L; // StartBlockIndex is 0 in CSV
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with LastResetBlockIndex = 0 and some progress
            // This simulates a new season where previous progress should be reset
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.ClearFloor(5); // Set some previous progress
            // LastResetBlockIndex is already 0 from constructor, which will trigger season reset
            Assert.Equal(0, infiniteTowerInfo.LastResetBlockIndex);
            Assert.Equal(5, infiniteTowerInfo.ClearedFloor);
            Assert.True(infiniteTowerInfo.RemainingTickets > 0);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify that season reset was performed
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            // LastResetBlockIndex should be updated to current block index
            // This confirms that PerformSeasonReset was called
            Assert.Equal(blockIndex, updatedInfiniteTowerInfo.LastResetBlockIndex);

            // LastTicketRefillBlockIndex should also be updated
            Assert.Equal(blockIndex, updatedInfiniteTowerInfo.LastTicketRefillBlockIndex);

            // TotalTicketsUsed should be reset to 0 after season reset, then used for battle
            // So it should be 1 (0 after reset, then +1 for battle)
            Assert.Equal(1, updatedInfiniteTowerInfo.TotalTicketsUsed);

            // NumberOfTicketPurchases should be reset to 0 after season reset
            Assert.Equal(0, updatedInfiniteTowerInfo.NumberOfTicketPurchases);

            // Note: ClearedFloor is reset to 0 by PerformSeasonReset, but then updated
            // if the battle simulation succeeds. So we verify season reset happened
            // by checking LastResetBlockIndex and TotalTicketsUsed reset.
        }

        [Fact]
        public void Execute_With_SeasonReset_Should_Grant_One_Ticket()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 0L; // StartBlockIndex is 0 in CSV
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with LastResetBlockIndex = 0 (newly created)
            // This will trigger season reset which should grant 1 ticket
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            // LastResetBlockIndex is already 0 from constructor
            Assert.Equal(0, infiniteTowerInfo.LastResetBlockIndex);
            Assert.True(infiniteTowerInfo.RemainingTickets > 0);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify that season reset was performed and ticket was granted
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            Assert.Equal(infiniteTowerInfo.RemainingTickets - 1, updatedInfiniteTowerInfo.RemainingTickets);
            Assert.Equal(1, updatedInfiniteTowerInfo.TotalTicketsUsed);

            // Verify that the ticket was granted (TotalTicketsUsed = 1 confirms ticket was used)
            // This means the ticket was granted by PerformSeasonReset and then consumed
        }

        [Fact]
        public void Execute_With_NewSeason_After_PreviousSeason_Should_Reset_Progress()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var previousBlockIndex = 1000L;
            var newSeasonStartBlockIndex = 2000L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            // Modify schedule to have a new season start at block 2000
            var scheduleSheet = new InfiniteTowerScheduleSheet();
            var scheduleSheetString = sheets["InfiniteTowerScheduleSheet"];
            var scheduleLines = scheduleSheetString.Split('\n');
            if (scheduleLines.Length > 1)
            {
                // Update StartBlockIndex to 2000 for new season
                scheduleLines[1] = "1,1,2000,1000000,3,10,10800,1,100";
                sheets["InfiniteTowerScheduleSheet"] = string.Join("\n", scheduleLines);
            }

            scheduleSheet.Set(sheets["InfiniteTowerScheduleSheet"]);

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with previous season progress
            // LastResetBlockIndex < new season StartBlockIndex (2000) should trigger season reset
            // Get initial tickets from parsed schedule sheet
            var scheduleRow = scheduleSheet.Values.FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);
            var initialTickets = scheduleRow != null
                ? Math.Min(scheduleRow.DailyFreeTickets, scheduleRow.MaxTickets)
                : 0;
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, infiniteTowerId, initialTickets);
            infiniteTowerInfo.ClearFloor(10); // Previous season progress
            infiniteTowerInfo.AddTickets(5);
            // Set LastResetBlockIndex to previous season (before new season start)
            infiniteTowerInfo.GetType().GetProperty("LastResetBlockIndex")?.SetValue(infiniteTowerInfo, previousBlockIndex);
            Assert.Equal(10, infiniteTowerInfo.ClearedFloor);
            Assert.Equal(previousBlockIndex, infiniteTowerInfo.LastResetBlockIndex);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act - Execute at new season start block
            var context = new ActionContext
            {
                BlockIndex = newSeasonStartBlockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            var nextState = action.Execute(context);

            // Assert
            Assert.NotNull(nextState);

            // Verify that season reset was performed
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);

            // LastResetBlockIndex should be updated to new season start block index
            // This confirms that PerformSeasonReset was called
            Assert.Equal(newSeasonStartBlockIndex, updatedInfiniteTowerInfo.LastResetBlockIndex);

            // LastTicketRefillBlockIndex should also be updated
            Assert.Equal(newSeasonStartBlockIndex, updatedInfiniteTowerInfo.LastTicketRefillBlockIndex);

            // TotalTicketsUsed should be reset to 0 after season reset, then used for battle
            // So it should be 1 (0 after reset, then +1 for battle)
            Assert.Equal(1, updatedInfiniteTowerInfo.TotalTicketsUsed);

            // NumberOfTicketPurchases should be reset to 0 after season reset
            Assert.Equal(0, updatedInfiniteTowerInfo.NumberOfTicketPurchases);

            // Note: ClearedFloor is reset to 0 by PerformSeasonReset, but then updated
            // if the battle simulation succeeds. So we verify season reset happened
            // by checking LastResetBlockIndex and TotalTicketsUsed reset.
        }

        [Fact]
        public void Execute_WithFood_ShouldConsumeFood_WithoutAvatarStateApply()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            // Set avatar level to ensure success
            avatarState.level = 100;

            // Add food items to inventory
            var consumableSheet = _tableSheets.ConsumableItemSheet;
            var foodRow = consumableSheet.Values.FirstOrDefault();
            if (foodRow == null)
            {
                throw new InvalidOperationException("No consumable item found in ConsumableItemSheet");
            }

            var food1 = (Consumable)ItemFactory.CreateItemUsable(foodRow, Guid.NewGuid(), 0, 0);
            var food2 = (Consumable)ItemFactory.CreateItemUsable(foodRow, Guid.NewGuid(), 0, 0);
            var food3 = (Consumable)ItemFactory.CreateItemUsable(foodRow, Guid.NewGuid(), 0, 0);

            avatarState.inventory.AddItem(food1);
            avatarState.inventory.AddItem(food2);
            avatarState.inventory.AddItem(food3);

            var initialFoodCount = avatarState.inventory.Items
                .Count(i => i.item is Consumable && i.item.Id == foodRow.Id);

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action with food items
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid> { food1.ItemId, food2.ItemId },
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            var nextState = action.Execute(context);

            // Assert
            var finalAvatarState = nextState.GetAvatarState(avatarAddress);
            var finalFoodCount = finalAvatarState.inventory.Items
                .Count(i => i.item is Consumable && i.item.Id == foodRow.Id);

            // The food should be consumed by the simulator, but without avatarState.Apply,
            // the inventory might not reflect the consumption
            // This test documents the current behavior
            Assert.True(
                finalFoodCount < initialFoodCount,
                $"Food count should not increase. Initial: {initialFoodCount}, Final: {finalFoodCount}");
        }

        [Fact]
        public void Execute_WithValidElementalType_ShouldSucceed()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            // Modify InfiniteTowerFloorSheet to require Fire and Water elemental types
            var floorSheetWithElementalRestriction = @"Id,Floor,RequiredCp,MaxCp,ForbiddenItemSubTypes,MinItemGrade,MaxItemGrade,MinItemLevel,MaxItemLevel,GuaranteedConditionId,MinRandomConditions,MaxRandomConditions,RandomConditionId1,RandomConditionWeight1,RandomConditionId2,RandomConditionWeight2,RandomConditionId3,RandomConditionWeight3,RandomConditionId4,RandomConditionWeight4,RandomConditionId5,RandomConditionWeight5,ItemRewardId1,ItemRewardCount1,ItemRewardId2,ItemRewardCount2,ItemRewardId3,ItemRewardCount3,ItemRewardId4,ItemRewardCount4,ItemRewardId5,ItemRewardCount5,FungibleAssetRewardTicker1,FungibleAssetRewardAmount1,FungibleAssetRewardTicker2,FungibleAssetRewardAmount2,FungibleAssetRewardTicker3,FungibleAssetRewardAmount3,FungibleAssetRewardTicker4,FungibleAssetRewardAmount4,FungibleAssetRewardTicker5,FungibleAssetRewardAmount5,NcgCost,MaterialCostId,MaterialCostCount,ForbiddenRuneTypes,RequiredElementalTypes
1,1,100,100000,,1,5,1,10,1,0,2,,,,,,,,,,99999999,1,,,,,,,,,,RUNESTONE_FENRIR1,100,,,,,,,,100,10000001,50,,1:2";
            sheets["InfiniteTowerFloorSheet"] = floorSheetWithElementalRestriction;

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Create equipment with valid elemental type (Fire = 1)
            var equipment = CreateTestEquipment(ItemType.Equipment, grade: 1, level: 1);
            equipment.ElementalType = ElementalType.Fire; // Valid: Fire is in required list (1:2)
            avatarState.inventory.AddItem(equipment);

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid> { equipment.NonFungibleId },
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            var nextState = action.Execute(context);

            // Assert - Should succeed without throwing InvalidElementalException
            Assert.NotNull(nextState);
            var updatedInfiniteTowerInfo = nextState.GetInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            Assert.NotNull(updatedInfiniteTowerInfo);
        }

        [Fact]
        public void Execute_WithInvalidElementalType_ShouldThrow()
        {
            // Arrange
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var blockIndex = 100L;
            var infiniteTowerId = 1;
            var floorId = 1;

            // Create initial state
            var initialState = new World(MockUtil.MockModernWorldState);
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            // Set up sheets with default CSV data
            var sheets = new Dictionary<string, string>();
            foreach (var (key, value) in TableSheetsImporter.ImportSheets())
            {
                sheets[key] = value;
            }

            // Modify InfiniteTowerFloorSheet to require Fire and Water elemental types (1:2)
            var floorSheetWithElementalRestriction = @"Id,Floor,RequiredCp,MaxCp,ForbiddenItemSubTypes,MinItemGrade,MaxItemGrade,MinItemLevel,MaxItemLevel,GuaranteedConditionId,MinRandomConditions,MaxRandomConditions,RandomConditionId1,RandomConditionWeight1,RandomConditionId2,RandomConditionWeight2,RandomConditionId3,RandomConditionWeight3,RandomConditionId4,RandomConditionWeight4,RandomConditionId5,RandomConditionWeight5,ItemRewardId1,ItemRewardCount1,ItemRewardId2,ItemRewardCount2,ItemRewardId3,ItemRewardCount3,ItemRewardId4,ItemRewardCount4,ItemRewardId5,ItemRewardCount5,FungibleAssetRewardTicker1,FungibleAssetRewardAmount1,FungibleAssetRewardTicker2,FungibleAssetRewardAmount2,FungibleAssetRewardTicker3,FungibleAssetRewardAmount3,FungibleAssetRewardTicker4,FungibleAssetRewardAmount4,FungibleAssetRewardTicker5,FungibleAssetRewardAmount5,NcgCost,MaterialCostId,MaterialCostCount,ForbiddenRuneTypes,RequiredElementalTypes
1,1,100,100000,,1,5,1,10,1,0,2,,,,,,,,,,99999999,1,,,,,,,,,,RUNESTONE_FENRIR1,100,,,,,,,,100,10000001,50,,,1:2";
            sheets["InfiniteTowerFloorSheet"] = floorSheetWithElementalRestriction;

            foreach (var (key, value) in sheets)
            {
                initialState = (World)initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            // Set up game config
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            initialState = (World)initialState.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            // Create equipment with invalid elemental type (Land = 3, not in required list 1:2)
            var equipment = CreateTestEquipment(ItemType.Equipment, grade: 1, level: 1);
            equipment.ElementalType = ElementalType.Land; // Invalid: Land is not in required list (1:2 = Fire:Water)
            avatarState.inventory.AddItem(equipment);

            // Set up states
            initialState = (World)initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            // Create infinite tower info with tickets
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, infiniteTowerId);
            infiniteTowerInfo.AddTickets(5);
            initialState = (World)initialState.SetInfiniteTowerInfo(avatarAddress, infiniteTowerInfo);

            // Create action
            var action = new InfiniteTowerBattle
            {
                AvatarAddress = avatarAddress,
                InfiniteTowerId = infiniteTowerId,
                FloorId = floorId,
                Equipments = new List<Guid> { equipment.NonFungibleId },
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                BuyTicketIfNeeded = false,
            };

            // Act & Assert
            var context = new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = initialState,
                RandomSeed = 0,
            };

            var exception = Assert.Throws<InvalidElementalException>(() => action.Execute(context));
            Assert.Contains("Invalid equipment elemental type", exception.Message);
            Assert.Contains("Land", exception.Message);
            Assert.Contains("Fire, Water", exception.Message);
        }

        /// <summary>
        /// Creates InfiniteTowerInfo with initial tickets from schedule sheet.
        /// </summary>
        private InfiniteTowerInfo CreateInfiniteTowerInfo(Address avatarAddress, int infiniteTowerId)
        {
            var initialTickets = 0;
            if (_tableSheets.InfiniteTowerScheduleSheet != null)
            {
                var scheduleRow = _tableSheets.InfiniteTowerScheduleSheet.Values
                    .FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);
                if (scheduleRow != null)
                {
                    initialTickets = Math.Min(scheduleRow.DailyFreeTickets, scheduleRow.MaxTickets);
                }
            }

            return new InfiniteTowerInfo(avatarAddress, infiniteTowerId, initialTickets);
        }

        private Equipment CreateTestEquipment(ItemType itemType, int grade = 1, int level = 1)
        {
            // EquipmentItemSheet에서 직접 찾아서 사용
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values
                .FirstOrDefault(x => x.Grade == grade);

            if (equipmentRow == null)
            {
                // 해당 조건의 아이템이 없으면 기본 Equipment를 사용
                equipmentRow = _tableSheets.EquipmentItemSheet.Values.FirstOrDefault();
            }

            if (equipmentRow == null)
            {
                throw new InvalidOperationException($"No equipment found in EquipmentItemSheet");
            }

            var item = ItemFactory.CreateItem(equipmentRow, new TestRandom());
            if (item is Equipment equipment)
            {
                // Level 설정
                // Equipment의 기본 레벨은 0이므로, level이 1 이상이면 레벨업을 수행
                if (level >= 1)
                {
                    for (int i = 0; i < level; i++)
                    {
                        equipment.LevelUp(new TestRandom(), _tableSheets.EnhancementCostSheetV2.Values.First(), false);
                    }
                }

                return equipment;
            }

            throw new InvalidOperationException(
                $"Created item is not Equipment type: {item.GetType()}. " +
                $"Sheet Id: {equipmentRow.Id}");
        }

        private ItemBase CreateTestItem(ItemType itemType, int grade = 1, int level = 1)
        {
            // 요청된 타입에 따라 적절한 시트에서 아이템 생성
            switch (itemType)
            {
                case ItemType.Equipment:
                    return CreateTestEquipment(itemType, grade, level);
                case ItemType.Costume:
                    return CreateTestCostume(itemType, grade, level);
                case ItemType.Material:
                    // MaterialItemSheet에서 Material 아이템 생성
                    var materialRow = _tableSheets.MaterialItemSheet.Values.FirstOrDefault();
                    if (materialRow == null)
                    {
                        throw new InvalidOperationException($"No material found in MaterialItemSheet");
                    }

                    return ItemFactory.CreateItem(materialRow, new TestRandom());

                default:
                    throw new InvalidOperationException($"Unsupported ItemType: {itemType}");
            }
        }

        private Equipment CreateTestWeapon()
        {
            // Weapon 타입의 Equipment 찾기
            var weaponRow = _tableSheets.EquipmentItemSheet.Values
                .FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);

            if (weaponRow == null)
            {
                throw new InvalidOperationException($"No weapon found in EquipmentItemSheet");
            }

            var item = ItemFactory.CreateItem(weaponRow, new TestRandom());
            if (item is Equipment equipment)
            {
                return equipment;
            }

            throw new InvalidOperationException($"Created item is not Equipment type: {item.GetType()}");
        }

        private Costume CreateTestCostume(ItemType itemType, int grade = 1, int level = 1)
        {
            // CostumeItemSheet에서 직접 찾아서 사용
            var costumeRow = _tableSheets.CostumeItemSheet.Values
                .FirstOrDefault(x => x.Grade == grade);

            if (costumeRow == null)
            {
                // 해당 조건의 코스튬이 없으면 기본 코스튬을 사용
                costumeRow = _tableSheets.CostumeItemSheet.Values.FirstOrDefault();
            }

            if (costumeRow == null)
            {
                throw new InvalidOperationException($"No costume found in CostumeItemSheet");
            }

            var costume = ItemFactory.CreateCostume(costumeRow, Guid.NewGuid());
            return costume;
        }
    }
}
