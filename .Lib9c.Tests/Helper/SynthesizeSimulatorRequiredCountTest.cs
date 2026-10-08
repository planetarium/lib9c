namespace Lib9c.Tests.Helper;

using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Xunit;

/// <summary>
/// <see cref="SynthesizeSimulator.GetRequiredCount"/>: how a
/// <see cref="BoostScheduleSheet.Targets.SynthesizeRequiredCount"/> row changes the number of
/// materials one synthesis consumes.
/// </summary>
public class SynthesizeSimulatorRequiredCountTest
{
    private const string Header = "id,target,target_id,op,value,start_block,end_block\n";

    private static readonly string Target = BoostScheduleSheet.Targets.SynthesizeRequiredCount;

    // The tiers planning asked for: G1-G4 x0.6, G5-G6 x0.7, G7 x0.8, nothing above.
    private static readonly string TieredCsv =
        Header +
        $"1,{Target},1~4,MUL,0.6,100,200\n" +
        $"2,{Target},5~6,MUL,0.7,100,200\n" +
        $"3,{Target},7,MUL,0.8,100,200\n";

    [Theory]
    [InlineData(1)]
    [InlineData(13)]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithoutABoostTheConfiguredCountIsKept(int requiredCount)
    {
        Assert.Equal(requiredCount, SynthesizeSimulator.GetRequiredCount(requiredCount, null));
    }

    [Theory]
    [InlineData("MUL,0.6", 13, 7)] // 7.8 rounds down
    [InlineData("MUL,0.6", 10, 6)]
    [InlineData("MUL,2", 9, 18)]
    [InlineData("ADD,-3", 11, 8)]
    [InlineData("ADD,5", 4, 9)]
    public void AppliesTheRow(string opAndValue, int requiredCount, int expected)
    {
        var row = Row($"1,{Target},*,{opAndValue},100,200");

        Assert.Equal(expected, SynthesizeSimulator.GetRequiredCount(requiredCount, row));
    }

    [Theory]
    [InlineData("MUL,0.6", 1)]
    [InlineData("MUL,0.1", 4)]
    [InlineData("ADD,-100", 4)]
    public void NeverDropsBelowOne(string opAndValue, int requiredCount)
    {
        var row = Row($"1,{Target},*,{opAndValue},100,200");

        Assert.Equal(1, SynthesizeSimulator.GetRequiredCount(requiredCount, row));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void LeavesANonPositiveCountAlone(int requiredCount)
    {
        var row = Row($"1,{Target},*,ADD,5,100,200");

        Assert.Equal(requiredCount, SynthesizeSimulator.GetRequiredCount(requiredCount, row));
    }

    /// <summary>
    /// The live <see cref="SynthesizeSheet"/> under the tiers planning asked for, one row per
    /// grade range, applied to every sub type of a grade alike.
    /// </summary>
    /// <param name="grade">The material grade id.</param>
    /// <param name="itemSubType">The material sub type.</param>
    /// <param name="expected">The count one synthesis consumes during the event.</param>
    [Theory]
    [InlineData(1, ItemSubType.Aura, 7)] // 13 x 0.6
    [InlineData(3, ItemSubType.FullCostume, 10)] // 18 x 0.6
    [InlineData(4, ItemSubType.Grimoire, 6)] // 11 x 0.6
    [InlineData(5, ItemSubType.Aura, 8)] // 12 x 0.7
    [InlineData(6, ItemSubType.FullCostume, 7)] // 11 x 0.7
    [InlineData(7, ItemSubType.Grimoire, 7)] // 9 x 0.8
    [InlineData(8, ItemSubType.Aura, 4)] // no row
    [InlineData(9, ItemSubType.Aura, 1)] // no row
    public void TiersByGradeRange(int grade, ItemSubType itemSubType, int expected)
    {
        var synthesizeSheet = new TableSheets(TableSheetsImporter.ImportSheets()).SynthesizeSheet;
        var boostScheduleSheet = new BoostScheduleSheet();
        boostScheduleSheet.Set(TieredCsv);

        var row = boostScheduleSheet.FindActive(Target, grade, 150);
        var requiredCount = synthesizeSheet[grade].RequiredCountDict[itemSubType].RequiredCount;

        Assert.Equal(expected, SynthesizeSimulator.GetRequiredCount(requiredCount, row));
    }

    private static BoostScheduleSheet.Row Row(string line)
    {
        var sheet = new BoostScheduleSheet();
        sheet.Set(Header + line + "\n");
        var row = Assert.Single(sheet.Values);
        Assert.True(row.IsValid);
        return row;
    }
}
