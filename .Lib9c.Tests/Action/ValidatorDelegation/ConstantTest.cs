#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using Lib9c.ValidatorDelegation;
using Xunit;

public class ConstantTest
{
    [Fact(Skip = "Allow after positive unbonding period")]
    public void StaticPropertyTest()
    {
        Assert.True(ValidatorDelegatee.MaxCommissionPercentage < int.MaxValue);
        Assert.True(ValidatorDelegatee.MaxCommissionPercentage >= 0);
    }
}
