namespace Lib9c.Tests.Action.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.AdventureBoss;
    using Xunit;

    public class UnlockFloorTest
    {
        private static readonly Dictionary<string, string> Sheets =
            TableSheetsImporter.ImportSheets();

        private static readonly TableSheets TableSheets = new (Sheets);
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

        // Wanted
        private static readonly Address WantedAddress = new PrivateKey().Address;

        private static readonly Address WantedAvatarAddress =
            Addresses.GetAvatarAddress(WantedAddress, 0);

        private static readonly AvatarState WantedAvatarState = new (
            WantedAvatarAddress, WantedAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "wanted"
        );

        private static readonly AgentState WantedState = new (WantedAddress)
        {
            avatarAddresses = { [0] = WantedAvatarAddress, },
        };

        // Test Account
        private static readonly Address TesterAddress =
            new ("2000000000000000000000000000000000000002");

        private static readonly Address TesterAvatarAddress =
            Addresses.GetAvatarAddress(TesterAddress, 0);

        private static readonly AvatarState TesterAvatarState = new (
            TesterAvatarAddress, TesterAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "Tester"
        );

        private static readonly AgentState TesterState = new (TesterAddress)
        {
            avatarAddresses =
            {
                [0] = TesterAvatarAddress,
            },
        };

        private readonly IWorld _initialState = new World(MockUtil.MockModernWorldState)
            .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(NCG).Serialize())
            .SetLegacyState(
                GameConfigState.Address,
                new GameConfigState(Sheets["GameConfigSheet"]).Serialize()
            )
            .SetAvatarState(WantedAvatarAddress, WantedAvatarState)
            .SetAgentState(WantedAddress, WantedState)
            .SetAvatarState(TesterAvatarAddress, TesterAvatarState)
            .SetAgentState(TesterAddress, TesterState)
            .MintAsset(new ActionContext(), WantedAddress, 1_000_000 * NCG);

        [Theory]
        // Success
        [InlineData(false, false, 5, 10, null)]
        [InlineData(true, false, 5, 10, null)]
        // Max floor
        [InlineData(false, false, 20, 20, typeof(InvalidOperationException))]
        [InlineData(true, false, 20, 20, typeof(InvalidOperationException))]
        // Not enough resources
        [InlineData(false, true, 5, 5, typeof(NotEnoughMaterialException))]
        [InlineData(true, true, 5, 5, typeof(InsufficientBalanceException))]
        public void Execute(
            bool useNcg, bool notEnough, int startFloor, int expectedFloor, Type exc
        )
        {
            // Settings
            var state = _initialState;
            var gameConfigState = new GameConfigState(Sheets[nameof(GameConfigSheet)]);
            state = state.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var validatorKey = new PrivateKey();
            state = DelegationUtil.EnsureValidatorPromotionReady(state, validatorKey, 0L);
            state = DelegationUtil.MakeGuild(state, WantedAddress, validatorKey, 0L);

            state = Stake(state, WantedAddress);
            var materialSheet = state.GetSheet<MaterialItemSheet>();
            var goldenDust =
                ItemFactory.CreateMaterial(materialSheet.Values.First(row => row.Id == 600201));

            if (!notEnough)
            {
                var unlockRow = state.GetSheet<AdventureBossUnlockFloorCostSheet>().Values
                    .First(row => row.FloorId == 6);
                if (useNcg)
                {
                    state = state.MintAsset(
                        new ActionContext(),
                        TesterAddress,
                        unlockRow.NcgPrice * NCG
                    );
                }
                else
                {
                    var inventory = state.GetInventoryV2(TesterAvatarAddress);
                    inventory.AddItem(goldenDust, unlockRow.GoldenDustPrice);
                    state = state.SetInventory(TesterAvatarAddress, inventory);
                }
            }

            // Open Season
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = gameConfigState.AdventureBossMinBounty * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = 0L,
                RandomSeed = 1,
            });

            // Explore
            state = new ExploreAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = 1L,
            });

            // Make all floors cleared
            var explorer = state.GetExplorer(1, TesterAvatarAddress);
            explorer.MaxFloor = startFloor;
            explorer.Floor = explorer.MaxFloor;
            state = state.SetExplorer(1, explorer);

            // Unlock
            var action = new UnlockFloor
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                UseNcg = useNcg,
            };

            if (exc is not null)
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = TesterAddress,
                        BlockIndex = 2L,
                    })
                );
            }
            else
            {
                var resultState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = TesterAddress,
                    BlockIndex = 2L,
                });

                Assert.Equal(0 * NCG, resultState.GetBalance(TesterAddress, NCG));
                if (!useNcg)
                {
                    var inventory = resultState.GetInventoryV2(TesterAvatarAddress);
                    Assert.Null(inventory.Items.FirstOrDefault(i => i.item.Id == 600202));
                }

                Assert.Equal(
                    expectedFloor,
                    resultState.GetExplorer(1, TesterAvatarAddress).MaxFloor
                );
            }
        }

        private IWorld Stake(IWorld world, Address agentAddress)
        {
            var action = new Stake(new BigInteger(500_000));
            var state = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                BlockIndex = 0L,
            });
            return state;
        }
    }
}
