namespace Lib9c.Tests.Action.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
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

        private static readonly Address Explorer1Address =
            Addresses.GetAvatarAddress(ExplorerAddress, 0);

        private static readonly AvatarState Explorer1State = new (
            Explorer1Address, ExplorerAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "explorer1"
        );

        private static readonly Address Explorer2Address =
            Addresses.GetAvatarAddress(ExplorerAddress, 1);

        private static readonly AvatarState Explorer2State = new (
            Explorer2Address, ExplorerAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "explorer2"
        );

        private static readonly AgentState ExplorerState = new (ExplorerAddress)
        {
            avatarAddresses =
            {
                [0] = Explorer1Address,
                [1] = Explorer2Address,
            },
        };

        private readonly IWorld _initialState = new World(MockUtil.MockModernWorldState)
            .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(NCG).Serialize())
            .SetAvatarState(WantedAvatarAddress, WantedAvatarState)
            .SetAgentState(WantedAddress, WantedState)
            .SetAvatarState(Explorer1Address, Explorer1State)
            .SetAvatarState(Explorer2Address, Explorer2State)
            .SetAgentState(ExplorerAddress, ExplorerState)
            .MintAsset(new ActionContext(), WantedAddress, InitialBalance * NCG)
            .MintAsset(new ActionContext(), ExplorerAddress, InitialBalance * NCG);

        [Theory]
        [InlineData(new[] { 1 }, 2, false, false, true, false, null)]
        public void Execute(
            int[] prevSeasonList,
            int currentSeason,
            bool prevWanted,
            bool prevExplore,
            bool wanted,
            bool explore,
            Type exc
        )
        {
            // Set test environment
            var state = _initialState;
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = new Stake(new BigInteger(500_000)).Execute(new ActionContext
            {
                PreviousState = state,
                Signer = WantedAddress,
                BlockIndex = 0L,
            });
            state = new Stake(new BigInteger(500_000)).Execute(new ActionContext
            {
                PreviousState = state,
                Signer = ExplorerAddress,
                BlockIndex = 0L,
            });

            // Prev. seasons
            foreach (var season in prevSeasonList)
            {
                var seasonInfo = new SeasonInfo(season, 0, 100, 200);
                var bountyBoard = new BountyBoard(season);
                if (prevWanted)
                {
                    bountyBoard.Investors.Add(new Investor(
                        Explorer1Address,
                        Explorer1State.name,
                        Wanted.MinBounty * NCG
                    ));
                }
                else
                {
                    bountyBoard.Investors.Add(new Investor(
                        WantedAvatarAddress,
                        WantedAvatarState.name,
                        Wanted.MinBounty * NCG
                    ));
                }

                var exploreBoard = new ExploreBoard(season);
                if (prevExplore)
                {
                    var explorer = new Explorer(Explorer1Address);
                    state = state.SetExplorer(season, explorer);
                    exploreBoard.ExplorerList.Add(Explorer1Address);
                }

                state = state.SetBountyBoard(season, bountyBoard);
                state = state.SetExploreBoard(season, exploreBoard);
                state = state.SetSeasonInfo(seasonInfo);
                state = state.SetLatestAdventureBossSeason(seasonInfo);
            }

            // Action
            state = new Wanted
            {
                Season = currentSeason,
                AvatarAddress = wanted ? Explorer1Address : WantedAvatarAddress,
                Bounty = Wanted.MinBounty * NCG,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = wanted ? ExplorerAddress : WantedAddress,
                BlockIndex = 200L,
            });

            if (explore)
            {
                state = new AdventureBossBattle
                {
                    Season = currentSeason,
                    AvatarAddress = Explorer1Address,
                }.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = ExplorerAddress,
                    BlockIndex = 201L,
                });
            }

            // Claim
            var claimAction = new ClaimAdventureBossReward
            {
                Season = currentSeason,
                AvatarAddress = Explorer1Address,
            };

            // Test
            if (exc is not null)
            {
                Assert.Throws(exc, () => claimAction.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = ExplorerAddress,
                        BlockIndex = 200 + SeasonInfo.BossActiveBlockInterval + 1,
                    }
                ));
            }
            else
            {
                var resultState = claimAction.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = ExplorerAddress,
                    BlockIndex = 200 + SeasonInfo.BossActiveBlockInterval + 1,
                });
            }
        }

        [Theory]
        [InlineData(10_000L, null, Skip = "WIP: reward action")]
        [InlineData(1, typeof(SeasonInProgressException), Skip = "WIP: reward action")]
        [InlineData(1_000_000L, typeof(ClaimExpiredException), Skip = "WIP: reward action")]
        public void ExecuteLegacy(long blockIndex, Type exc)
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = Addresses.GetAvatarAddress(agentAddress, 0);
            var agentState = new AgentState(agentAddress)
            {
                avatarAddresses =
                {
                    [0] = avatarAddress,
                },
            };
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0L,
                tableSheets.GetAvatarSheets(),
                default
            );
            Assert.Empty(avatarState.inventory.Items);
            var state = new World(MockUtil.MockModernWorldState)
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            state = sheets.Aggregate(
                state,
                (current, kv) =>
                    LegacyModule.SetLegacyState(
                        current,
                        Addresses.GetSheetAddress(kv.Key),
                        (Text)kv.Value
                    )
            );
            state = state.SetSeasonInfo(new SeasonInfo(1, 0L));

            var action = new ClaimAdventureBossReward
            {
                Season = 1,
                AvatarAddress = avatarAddress,
            };

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    BlockIndex = blockIndex,
                });
                var nextAvatarState = nextState.GetAvatarState(avatarAddress);
                Assert.Single(nextAvatarState.inventory.Items);
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(new ActionContext
                        { PreviousState = state, Signer = agentAddress, BlockIndex = blockIndex })
                );
            }
        }
    }
}
