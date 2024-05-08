namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class WantedTest
    {
        [Fact]
        public void Execute()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
            var ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(ncg);
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = Addresses.GetAvatarAddress(agentAddress, 0);
            var avatarAddress2 = Addresses.GetAvatarAddress(agentAddress, 1);
            var agentState = new AgentState(agentAddress)
            {
                avatarAddresses =
                {
                    [0] = avatarAddress,
                    [1] = avatarAddress2,
                },
            };
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .MintAsset(new ActionContext(), agentAddress, 300 * ncg);

            var action = new Wanted
            {
                AvatarAddress = avatarAddress,
                Bounty = 100 * ncg,
            };
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddress,
                BlockIndex = 0L,
            });
            Assert.Equal(200 * ncg, nextState.GetBalance(agentAddress, ncg));
            Assert.Equal(100 * ncg, nextState.GetBalance(Addresses.BountyBoard, ncg));
            var bountyBoard = nextState.GetBountyBoard(0);
            Assert.NotNull(bountyBoard);
            var investor = Assert.Single(bountyBoard.Investors);
            Assert.Equal(avatarAddress, investor.AvatarAddress);
            Assert.Equal(100 * ncg, investor.Price);
            Assert.Equal(1, investor.Count);

            action.AvatarAddress = avatarAddress2;
            nextState = action.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = agentAddress,
                BlockIndex = 1L,
            });

            Assert.Equal(100 * ncg, nextState.GetBalance(agentAddress, ncg));
            Assert.Equal(200 * ncg, nextState.GetBalance(Addresses.BountyBoard, ncg));
            bountyBoard = nextState.GetBountyBoard(0);
            Assert.NotNull(bountyBoard);
            Assert.Equal(2, bountyBoard.Investors.Count);
            investor = bountyBoard.Investors.First(i => i.AvatarAddress == avatarAddress2);
            Assert.Equal(100 * ncg, investor.Price);
            Assert.Equal(1, investor.Count);

            action.AvatarAddress = avatarAddress;
            nextState = action.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = agentAddress,
                BlockIndex = 2L,
            });

            Assert.Equal(0 * ncg, nextState.GetBalance(agentAddress, ncg));
            Assert.Equal(300 * ncg, nextState.GetBalance(Addresses.BountyBoard, ncg));
            bountyBoard = nextState.GetBountyBoard(0);
            Assert.NotNull(bountyBoard);
            Assert.Equal(2, bountyBoard.Investors.Count);
            investor = bountyBoard.Investors.First(i => i.AvatarAddress == avatarAddress);
            Assert.Equal(200 * ncg, investor.Price);
            Assert.Equal(2, investor.Count);
        }
    }
}
