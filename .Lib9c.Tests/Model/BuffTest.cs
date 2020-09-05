namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Stat;
    using Xunit;

    public class BuffTest
    {
        private readonly TableSheets _tableSheets;

        public BuffTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Clone()
        {
            var row = _tableSheets.BuffSheet.Values.First(r => r.StatModifier.StatType == StatType.ATK);
            var buff = new AttackBuff(row);
            var clone = buff.Clone();

            Assert.Equal(buff, clone);
        }

        [Fact]
        public void Serialize()
        {
            var row = _tableSheets.BuffSheet.Values.First(r => r.StatModifier.StatType == StatType.ATK);
            var buff = new AttackBuff(row);
            var serialized = buff.Serialize();
            var deserialized = new AttackBuff(serialized);

            Assert.Equal(buff, deserialized);
        }
    }
}
