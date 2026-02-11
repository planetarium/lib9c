namespace Lib9c.Tests.Helper;

using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Xunit;

public class SynthesizeSimulatorGradeTest
{
    [Fact]
    public void GetTargetGrade_Grade_Mythic_ShouldUpgradeToTranscendent()
    {
        Assert.Equal(Grade.Transcendent, SynthesizeSimulator.GetTargetGrade(Grade.Mythic));
    }

    [Fact]
    public void GetTargetGrade_Grade_Transcendent_ShouldStayTranscendent()
    {
        Assert.Equal(Grade.Transcendent, SynthesizeSimulator.GetTargetGrade(Grade.Transcendent));
    }

    [Theory]
    [InlineData(7, 8)]
    [InlineData(8, 8)]
    public void GetTargetGrade_Int_ShouldUpgradeOrCap(int gradeId, int expected)
    {
        Assert.Equal(expected, SynthesizeSimulator.GetTargetGrade(gradeId));
    }
}
