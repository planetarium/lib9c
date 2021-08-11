namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class WeeklyArenaState2Test
    {
        private readonly TableSheets _tableSheets;

        public WeeklyArenaState2Test()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
        }

        [Theory]
        [InlineData(1, "f0Ef771190B03C4Fc83B40D77F1DC8f1a6A53869")]
        [InlineData(2, "967665b97294D1ed17446E04AD562C1201D89827")]
        public void DeriveAddress(int index, string expected)
        {
            var state = new WeeklyArenaState2(index);
            Assert.Equal(new Address(expected), state.address);
        }

        [Fact]
        public void Serialize()
        {
            var address = default(Address);
            var state = new WeeklyArenaState2(address);
            for (var i = 0; i < 3; i++)
            {
                state.Update(new PrivateKey().ToAddress());
            }

            var serialized = (List)state.Serialize();
            var deserialized = new WeeklyArenaState2(serialized);

            Assert.Equal(state, deserialized);
        }

        [Fact]
        public void Serialize_With_DotNetAPI()
        {
            var address = default(Address);
            var state = new WeeklyArenaState2(address);
            for (var i = 0; i < 3; i++)
            {
                state.Update(new PrivateKey().ToAddress());
            }

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (WeeklyArenaState2)formatter.Deserialize(ms);
            Assert.Equal(state, deserialized);
        }
    }
}
