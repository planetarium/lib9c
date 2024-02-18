namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class MigrationAvatarStateTest
    {
        private readonly TableSheets _tableSheets;

        public MigrationAvatarStateTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new World(
                new MockWorldState()
                    .SetState(ReservedAddresses.LegacyAccount, AdminState.Address, new AdminState(admin, 100).Serialize())
                    .SetState(ReservedAddresses.LegacyAccount, avatarAddress, MigrationAvatarState.LegacySerializeV2(avatarState)));

            var action = new MigrationAvatarState
            {
                avatarStates = new List<Dictionary>
                {
                    (Dictionary)MigrationAvatarState.LegacySerializeV1(avatarState),
                },
            };

            IWorld nextState = action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = admin,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            Assert.NotNull(nextAvatarState.inventory);
            Assert.NotNull(nextAvatarState.worldInformation);
            Assert.NotNull(nextAvatarState.questList);
        }
    }
}
