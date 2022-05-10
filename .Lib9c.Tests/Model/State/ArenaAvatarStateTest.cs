namespace Lib9c.Tests.Model.State
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
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var state = new ArenaAvatarState(avatarState);

            var serialized = (List)state.Serialize();
            var deserialized = new ArenaAvatarState(serialized);

            Assert.Equal(state.Records.Serialize(), deserialized.Records.Serialize());
            var record = (List)state.Records.Serialize();
            var arenaRecord = new ArenaRecord(record);
            var serializedAR = (List)arenaRecord.Serialize();
            var deserializedAR = new ArenaRecord(serializedAR);

            Assert.Equal(arenaRecord.Win, deserializedAR.Win);
            Assert.Equal(arenaRecord.Lose, deserializedAR.Lose);
            Assert.Equal(arenaRecord.Score, deserializedAR.Score);

            Assert.Equal(state.Costumes, deserialized.Costumes);
            Assert.Equal(state.Equipments, deserialized.Equipments);
            Assert.Equal(state.Ticket, deserialized.Ticket);
            Assert.Equal(state.NcgTicket, deserialized.NcgTicket);
            Assert.Equal(state.Level, deserialized.Level);
            Assert.Equal(state.NameWithHash, deserialized.NameWithHash);
            Assert.Equal(state.CharacterId, deserialized.CharacterId);
            Assert.Equal(state.HairIndex, deserialized.HairIndex);
            Assert.Equal(state.LensIndex, deserialized.LensIndex);
            Assert.Equal(state.EarIndex, deserialized.EarIndex);
            Assert.Equal(state.TailIndex, deserialized.TailIndex);
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
