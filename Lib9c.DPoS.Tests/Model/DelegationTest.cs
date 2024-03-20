using Lib9c.DPoS.Model;
using Xunit;

namespace Lib9c.DPoS.Tests.Model
{
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
