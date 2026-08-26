namespace Lib9c.Tests.TableData
{
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.TableData;
    using Xunit;

    public class RestrictionSheetTest
    {
        private const string Header = "key,market_registrable,synthesize_material\n";

        [Fact]
        public void ShippedCsvPassesItsOwnValidator()
        {
            Assert.True(TableSheetsImporter.TryGetCsv(nameof(RestrictionSheet), out var csv));
            RestrictionSheet.ValidateCsv(csv);
        }

        [Fact]
        public void ShippedCsvMatchesHardcodedSynthesizeDenyList()
        {
            var sheet = Shipped();
            foreach (var itemId in Synthesize.InvalidMaterialItemId)
            {
                Assert.False(sheet.IsItemSynthesizeMaterial(itemId));
            }

            Assert.Equal(
                Synthesize.InvalidMaterialItemId.Length,
                sheet.Values.Count(row => row.SynthesizeMaterial is false));
        }

        [Fact]
        public void ShippedCsvMatchesHardcodedCurrencyDenyList()
        {
            var sheet = Shipped();
            foreach (var currency in RegisterProduct.NonTradableTickerCurrencies)
            {
                Assert.False(sheet.IsCurrencyMarketRegistrable(currency.Ticker));
            }

            Assert.Equal(
                RegisterProduct.NonTradableTickerCurrencies.Count,
                sheet.Values.Count(row => row.MarketRegistrable is false));
        }

        [Fact]
        public void EmptyCellMeansUnspecified()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,,\n");

            Assert.True(sheet.IsItemMarketRegistrable(1));
            Assert.True(sheet.IsItemSynthesizeMaterial(1));
            Assert.True(sheet.TryGetValue("1", out var row));
            Assert.Null(row.MarketRegistrable);
            Assert.Null(row.SynthesizeMaterial);
        }

        [Fact]
        public void UnlistedTargetIsUnrestricted()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,false,false\n");

            Assert.True(sheet.IsItemMarketRegistrable(2));
            Assert.True(sheet.IsItemSynthesizeMaterial(2));
            Assert.True(sheet.IsCurrencyMarketRegistrable("NCG"));
        }

        [Fact]
        public void UnparsableCellRestricts()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,flase,\n");

            Assert.False(sheet.IsItemMarketRegistrable(1));
            Assert.True(sheet.IsItemSynthesizeMaterial(1));

            // The tolerant runtime and the strict patch time validator disagree on purpose.
            Assert.Throws<SheetRowValidateException>(
                () => RestrictionSheet.ValidateCsv($"{Header}1,flase,\n"));
        }

        [Fact]
        public void MissingColumnsAreUnspecified()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1\n");

            Assert.True(sheet.IsItemMarketRegistrable(1));
            Assert.True(sheet.IsItemSynthesizeMaterial(1));
        }

        [Fact]
        public void OneUnusableRowDoesNotDropTheRest()
        {
            var sheet = new RestrictionSheet();

            // Blank lines would collide on the same key, and a short row has no policy at all;
            // neither may take the sheet down with it.
            sheet.Set($"{Header}\n\n1\n2,false,false\n");

            Assert.Equal(2, sheet.Count);
            Assert.False(sheet.IsItemMarketRegistrable(2));
            Assert.False(sheet.ContainsKey(string.Empty));
        }

        [Fact]
        public void DuplicatedKeyMergesTowardRestriction()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,true,\n1,false,false\n");

            Assert.Single(sheet.Values);
            Assert.False(sheet.IsItemMarketRegistrable(1));
            Assert.False(sheet.IsItemSynthesizeMaterial(1));
        }

        [Fact]
        public void DuplicatedKeyMergesRegardlessOfOrder()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,false,false\n1,true,\n");

            Assert.Single(sheet.Values);
            Assert.False(sheet.IsItemMarketRegistrable(1));
            Assert.False(sheet.IsItemSynthesizeMaterial(1));
        }

        [Fact]
        public void DuplicatedKeyKeepsSpecifiedValueOverUnspecified()
        {
            var sheet = new RestrictionSheet();
            sheet.Set($"{Header}1,,\n1,false,\n");

            Assert.Single(sheet.Values);
            Assert.False(sheet.IsItemMarketRegistrable(1));
        }

        [Fact]
        public void CommentedLinesAreIgnored()
        {
            var csv = Header +
                      "_a commented row,false,false\n" +
                      ",another commented row,\n" +
                      "1,false,\n";

            RestrictionSheet.ValidateCsv(csv);

            var sheet = new RestrictionSheet();
            sheet.Set(csv);
            Assert.Single(sheet.Values);
            Assert.False(sheet.IsItemMarketRegistrable(1));
        }

        [Theory]
        // A "_" prefixed column is dropped from every line, so the policy cells shift left.
        [InlineData("key,_memo,market_registrable,synthesize_material\n10,Costume A,false,\n", false, true)]
        [InlineData("key,market_registrable,_memo,synthesize_material\n10,false,Costume A,\n", false, true)]
        [InlineData("key,market_registrable,synthesize_material,_memo\n10,false,,Costume A\n", false, true)]
        [InlineData("key,_memo,market_registrable,synthesize_material\n10,Costume A,,false\n", true, false)]
        public void ValidatorAgreesWithRuntimeOnDroppedColumns(
            string csv,
            bool marketRegistrable,
            bool synthesizeMaterial)
        {
            RestrictionSheet.ValidateCsv(csv);

            var sheet = new RestrictionSheet();
            sheet.Set(csv);
            Assert.Equal(marketRegistrable, sheet.IsItemMarketRegistrable(10));
            Assert.Equal(synthesizeMaterial, sheet.IsItemSynthesizeMaterial(10));
        }

        [Theory]
        [InlineData("1,false,")]
        [InlineData("1,,false")]
        [InlineData("1,false,false")]
        [InlineData("2147483647,false,")]
        [InlineData("CRYSTAL,false,")]
        [InlineData("RUNESTONE_FENRIR1,false,")]
        [InlineData("Item_NT_600202,false,")]
        [InlineData("Mead,false,")]
        public void ValidateCsv(string row) => RestrictionSheet.ValidateCsv($"{Header}{row}\n");

        [Theory]
        [InlineData(" ,false,")] // no key
        [InlineData("-1,false,")] // negative id
        [InlineData("0,false,")] // zero id
        [InlineData("010660004,false,")] // ToKey() never renders a leading zero
        [InlineData("1 2,false,")] // not a number, not a ticker
        [InlineData("crystal,false,")] // lookup is case sensitive
        [InlineData("СRYSTAL,false,")] // leading character is Cyrillic Es
        [InlineData("1,flase,")] // unparsable policy
        [InlineData("1,,")] // no policy at all
        [InlineData("1,false,\n1,,false")] // duplicated key
        [InlineData("CRYSTAL,,false")] // synthesis does not apply to a currency
        public void ValidateCsv_Throws(string body) =>
            Assert.Throws<SheetRowValidateException>(
                () => RestrictionSheet.ValidateCsv($"{Header}{body}\n"));

        [Theory]
        [InlineData("key,synthesize_material,market_registrable\n1,false,\n")] // swapped columns
        [InlineData("1,false,\n")] // header omitted
        [InlineData("key,market_registrable\n1,false\n")] // column missing
        [InlineData("key,market_registrable,synthesize_material,extra\n1,false,,\n")] // extra column
        [InlineData("")] // nothing at all
        public void ValidateCsv_ThrowsOnBadHeader(string csv) =>
            Assert.Throws<SheetRowValidateException>(() => RestrictionSheet.ValidateCsv(csv));

        private static RestrictionSheet Shipped()
        {
            Assert.True(TableSheetsImporter.TryGetCsv(nameof(RestrictionSheet), out var csv));
            var sheet = new RestrictionSheet();
            sheet.Set(csv);
            return sheet;
        }
    }
}
