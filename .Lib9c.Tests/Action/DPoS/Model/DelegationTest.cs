namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class DelegationTest : PoSTest
    {
        private readonly Delegation _delegation;

        public DelegationTest()
        {
            _delegation = new Delegation(CreateAddress(), CreateAddress());
        }

        [Fact]
        public void MarshallingTest()
        {
            Delegation newDelegation
                = new Delegation(_delegation.Serialize());
            Assert.Equal(_delegation, newDelegation);
        }
    }
}
