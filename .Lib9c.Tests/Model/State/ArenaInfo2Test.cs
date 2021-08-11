namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaInfo2Test
    {
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;

        public ArenaInfo2Test()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                Addresses.Blacksmith,
                Addresses.Admin,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "name"
            );
        }

        [Fact]
        public void Serialize()
        {
            var info = new ArenaInfo2(_avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, false);
            var deserialized = new ArenaInfo2((List)info.Serialize());
            Assert.Equal(info, deserialized);
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var info = new ArenaInfo2(_avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, false);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, info);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (ArenaInfo2)formatter.Deserialize(ms);

            Assert.Equal(info.Serialize(), deserialized.Serialize());
        }

        [Theory]
        [InlineData(1000, 1001, 1)]
        [InlineData(1001, 1100, 2)]
        [InlineData(1100, 1200, 3)]
        [InlineData(1200, 1400, 4)]
        [InlineData(1400, 1800, 5)]
        [InlineData(1800, 10000, 6)]
        public void GetRewardCount(int minScore, int maxScore, int expected)
        {
            var score = new Random().Next(minScore, maxScore);
            var prevInfo = new ArenaInfo2(_avatarState, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet, false);
            var serialized = (List)prevInfo.Serialize();
            serialized = (List)serialized.Replace(prevInfo.Score.Serialize(), score.Serialize());
            var info = new ArenaInfo2(serialized);
            Assert.Equal(expected, info.GetRewardCount());
        }
    }
}
