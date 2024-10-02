#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using Nekoyume.ValidatorDelegation;
using Xunit;

public class ConstantTest
{
    [Fact]
    public void StaticPropertyTest()
    {
        Assert.True(ValidatorDelegatee.ValidatorUnbondingPeriod > 0);
    }
}
