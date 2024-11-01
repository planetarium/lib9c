#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using Nekoyume.ValidatorDelegation;
using Xunit;

public class ConstantTest
{
    [Fact]
    public void StaticPropertyTest()
    {
        Assert.True(ValidatorSettings.ValidatorUnbondingPeriod > 0);
        Assert.True(ValidatorSettings.MaxCommissionPercentage < int.MaxValue);
        Assert.True(ValidatorSettings.MaxCommissionPercentage >= 0);
    }
}
