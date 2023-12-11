namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class MaterialTest
    {
        private readonly MaterialItemSheet _sheet;
        private readonly MaterialItemSheet.Row _materialRow;

        public MaterialTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _sheet = tableSheets.MaterialItemSheet;
            _materialRow = tableSheets.MaterialItemSheet.First;
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);
            var serialized = material.Serialize();
            var deserialized = new Material((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(material, deserialized);
        }

        [Fact]
        public void SerializeWithDotNetApi()
        {
            Assert.NotNull(_materialRow);

            var material = new Material(_materialRow);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, material);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Material)formatter.Deserialize(ms);

            Assert.Equal(material, deserialized);
        }

        [Fact(Skip = "This is only to get material's fungibleID in CSV format.")]
        // [Fact]
        public void GetFungibleIdCSV()
        {
            var reader = TableSheetsImporter.ImportSheets()["MaterialItemSheet"].Split("\n").Skip(1)
                .Where(row => !row.StartsWith("_")).ToList();
            reader.Sort();
            var writer = new StringBuilder();
            writer.AppendLine("ID,Name,Fungible ID");
            Assert.Equal(reader.Count, _sheet.Count);
            foreach (var r in reader)
            {
                var row = r.Split(",");
                var sheetRow = _sheet[int.Parse(row[0])];
                writer.AppendLine($"{sheetRow.Id},{row[1]},{sheetRow.ItemId}");
            }

            File.WriteAllText(
                Path.Combine(
                    Path.GetFullPath("../../").Replace(
                        Path.Combine(".Lib9c.Tests", "bin"),
                        Path.Combine("Lib9c", "TableCSV")),
                    $"_MaterialFungibleId_{DateTimeOffset.Now.Date:yyyy-MM-dd}.csv"),
                writer.ToString()
            );
        }
    }
}
