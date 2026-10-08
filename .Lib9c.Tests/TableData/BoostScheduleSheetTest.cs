namespace Lib9c.Tests.TableData
{
    using Nekoyume.TableData;
    using Xunit;

    public class BoostScheduleSheetTest
    {
        private const string Header = "id,target,target_id,op,value,start_block,end_block\n";
        private const string Target = BoostScheduleSheet.Targets.EquipmentSummonGuarantee;

        [Fact]
        public void ShippedCsvPassesItsOwnValidator()
        {
            Assert.True(TableSheetsImporter.TryGetCsv(nameof(BoostScheduleSheet), out var csv));
            BoostScheduleSheet.ValidateCsv(csv);
        }

        [Fact]
        public void ShippedCsvSchedulesNothing()
        {
            // The sheet ships empty: an adjustment is an operational decision made by patching a
            // chain, never something a release carries in.
            Assert.True(TableSheetsImporter.TryGetCsv(nameof(BoostScheduleSheet), out var csv));
            var sheet = new BoostScheduleSheet();
            sheet.Set(csv);
            Assert.Empty(sheet);
        }

        [Fact]
        public void ParsesRow()
        {
            var sheet = Parse($"1,{Target},10001,MUL,1.5,100,200\n");

            var row = sheet[1];
            Assert.True(row.IsValid);
            Assert.Equal(Target, row.Target);
            Assert.Equal(10001, row.TargetId);
            Assert.Equal(BoostScheduleSheet.Operation.Mul, row.Op);
            Assert.Equal(1.5m, row.Value);
            Assert.Equal(100, row.StartBlockIndex);
            Assert.Equal(200, row.EndBlockIndex);
        }

        [Fact]
        public void MemoColumnIsDropped()
        {
            var sheet = new BoostScheduleSheet();
            sheet.Set(
                "id,target,target_id,op,value,start_block,end_block,_memo\n" +
                $"1,{Target},10001,ADD,1,100,200,anniversary\n");

            Assert.True(sheet[1].IsValid);
        }

        [Fact]
        public void AnyTargetIdMatchesEveryId()
        {
            var sheet = Parse($"1,{Target},*,ADD,1,100,200\n");

            Assert.Null(sheet[1].TargetId);
            Assert.NotNull(sheet.FindActive(Target, 10001, 150));
            Assert.NotNull(sheet.FindActive(Target, 99999, 150));
        }

        [Theory]
        [InlineData("1,,10001,MUL,1.5,100,200")] // empty target
        [InlineData("1,TARGET,abc,MUL,1.5,100,200")] // target_id not a number
        [InlineData("1,TARGET,0,MUL,1.5,100,200")] // target_id not positive
        [InlineData("1,TARGET,10001,POW,1.5,100,200")] // unknown op
        [InlineData("1,TARGET,10001,mul,1.5,100,200")] // op is case sensitive
        [InlineData("1,TARGET,10001,MUL,1.5x,100,200")] // value not a number
        [InlineData("1,TARGET,10001,MUL,0,100,200")] // MUL must be positive
        [InlineData("1,TARGET,10001,MUL,-1,100,200")]
        [InlineData("1,TARGET,10001,MUL,3000000000,100,200")] // beyond MaxAbsoluteValue
        [InlineData("1,TARGET,10001,ADD,0.5,100,200")] // ADD must be integral
        [InlineData("1,TARGET,10001,MUL,1.5,-1,200")] // negative start
        [InlineData("1,TARGET,10001,MUL,1.5,200,200")] // empty range
        [InlineData("1,TARGET,10001,MUL,1.5,300,200")] // reversed range
        [InlineData("1,TARGET,10001,MUL,1.5,100")] // missing cell
        public void MalformedRowIsKeptInvalidInsteadOfThrowing(string line)
        {
            // Reading must never throw: every action that reads the sheet would fail with it.
            var sheet = Parse(line.Replace("TARGET", Target) + "\n");

            Assert.False(sheet[1].IsValid);
            Assert.Null(sheet.FindActive(Target, 10001, 150));
        }

        [Fact]
        public void RowWithoutUsableIdIsDropped()
        {
            var sheet = Parse(
                $"abc,{Target},10001,ADD,1,100,200\n" +
                $"0,{Target},10001,ADD,1,100,200\n" +
                $"2,{Target},10001,ADD,2,100,200\n");

            Assert.Single(sheet);
            Assert.True(sheet.ContainsKey(2));
        }

        [Fact]
        public void DuplicatedIdKeepsTheFirstRowInsteadOfThrowing()
        {
            var sheet = Parse(
                $"1,{Target},10001,ADD,1,100,200\n" +
                $"1,{Target},10001,ADD,5,100,200\n");

            Assert.Single(sheet);
            Assert.Equal(1m, sheet[1].Value);
        }

        [Theory]
        [InlineData(99, false)]
        [InlineData(100, true)] // start is inclusive
        [InlineData(199, true)]
        [InlineData(200, false)] // end is exclusive
        public void RangeIsHalfOpen(long blockIndex, bool expected)
        {
            var sheet = Parse($"1,{Target},10001,ADD,1,100,200\n");

            Assert.Equal(expected, sheet.FindActive(Target, 10001, blockIndex) is not null);
        }

        [Fact]
        public void MatchesTargetAndTargetIdExactly()
        {
            var sheet = Parse($"1,{Target},10001,ADD,1,100,200\n");

            Assert.Null(sheet.FindActive(BoostScheduleSheet.Targets.RuneSummonGuarantee, 10001, 150));
            Assert.Null(sheet.FindActive(Target.ToLowerInvariant(), 10001, 150));
            Assert.Null(sheet.FindActive(Target, 10002, 150));
        }

        [Fact]
        public void TargetNoVersionAppliesIsStillReadable()
        {
            // A node that predates a target must read the sheet as usual and just find no match.
            var sheet = Parse(
                "1,SOME_FUTURE_TARGET,1,MUL,2,100,200\n" +
                $"2,{Target},10001,ADD,1,100,200\n");

            Assert.True(sheet[1].IsValid);
            Assert.Equal(2, sheet.FindActive(Target, 10001, 150).Id);
        }

        [Fact]
        public void LowestIdWinsAndRowsNeverStack()
        {
            var sheet = Parse(
                $"7,{Target},*,ADD,5,100,200\n" +
                $"3,{Target},10001,ADD,1,150,250\n");

            Assert.Equal(7, sheet.FindActive(Target, 10001, 120).Id);
            Assert.Equal(3, sheet.FindActive(Target, 10001, 160).Id);
            Assert.Equal(3, sheet.FindActive(Target, 10001, 220).Id);
            Assert.Equal(7, sheet.FindActive(Target, 10002, 160).Id);
        }

        [Fact]
        public void InvalidRowDoesNotShadowAValidOne()
        {
            var sheet = Parse(
                $"1,{Target},10001,MUL,0,100,200\n" +
                $"2,{Target},10001,ADD,1,100,200\n");

            Assert.Equal(2, sheet.FindActive(Target, 10001, 150).Id);
        }

        [Fact]
        public void EmptySheetFindsNothing()
        {
            Assert.Null(new BoostScheduleSheet().FindActive(Target, 10001, 150));
            Assert.Null(Parse(string.Empty).FindActive(Target, 10001, 150));
        }

        [Theory]
        [InlineData("MUL", "1.5", 1, 1)] // rounds down: a small base can stay put
        [InlineData("MUL", "1.5", 2, 3)]
        [InlineData("MUL", "1.5", 3, 4)]
        [InlineData("MUL", "2", 3, 6)]
        [InlineData("MUL", "0.5", 3, 1)]
        [InlineData("ADD", "1", 1, 2)]
        [InlineData("ADD", "-5", 3, 0)] // clamped at zero
        [InlineData("MUL", "2147483647", 2, int.MaxValue)] // clamped at int.MaxValue
        [InlineData("ADD", "2147483647", int.MaxValue, int.MaxValue)]
        public void Apply(string op, string value, int baseValue, int expected)
        {
            var sheet = Parse($"1,{Target},10001,{op},{value},100,200\n");

            Assert.Equal(expected, sheet[1].Apply(baseValue));
        }

        [Fact]
        public void ValidateCsvAcceptsWellFormedRows()
        {
            BoostScheduleSheet.ValidateCsv(
                "id,target,target_id,op,value,start_block,end_block,_memo\n" +
                $"1,{Target},10001,MUL,1.5,100,200,memo\n" +
                $"2,{Target},*,ADD,-1,0,1,\n" +
                "3,SOME_FUTURE_TARGET,1,MUL,2,100,200,\n");
        }

        [Fact]
        public void ValidateCsvAcceptsHeaderOnly()
        {
            BoostScheduleSheet.ValidateCsv(Header);
        }

        [Fact]
        public void ValidateCsvSkipsBlankLinesASpreadsheetExportLeaves()
        {
            BoostScheduleSheet.ValidateCsv(
                Header +
                ",,,,,,\n" +
                $"1,{Target},10001,ADD,1,100,200\n" +
                "\n");
        }

        [Theory]
        [InlineData(",EQUIPMENT_SUMMON_GUARANTEE,10001,ADD,1,100,200\n")] // id left out
        [InlineData("_1,EQUIPMENT_SUMMON_GUARANTEE,10001,ADD,1,100,200\n")]
        public void ValidateCsvRejectsRowsTheSheetWouldSkip(string row)
        {
            // Sheet.Set drops these lines without a word, so accepting them would land an
            // adjustment that never applies.
            Assert.Throws<SheetRowValidateException>(
                () => BoostScheduleSheet.ValidateCsv(Header + row));
        }

        [Fact]
        public void ValidateCsvRejectsADroppedColumnInFront()
        {
            // With "_memo" first, every row whose memo is empty starts with "," and is skipped.
            Assert.Throws<SheetRowValidateException>(
                () => BoostScheduleSheet.ValidateCsv(
                    "_memo,id,target,target_id,op,value,start_block,end_block\n" +
                    $",1,{Target},10001,ADD,1,100,200\n"));
        }

        [Theory]
        [InlineData("id,target,target_id,op,value,end_block,start_block\n")] // column order
        [InlineData("id,target,op,value,start_block,end_block\n")] // missing column
        [InlineData(Header + "0,T,10001,ADD,1,100,200\n")] // id not positive
        [InlineData(Header + "abc,T,10001,ADD,1,100,200\n")] // id not a number
        [InlineData(Header + "1,T,10001,ADD,1,100,200\n1,T,10001,ADD,1,100,200\n")] // duplicated id
        [InlineData(Header + "1,,10001,ADD,1,100,200\n")] // empty target
        [InlineData(Header + "1,T,x,ADD,1,100,200\n")] // target_id
        [InlineData(Header + "1,T,10001,SET,1,100,200\n")] // op
        [InlineData(Header + "1,T,10001,MUL,0,100,200\n")] // value
        [InlineData(Header + "1,T,10001,ADD,1.5,100,200\n")] // value
        [InlineData(Header + "1,T,10001,ADD,1,200,100\n")] // range
        [InlineData(Header + "1,T,10001,ADD,1,100\n")] // missing cell
        [InlineData(Header + "1,T,10001,ADD,1,100,200,extra\n")] // extra cell
        public void ValidateCsvRejectsMalformedSchedule(string csv)
        {
            Assert.Throws<SheetRowValidateException>(() => BoostScheduleSheet.ValidateCsv(csv));
        }

        private static BoostScheduleSheet Parse(string rows)
        {
            var sheet = new BoostScheduleSheet();
            sheet.Set(Header + rows);
            return sheet;
        }
    }
}
