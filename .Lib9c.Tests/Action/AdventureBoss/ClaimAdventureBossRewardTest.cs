namespace Lib9c.Tests.Action.AdventureBoss
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ClaimAdventureBossRewardTest
    {
        [Theory]
        [InlineData(10_000L, null)]
        [InlineData(1, typeof(SeasonInProgressException))]
        [InlineData(1_000_000L, typeof(ClaimExpiredException))]
        public void Execute(long blockIndex, Type exc)
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
