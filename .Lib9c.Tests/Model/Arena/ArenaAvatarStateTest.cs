namespace Lib9c.Tests.Model.Arena
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
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
        public void Serialize_BackwardCompatibility()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var state = new ArenaAvatarState(avatarState);
            Assert.Equal(0, state.CP);

            var serialized = (List)state.Serialize();
            var deserialized = new ArenaAvatarState(serialized);

            Assert.Equal(state.Costumes, deserialized.Costumes);
            Assert.Equal(state.Equipments, deserialized.Equipments);
            Assert.Equal(state.CP, deserialized.CP);
        }

        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var state = new ArenaAvatarState(avatarState, 100);
            Assert.Equal(100, state.CP);

            var serialized = (List)state.Serialize();
            var deserialized = new ArenaAvatarState(serialized);

            Assert.Equal(state.Costumes, deserialized.Costumes);
            Assert.Equal(state.Equipments, deserialized.Equipments);
            Assert.Equal(state.CP, deserialized.CP);
        }

        private AvatarState GetNewAvatarState(Address avatarAddress, Address agentAddress)
        {
            var rankingState = new RankingState1();
            return new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingState.UpdateRankingMap(avatarAddress));
        }
    }
}
