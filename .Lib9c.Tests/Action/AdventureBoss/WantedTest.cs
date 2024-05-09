namespace Lib9c.Tests.Action.AdventureBoss
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class WantedTest
    {
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        private static readonly Address AgentAddress = new PrivateKey().Address;
        private static readonly Address AvatarAddress = Addresses.GetAvatarAddress(AgentAddress, 0);
        private static readonly Address AvatarAddress2 = Addresses.GetAvatarAddress(AgentAddress, 1);

        private readonly GoldCurrencyState _goldCurrencyState = new (NCG);

        private readonly AgentState _agentState = new (AgentAddress)
        {
            avatarAddresses =
            {
                [0] = AvatarAddress,
                [1] = AvatarAddress2,
            },
        };

        [Fact]
        public void Execute()
        {
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, _goldCurrencyState.Serialize())
                .SetAgentState(AgentAddress, _agentState)
                .MintAsset(new ActionContext(), AgentAddress, 300 * NCG);

            // Set avtive season
            state = state.SetSeasonInfo(new SeasonInfo(1, 0L));

            var action = new Wanted
            {
                Season = 1,
                AvatarAddress = AvatarAddress,
                Bounty = 100 * NCG,
            };
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = AgentAddress,
                BlockIndex = 0L,
            });
            Assert.Equal(200 * NCG, nextState.GetBalance(AgentAddress, NCG));
            Assert.Equal(100 * NCG, nextState.GetBalance(Addresses.BountyBoard, NCG));
            var bountyBoard = nextState.GetBountyBoard(1);
            Assert.NotNull(bountyBoard);
            var investor = Assert.Single(bountyBoard.Investors);
            Assert.Equal(AvatarAddress, investor.AvatarAddress);
            Assert.Equal(100 * NCG, investor.Price);
            Assert.Equal(1, investor.Count);

            action.AvatarAddress = AvatarAddress2;
            nextState = action.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = AgentAddress,
                BlockIndex = 1L,
            });

            Assert.Equal(100 * NCG, nextState.GetBalance(AgentAddress, NCG));
            Assert.Equal(200 * NCG, nextState.GetBalance(Addresses.BountyBoard, NCG));
            bountyBoard = nextState.GetBountyBoard(1);
            Assert.NotNull(bountyBoard);
            Assert.Equal(2, bountyBoard.Investors.Count);
            investor = bountyBoard.Investors.First(i => i.AvatarAddress == AvatarAddress2);
            Assert.Equal(100 * NCG, investor.Price);
            Assert.Equal(1, investor.Count);

            action.AvatarAddress = AvatarAddress;
            nextState = action.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = AgentAddress,
                BlockIndex = 2L,
            });

            Assert.Equal(0 * NCG, nextState.GetBalance(AgentAddress, NCG));
            Assert.Equal(300 * NCG, nextState.GetBalance(Addresses.BountyBoard, NCG));
            bountyBoard = nextState.GetBountyBoard(1);
            Assert.NotNull(bountyBoard);
            Assert.Equal(2, bountyBoard.Investors.Count);
            investor = bountyBoard.Investors.First(i => i.AvatarAddress == AvatarAddress);
            Assert.Equal(200 * NCG, investor.Price);
            Assert.Equal(2, investor.Count);
        }

        [Fact]
        public void CreateNewSeason()
        {
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, _goldCurrencyState.Serialize())
                .SetAgentState(AgentAddress, _agentState)
                .MintAsset(new ActionContext(), AgentAddress, 300 * NCG);

            // Validate no prev. season
            var latestSeasonInfo = state.GetLatestAdventureBossSeason();
            Assert.Equal(0, latestSeasonInfo.SeasonId);
            Assert.Equal(0, latestSeasonInfo.StartBlockIndex);
            Assert.Equal(0, latestSeasonInfo.EndBlockIndex);
            Assert.Equal(0, latestSeasonInfo.NextStartBlockIndex);

            var action = new Wanted
            {
                Season = 1,
                AvatarAddress = AvatarAddress,
                Bounty = 100 * NCG,
            };
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = AgentAddress,
                BlockIndex = 0L,
            });

            // Validate new season
            latestSeasonInfo = nextState.GetLatestAdventureBossSeason();
            Assert.Equal(1, latestSeasonInfo.SeasonId);
            Assert.Equal(0L, latestSeasonInfo.StartBlockIndex);
            Assert.Equal(SeasonInfo.BossActiveBlockInterval, latestSeasonInfo.EndBlockIndex);
            Assert.Equal(
                SeasonInfo.BossActiveBlockInterval + SeasonInfo.BossInactiveBlockInterval,
                latestSeasonInfo.NextStartBlockIndex
            );

            var season1 = nextState.GetSeasonInfo(1);
            Assert.Equal(latestSeasonInfo.SeasonId, season1.Season);
            Assert.Equal(latestSeasonInfo.StartBlockIndex, season1.StartBlockIndex);
            Assert.Equal(latestSeasonInfo.EndBlockIndex, season1.EndBlockIndex);
            Assert.Equal(latestSeasonInfo.NextStartBlockIndex, season1.NextStartBlockIndex);
        }

        [Fact]
        public void SeasonAlreadyInProgress()
        {
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, _goldCurrencyState.Serialize())
                .SetAgentState(AgentAddress, _agentState)
                .MintAsset(new ActionContext(), AgentAddress, 300 * NCG);

            // Set active season
            var seasonInfo = new SeasonInfo(1, 0L);
            state = state.SetSeasonInfo(seasonInfo);
            state = state.SetLatestAdventureBossSeason(seasonInfo);
            var latestSeasonInfo = state.GetLatestAdventureBossSeason();
            Assert.Equal(1, latestSeasonInfo.SeasonId);
            Assert.Equal(0L, latestSeasonInfo.StartBlockIndex);
            Assert.Equal(SeasonInfo.BossActiveBlockInterval, latestSeasonInfo.EndBlockIndex);
            Assert.Equal(
                SeasonInfo.BossActiveBlockInterval + SeasonInfo.BossInactiveBlockInterval,
                latestSeasonInfo.NextStartBlockIndex
            );

            // Try to create new season within season 1
            var action = new Wanted
            {
                Season = 2,
                AvatarAddress = AvatarAddress,
                Bounty = 100 * NCG,
            };
            Assert.Throws<InvalidAdventureBossSeasonException>(() => action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = AgentAddress,
                    BlockIndex = 100L,
                }
            ));
        }

        [Fact]
        public void CannotCreateSeason0()
        {
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, _goldCurrencyState.Serialize())
                .SetAgentState(AgentAddress, _agentState)
                .MintAsset(new ActionContext(), AgentAddress, 300 * NCG);

            var action = new Wanted
            {
                Season = 0,
                AvatarAddress = AvatarAddress,
                Bounty = 1 * NCG,
            };

            Assert.Throws<InvalidAdventureBossSeasonException>(() =>
                action.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = AgentAddress,
                        BlockIndex = 0L,
                    }
                )
            );
        }
    }
}
