namespace Lib9c.Tests.Action.AdventureBoss
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class ClaimAdventureBossRewardTest
    {
        private const int InitialBalance = 1_000_000;

        private static readonly Dictionary<string, string> Sheets =
            TableSheetsImporter.ImportSheets();

        private static readonly TableSheets TableSheets = new TableSheets(Sheets);
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

        // Explorer
        private static readonly Address ExplorerAddress = new PrivateKey().Address;

        private static readonly Address ExplorerAvatarAddress =
            Addresses.GetAvatarAddress(ExplorerAddress, 0);

        private static readonly AvatarState ExplorerAvatarState = new (
            ExplorerAvatarAddress, ExplorerAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "explorer"
        );

        private static readonly AgentState ExplorerState = new (ExplorerAddress)
        {
            avatarAddresses =
            {
                [0] = ExplorerAvatarAddress,
            },
        };

        // Test Account
        private static readonly Address TesterAddress = new PrivateKey().Address;

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
            .SetAvatarState(WantedAvatarAddress, WantedAvatarState)
            .SetAgentState(WantedAddress, WantedState)
            .SetAvatarState(ExplorerAvatarAddress, ExplorerAvatarState)
            .SetAgentState(ExplorerAddress, ExplorerState)
            .SetAvatarState(TesterAvatarAddress, TesterAvatarState)
            .SetAgentState(TesterAddress, TesterState)
            .MintAsset(new ActionContext(), WantedAddress, InitialBalance * NCG)
            .MintAsset(new ActionContext(), ExplorerAddress, InitialBalance * NCG)
            .MintAsset(new ActionContext(), TesterAddress, InitialBalance * NCG);

        private RuneSheet _runeSheet = new ();

        // Member Data
        public static IEnumerable<object[]> GetWantedTestData()
        {
            /*
             * seed, myBounty, anotherWanted, anotherBounty, expectedReward
             */

            yield return new object[]
            {
                0, 100, false, null,
                new ClaimableReward
                {
                    NcgReward = 5 * NCG, // 5% of 100 NCG
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 0 },
                        { 600203, 0 },
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 14 },
                        { 30001, 34 },
                    },
                },
            };

            yield return new object[]
            {
                1, 100, true, 100,
                new ClaimableReward
                {
                    NcgReward = 10 * NCG, // 5% of 200 NCG
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 168 }, // (200*1.2) * 0.7 / 0.5 * (120/240)
                        { 600202, 0 },
                        { 600203, 5 }, // (200*1.2) * 0.3 / 7.5 * (120/240)
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 0 },
                        { 30001, 0 },
                    },
                },
            };

            yield return new object[]
            {
                3, 100, true, 200,
                new ClaimableReward
                {
                    NcgReward = 15 * NCG, // 5% of 300 NCG
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 47 }, // (300*1.2) * 0.7 / 1.5 * (100/360)
                        { 600203, 0 },
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 0 },
                        { 30001, 12 }, // (300*1.2) * 0.3 / 2.5 * (100/360)
                    },
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetWantedTestData))]
        public void WantedReward(
            int seed,
            int bounty,
            bool anotherWanted,
            int anotherBounty,
            ClaimableReward expectedReward
        )
        {
            // Settings
            var state = _initialState;
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = Stake(state, TesterAddress);

            // Wanted
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Bounty = bounty * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = 0L,
                RandomSeed = seed,
            });

            if (anotherWanted)
            {
                state = Stake(state, WantedAddress);
                state = new Wanted
                {
                    Season = 1,
                    AvatarAddress = WantedAvatarAddress,
                    Bounty = anotherBounty * NCG,
                }.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = WantedAddress,
                    BlockIndex = 1L,
                    RandomSeed = seed,
                });
            }

            // Burn all remaining NCG to make test easier
            state = state.BurnAsset(
                new ActionContext(),
                TesterAddress,
                state.GetBalance(TesterAddress, NCG)
            );

            // Test
            var resultState = new ClaimAdventureBossReward
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().EndBlockIndex + 1,
                RandomSeed = seed,
            });

            Test(resultState, expectedReward);
        }

        [Fact]
        public void WantedMultipleSeason()
        {
            const int seed = 0;
            // Settings
            var expectedReward = new ClaimableReward
            {
                NcgReward = 10 * NCG,
                FavReward = new Dictionary<int, int>
                {
                    { 20001, 14 },
                    { 30001, 34 },
                },
                ItemReward = new Dictionary<int, int>
                {
                    { 600201, 72 },
                    { 600202, 0 },
                    { 600203, 11 },
                },
            };
            var state = _initialState;
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = Stake(state, TesterAddress);
            state = Stake(state, WantedAddress);

            // Wanted for season 1, 3
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed,
            });
            state = new Wanted
            {
                Season = 2,
                AvatarAddress = WantedAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed + 1,
            });
            state = new Wanted
            {
                Season = 3,
                AvatarAddress = TesterAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed + 2,
            });

            // Burn remaining NCG
            state = state.BurnAsset(
                new ActionContext(),
                TesterAddress,
                state.GetBalance(TesterAddress, NCG)
            );

            // Test
            var resultState = new ClaimAdventureBossReward
            {
                Season = 3,
                AvatarAddress = TesterAvatarAddress,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().EndBlockIndex,
                RandomSeed = seed + 3,
            });
            Test(resultState, expectedReward);
        }

        // [Theory]
        // [InlineData()]
        // public void ExploreReward()
        // {
        // }
        //
        // [Theory]
        // [InlineData()]
        // public void AllReward()
        // {
        // }
        //==================================
        // Wanted only
        // Explore only
        // Wanted + Explore
        // Prev. Wanted + current Wanted
        // Prev. Explore + current Explore
        // Prev. Wanted + current Explore
        // Prev. Explore + current Wanted
        // Prev. Wanted only
        // Prev. Explore only
        [Fact]
        public void Execute()
        {
        }

        [Fact]
        public void StressTest()
        {
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
            _runeSheet = state.GetSheet<RuneSheet>();
            return state;
        }

        private void Test(IWorld world, ClaimableReward expectedReward)
        {
            Assert.Equal(expectedReward.NcgReward, world.GetBalance(TesterAddress, NCG));
            foreach (var fav in expectedReward.FavReward)
            {
                var rune = _runeSheet.OrderedList.First(row => row.Id == fav.Key);
                Assert.Equal(
                    fav.Value * Currencies.GetRune(rune.Ticker),
                    world.GetBalance(TesterAvatarAddress, Currencies.GetRune(rune.Ticker))
                );
            }

            var inventory = world.GetInventory(TesterAvatarAddress);
            foreach (var item in expectedReward.ItemReward)
            {
                var itemState = inventory.Items.FirstOrDefault(i => i.item.Id == item.Key);
                if (item.Value == 0)
                {
                    Assert.Null(itemState);
                }
                else
                {
                    Assert.Equal(item.Value, itemState!.count);
                }
            }
        }
    }
}
