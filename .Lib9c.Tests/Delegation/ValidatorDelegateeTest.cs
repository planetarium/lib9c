namespace Lib9c.Tests.Delegation;

using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Delegation;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ValidatorDelegateeTest : DelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var world = World;
        var validatorKey = new PrivateKey();
        var actionContext = new ActionContext { };
        var repository1 = new ValidatorRepository(world, actionContext);
        var delegatee1 = repository1.CreateDelegatee(
            validatorKey.PublicKey, commissionPercentage: 10);
        IDelegatee delegateeInterface1 = delegatee1;

        world = repository1.World;
        var repository2 = new ValidatorRepository(world, actionContext);
        var delegatee2 = repository2.GetDelegatee(validatorKey.Address);
        IDelegatee delegateeInterface2 = delegatee2;

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
