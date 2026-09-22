namespace Lib9c.Tests.Helper;

using System;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Xunit;

public class SynthesizeSimulatorGradeTest
{
    private const string EquipmentItemSheetHeader =
        "id,_name,item_sub_type,grade,elemental_type,set_id,stat_type,stat_value,attack_range,spine_resource_path,exp\n";

    private const string TranscendentAuraRow =
        "10680000,Ymir Aura,Aura,8,Normal,0,ATK,10,0,10620001,1300000\n";

    private const string UltimateAuraRow =
        "10690000,Ymir Aura G9,Aura,9,Normal,0,ATK,10,0,10620001,1300000\n";

    [Fact]
    public void GetTargetGrade_Grade_Mythic_ShouldUpgradeToTranscendent()
    {
        Assert.Equal(Grade.Transcendent, SynthesizeSimulator.GetTargetGrade(Grade.Mythic));
    }

    /// <summary>
    /// The step up no longer stops at <see cref="Grade.Transcendent"/>: whether the next grade
    /// exists is a question the item sheets answer, not this method.
    /// </summary>
    [Fact]
    public void GetTargetGrade_Grade_Transcendent_ShouldUpgradeToUltimate()
    {
        Assert.Equal(Grade.Ultimate, SynthesizeSimulator.GetTargetGrade(Grade.Transcendent));
    }

    /// <summary>
    /// There is no cap written into the code, so the step above the highest grade the sheets
    /// currently carry is still one higher. Adding a grade is a sheet change, not a code change.
    /// </summary>
    [Theory]
    [InlineData(7, 8)]
    [InlineData(8, 9)]
    [InlineData(9, 10)]
    public void GetTargetGrade_Int_ShouldStepUpWithoutCap(int gradeId, int expected)
    {
        Assert.Equal(expected, SynthesizeSimulator.GetTargetGrade(gradeId));
    }

    /// <summary>
    /// A grade below the first one is not a grade, and asking for the step above it stays an
    /// error rather than becoming <see cref="Grade.Normal"/>.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetTargetGrade_Int_BelowFirstGrade_ShouldThrow(int gradeId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SynthesizeSimulator.GetTargetGrade(gradeId));
    }

    /// <summary>
    /// Having no cap is not the same as wrapping around. Arithmetic here is unchecked, so the step
    /// above <see cref="int.MaxValue"/> would silently be <see cref="int.MinValue"/> — a grade
    /// below the first one. It stays an error, as it was before the cap was removed.
    /// </summary>
    [Fact]
    public void GetTargetGrade_Int_AtMaxValue_ShouldThrowRatherThanWrap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SynthesizeSimulator.GetTargetGrade(int.MaxValue));
    }

    /// <summary>
    /// The cap lives in the sheets. While no grade above <see cref="Grade.Transcendent"/> is
    /// listed, synthesizing Transcendent material stays at Transcendent.
    /// </summary>
    [Fact]
    public void GetUpgradeGrade_CapsAtTheHighestGradeTheSheetCarries()
    {
        var sheet = new EquipmentItemSheet();
        sheet.Set(EquipmentItemSheetHeader + TranscendentAuraRow);

        Assert.Equal(
            Grade.Transcendent,
            SynthesizeSimulator.GetUpgradeGrade(
                Grade.Transcendent,
                ItemSubType.Aura,
                sheet));
    }

    /// <summary>
    /// ...and listing one opens it, with no code change. This is the whole mechanism by which a
    /// new grade arrives: a row in <see cref="EquipmentItemSheet"/>.
    /// </summary>
    [Fact]
    public void GetUpgradeGrade_ListingTheNextGradeOpensIt()
    {
        var sheet = new EquipmentItemSheet();
        sheet.Set(EquipmentItemSheetHeader + TranscendentAuraRow + UltimateAuraRow);

        Assert.Equal(
            Grade.Ultimate,
            SynthesizeSimulator.GetUpgradeGrade(
                Grade.Transcendent,
                ItemSubType.Aura,
                sheet));
    }
}
