namespace Lib9c.Tests.Delegation;

using Lib9c.Tests.Action;
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.Model.Guild;
using Xunit;

public class GuildDelegatorTest : DelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorAddress = new PrivateKey().Address;
        world = EnsureToCreateDelegatee(world, validatorKey);

        var repository1 = new GuildRepository(world, new ActionContext());
        var delegator1 = repository1.GetDelegator(delegatorAddress);
        IDelegator delegatorInterface1 = delegator1;

        var repository2 = new GuildRepository(world, new ActionContext());
        var delegator2 = repository2.GetDelegator(delegatorAddress);
        IDelegator delegatorInterface2 = delegator2;

        Assert.Equal(delegator1, delegator2);
        Assert.Equal(delegatorInterface1, delegatorInterface2);
        Assert.Equal(delegator1, delegatorInterface2);
        Assert.Equal(delegatorInterface1, delegator2);

        Assert.True(delegator1.Equals(delegator2));
        Assert.True(delegatorInterface1.Equals(delegatorInterface2));
        Assert.True(delegator1.Equals(delegatorInterface2));
        Assert.True(delegatorInterface1.Equals(delegator2));
    }
}
