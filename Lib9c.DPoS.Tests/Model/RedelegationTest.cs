using Lib9c.DPoS.Model;
using Xunit;

namespace Lib9c.DPoS.Tests.Model
{
    public class RedelegationTest : PoSTest
    {
        private readonly Redelegation _redelegation;

        public RedelegationTest()
        {
            _redelegation = new Redelegation(
                CreateAddress(), CreateAddress(), CreateAddress());
        }

        [Fact]
        public void MarshallingTest()
        {
            Redelegation newRedelegationInfo
                = new Redelegation(_redelegation.Serialize());
            Assert.Equal(_redelegation, newRedelegationInfo);
        }
    }
}
