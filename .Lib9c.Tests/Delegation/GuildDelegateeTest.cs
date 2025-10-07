namespace Lib9c.Tests.Delegation;

using Lib9c.Delegation;
using Lib9c.Model.Guild;
using Lib9c.Module.Guild;
using Lib9c.Module.ValidatorDelegation;
using Lib9c.Tests.Action;
using Lib9c.ValidatorDelegation;
using Libplanet.Crypto;
using Xunit;

public class GuildDelegateeTest : DelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var world = World;
        var validatorKey = new PrivateKey();
        var actionContext = new ActionContext { };
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegatee = validatorRepository.CreateDelegatee(
            validatorKey.PublicKey, commissionPercentage: 10);

        world = validatorRepository.World;
        var repository1 = new GuildRepository(world, actionContext);
        var delegatee1 = repository1.CreateDelegatee(validatorKey.Address);
        IDelegatee delegateeInterface1 = delegatee1;

        world = repository1.World;
        var repository2 = new GuildRepository(world, actionContext);
        var delegatee2 = repository2.GetDelegatee(validatorKey.Address);
        IDelegatee delegateeInterface2 = delegatee2;

        Assert.Equal(validatorDelegatee.Address, delegatee1.Address);

        Assert.Equal(delegatee1, delegatee2);
        Assert.Equal(delegateeInterface1, delegateeInterface2);
        Assert.Equal(delegatee1, delegateeInterface2);
        Assert.Equal(delegateeInterface1, delegatee2);

        Assert.True(delegatee1.Equals(delegatee2));
        Assert.True(delegateeInterface1.Equals(delegateeInterface2));
        Assert.True(delegatee1.Equals(delegateeInterface2));
        Assert.True(delegateeInterface1.Equals(delegatee2));
    }
}
