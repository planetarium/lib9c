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
    using Nekoyume.Data;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.Item;
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
        private static readonly Address ExplorerAddress =
            new ("2000000000000000000000000000000000000001");

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
                1, 100, false, null,
                new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 0 * NCG, // No Wanted Raffle
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 240 },
                        { 600202, 0 },
                        { 600203, 0 },
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
                1, 100, true, 100,
                new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 0 * NCG, // No Wanted Raffle
                    ItemReward = new Dictionary<int, int>
                    {
                        {
                            600201, 240
                        }, // (200*1.2) * 0.7 / 0.5 * (120/240) + (200*1.2) * 0.3 / 0.5 * (120/240)
                        { 600202, 0 },
                        { 600203, 0 },
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
                1, 100, true, 200,
                new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 0 * NCG, // No Wanted Raffle
                    ItemReward = new Dictionary<int, int>
                    {
                        {
                            600201, 200
                        }, // (300*1.2) * 0.7 / 0.5 * (100/360) + (300*1.2) * 0.2 / 0.5 * (100/360)
                        { 600202, 0 },
                        { 600203, 0 },
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 0 },
                        { 30001, 0 },
                    },
                },
            };
        }

        public static IEnumerable<object[]> GetExploreTestData()
        {
            yield return new object[]
            {
                1, 100, 0, new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = (5 + 15) * NCG, // 5NCG for raffle, 15NCG for 15% distribution
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 67 },
                        { 600203, 0 }, // 100 AP / 7.5 ratio * 100% contribution
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
                1, 100, 1, new AdventureBossGameData.ClaimableReward
                {
                    NcgReward =
                        // 7.5NCG for half of 15% distribution
                        FungibleAssetValue.FromRawValue(
                            NCG,
                            (BigInteger)(7.5 * 100)
                        ),
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 66 }, // total 200 AP / 1.5 ratio * 50% contribution
                        { 600203, 0 },
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
                1, 200, 1, new AdventureBossGameData.ClaimableReward
                {
                    // 15NCG for half of 15% distribution
                    NcgReward = 15 * NCG,
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 66 }, // Total 200 AP / 1.5 Ratio * 50% Contribution
                        { 600203, 0 },
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
                1, 100, 2, new AdventureBossGameData.ClaimableReward
                {
                    // No raffle, 5 NCG for 1/3 of 15% distribution
                    NcgReward = 5 * NCG,
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 66 },
                        { 600203, 0 },
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 0 },
                        { 30001, 0 },
                    },
                },
            };
        }

        public static IEnumerable<object[]> GetPrevRewardTestData()
        {
            yield return new object[]
            {
                true, false, new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 0 * NCG, // No NCG Reward
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 240 },
                        { 600202, 0 },
                        { 600203, 0 },
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
                false, true, new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 20 * NCG, // 5NCG for explore raffle, 15NCG for 15% distribution
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 67 },
                        { 600203, 0 },
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
                true, true, new AdventureBossGameData.ClaimableReward
                {
                    // 5NCG for explore raffle, 15NCG for 15% distribution
                    NcgReward = 20 * NCG,
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 240 },
                        { 600202, 67 },
                        { 600203, 0 },
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
                false, false, new AdventureBossGameData.ClaimableReward
                {
                    NcgReward = 0 * NCG,
                    ItemReward = new Dictionary<int, int>
                    {
                        { 600201, 0 },
                        { 600202, 0 },
                        { 600203, 0 },
                    },
                    FavReward = new Dictionary<int, int>
                    {
                        { 20001, 0 },
                        { 30001, 0 },
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
            AdventureBossGameData.ClaimableReward expectedReward
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

            Assert.True(resultState.GetBountyBoard(1).Investors
                .First(inv => inv.AvatarAddress == TesterAvatarAddress).Claimed);

            Test(resultState, expectedReward);
        }

        [Fact]
        public void WantedMultipleSeason()
        {
            const int seed = 1;
            // Settings
            var expectedReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = 0 * NCG, // No Raffle Reward
                FavReward = new Dictionary<int, int>
                {
                    { 20001, 0 },
                    { 30001, 0 },
                },
                ItemReward = new Dictionary<int, int>
                {
                    { 600201, 480 },
                    { 600202, 0 },
                    { 600203, 0 },
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
                RandomSeed = seed,
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
                RandomSeed = seed,
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

            for (var szn = 3; szn > 0; szn--)
            {
                var investor = resultState.GetBountyBoard(szn).Investors
                    .FirstOrDefault(inv => inv.AvatarAddress == TesterAvatarAddress);
                if (investor is not null)
                {
                    Assert.True(investor.Claimed);
                }
            }

            Test(resultState, expectedReward);
        }

        [Theory]
        [MemberData(nameof(GetExploreTestData))]
        public void ExploreReward(
            int seed,
            int bounty,
            int anotherExplorerCount,
            AdventureBossGameData.ClaimableReward expectedReward
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

            state = Stake(state, WantedAddress);

            // Wanted
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = bounty * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = 0L,
                RandomSeed = seed,
            });

            // Explore : just add data to avoid explore reward
            // Manipulate used AP Potion to calculate reward above zero
            var board = state.GetExploreBoard(1);
            var exp = state.TryGetExplorer(1, TesterAvatarAddress, out var e)
                ? e
                : new Explorer(TesterAvatarAddress, TesterAvatarState.name);
            board.ExplorerList.Add((TesterAvatarAddress, TesterAvatarState.name));
            board.UsedApPotion += 100;
            exp.UsedApPotion += 100;
            state = state.SetExploreBoard(1, board).SetExplorer(1, exp);

            var materialSheet = state.GetSheet<MaterialItemSheet>();
            var materialRow =
                materialSheet.OrderedList.First(i => i.ItemSubType == ItemSubType.ApStone);
            var potion = ItemFactory.CreateMaterial(materialRow);
            for (var i = 0; i < anotherExplorerCount; i++)
            {
                var inventory = state.GetInventoryV2(ExplorerAvatarAddress);
                inventory.AddItem(potion, 1);
                state = state.SetInventory(ExplorerAvatarAddress, inventory);
                state = new ExploreAdventureBoss
                {
                    Season = 1,
                    AvatarAddress = ExplorerAvatarAddress,
                }.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = ExplorerAddress,
                    BlockIndex = 1L,
                    RandomSeed = seed,
                });

                // Manipulate used AP Potion to calculate reward above zero
                board = state.GetExploreBoard(1);
                board.UsedApPotion += 99;
                exp = state.GetExplorer(1, ExplorerAvatarAddress);
                exp.UsedApPotion += 99;
                state = state.SetExploreBoard(1, board).SetExplorer(1, exp);
            }

            // Burn all remaining NCG to make test easier
            state = state.BurnAsset(
                new ActionContext
                {
                    RandomSeed = seed,
                },
                TesterAddress,
                state.GetBalance(TesterAddress, NCG)
            );

            // Claim
            var resultState = new ClaimAdventureBossReward
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().EndBlockIndex,
                RandomSeed = seed,
            });

            // Test
            var exploreBoard = resultState.GetExploreBoard(1);
            Assert.NotNull(exploreBoard.RaffleWinner);
            Assert.NotNull(exploreBoard.RaffleWinnerName);
            if (anotherExplorerCount == 0)
            {
                Assert.Equal(TesterAvatarAddress, exploreBoard.RaffleWinner);
                Assert.Equal(TesterAvatarState.name, exploreBoard.RaffleWinnerName);
            }

            Assert.Equal((int)(bounty * 0.05) * NCG, exploreBoard.RaffleReward);
            Assert.True(resultState.GetExplorer(1, TesterAvatarAddress).Claimed);

            Test(resultState, expectedReward);
        }

        [Fact]
        public void ExploreMultipleSeason()
        {
            const int seed = 1;
            // Settings
            var expectedReward = new AdventureBossGameData.ClaimableReward
            {
                // (5NCG for raffle, 15NCG for 15% distribution) for season 1 and 3
                NcgReward = 40 * NCG,
                FavReward = new Dictionary<int, int>
                {
                    { 20001, 0 },
                    { 30001, 0 },
                },
                ItemReward = new Dictionary<int, int>
                {
                    { 600201, 0 },
                    { 600202, 134 }, // (100 AP / 1.5 Ratio * 100% contribution) for season 1 and 3
                    { 600203, 0 },
                },
            };
            var state = _initialState;
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = Stake(state, TesterAddress);
            state = Stake(state, WantedAddress);
            state = Stake(state, ExplorerAddress);

            // Explore for season 1, 3 : just add data to avoid explore reward
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed,
            });

            // Manipulate used AP Potion to calculate reward above zero
            var board = state.GetExploreBoard(1);
            board.ExplorerList.Add((TesterAvatarAddress, TesterAvatarState.name));
            board.UsedApPotion += 100;
            var exp = state.TryGetExplorer(1, TesterAvatarAddress, out var e)
                ? e
                : new Explorer(TesterAvatarAddress, TesterAvatarState.name);
            exp.UsedApPotion += 100;
            state = state.SetExploreBoard(1, board).SetExplorer(1, exp);

            // No Explore
            state = new Wanted
            {
                Season = 2,
                AvatarAddress =
                    ExplorerAvatarAddress, // To avoid wanted for two seasons in a row error
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = ExplorerAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed,
            });

            state = new Wanted
            {
                Season = 3,
                AvatarAddress = WantedAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed,
            });

            // Manipulate used AP Potion to calculate reward above zero
            board = state.GetExploreBoard(3);
            board.ExplorerList.Add((TesterAvatarAddress, TesterAvatarState.name));
            board.UsedApPotion += 100;
            exp = state.TryGetExplorer(3, TesterAvatarAddress, out e)
                ? e
                : new Explorer(TesterAvatarAddress, TesterAvatarState.name);
            exp.UsedApPotion += 100;
            state = state.SetExploreBoard(3, board).SetExplorer(3, exp);

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

            for (var szn = 3; szn > 0; szn--)
            {
                if (resultState.TryGetExplorer(szn, TesterAvatarAddress, out var explorer))
                {
                    Assert.True(explorer.Claimed);
                }
            }

            Test(resultState, expectedReward);
        }

        [Fact]
        public void AllReward()
        {
            const int seed = 1;
            // Settings
            var expectedReward = new AdventureBossGameData.ClaimableReward
            {
                // 5NCG for explore raffle, 15NCG for 15% distribution.
                NcgReward = 20 * NCG,
                FavReward = new Dictionary<int, int>
                {
                    { 20001, 0 },
                    { 30001, 0 },
                },
                ItemReward = new Dictionary<int, int>
                {
                    { 600201, 240 },
                    { 600202, 67 },
                    { 600203, 0 },
                },
            };
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
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = 0L,
                RandomSeed = seed,
            });

            // Explore : just add data to avoid explore reward
            // Manipulate used AP Potion to calculate reward above zero
            var board = state.GetExploreBoard(1);
            board.UsedApPotion += 100;
            board.ExplorerList.Add((TesterAvatarAddress, TesterAvatarState.name));
            var exp = state.TryGetExplorer(1, TesterAvatarAddress, out var e)
                ? e
                : new Explorer(TesterAvatarAddress, TesterAvatarState.name);
            exp.UsedApPotion += 100;
            state = state.SetExploreBoard(1, board).SetExplorer(1, exp);

            // Burn
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
                BlockIndex = state.GetLatestAdventureBossSeason().EndBlockIndex,
                RandomSeed = seed,
            });

            Test(resultState, expectedReward);
        }

        [Theory]
        [MemberData(nameof(GetPrevRewardTestData))]
        public void PrevReward(
            bool wanted,
            bool explore,
            AdventureBossGameData.ClaimableReward expectedReward
        )
        {
            // Settings
            const int seed = 1;
            var state = _initialState;
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = Stake(state, TesterAddress);
            state = Stake(state, WantedAddress);
            state = Stake(state, ExplorerAddress);

            // Wanted
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = wanted ? TesterAvatarAddress : WantedAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = wanted ? TesterAddress : WantedAddress,
                BlockIndex = 0L,
                RandomSeed = seed,
            });

            // Explore : just add data to avoid explore reward
            // Manipulate used AP Potion to calculate reward above zero
            var board = state.GetExploreBoard(1);
            board.UsedApPotion += 100;
            board.ExplorerList.Add(explore
                ? (TesterAvatarAddress, TesterAvatarState.name)
                : (ExplorerAvatarAddress, ExplorerAvatarState.name));
            var exp =
                state.TryGetExplorer(
                    1, explore ? TesterAvatarAddress : ExplorerAvatarAddress, out var e
                )
                    ? e
                    : new Explorer(
                        explore ? TesterAvatarAddress : ExplorerAvatarAddress,
                        explore ? TesterAvatarState.name : ExplorerAvatarState.name
                    );
            exp.UsedApPotion += 100;
            state = state.SetExploreBoard(1, board).SetExplorer(1, exp);

            // Next Season
            state = new Wanted
            {
                Season = 2,
                AvatarAddress = ExplorerAvatarAddress,
                Bounty = 100 * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = ExplorerAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().NextStartBlockIndex,
                RandomSeed = seed,
            });

            // Burn
            state = state.BurnAsset(
                new ActionContext(),
                TesterAddress,
                state.GetBalance(TesterAddress, NCG)
            );

            // Test
            var resultState = new ClaimAdventureBossReward
            {
                Season = 2,
                AvatarAddress = TesterAvatarAddress,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = TesterAddress,
                BlockIndex = state.GetLatestAdventureBossSeason().EndBlockIndex,
                RandomSeed = seed + 2,
            });
            Test(resultState, expectedReward);
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

        private void Test(IWorld world, AdventureBossGameData.ClaimableReward expectedReward)
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

            var inventory = world.GetInventoryV2(TesterAvatarAddress);
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
