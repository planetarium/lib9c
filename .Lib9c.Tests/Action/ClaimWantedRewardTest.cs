namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ClaimWantedRewardTest
    {
        [Fact]
        public void Execute()
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

            state = sheets.Aggregate(state, (current, kv) => current.SetLegacyState(Addresses.GetSheetAddress(kv.Key), (Text)kv.Value));

            var action = new ClaimWantedReward()
            {
                AvatarAddress = avatarAddress,
                Season = 0,
            };
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddress,
                BlockIndex = 0L,
            });
            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            Assert.Single(nextAvatarState.inventory.Items);
        }
    }
}
