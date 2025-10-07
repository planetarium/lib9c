namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class ArenaAvatarStateTest
    {
        private readonly TableSheets _tableSheets;

        public ArenaAvatarStateTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var state = new ArenaAvatarState(avatarState);

            var serialized = (List)state.Serialize();
            var deserialized = new ArenaAvatarState(serialized);

            Assert.Equal(state.Costumes, deserialized.Costumes);
            Assert.Equal(state.Equipments, deserialized.Equipments);
        }

        private AvatarState GetNewAvatarState(Address avatarAddress, Address agentAddress)
        {
            var rankingState = new RankingState1();
            return AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingState.UpdateRankingMap(avatarAddress));
        }
    }
}
