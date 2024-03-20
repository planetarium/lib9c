using Lib9c.DPoS.Model;
using Xunit;

namespace Lib9c.DPoS.Tests.Model
{
    public class UndelegationTest : PoSTest
    {
        private readonly Undelegation _undelegation;

        public UndelegationTest()
        {
            _undelegation = new Undelegation(CreateAddress(), CreateAddress());
        }

        [Fact]
        public void MarshallingTest()
        {
            Undelegation newUndelegationInfo
                = new Undelegation(_undelegation.Serialize());
            Assert.Equal(_undelegation, newUndelegationInfo);
        }
    }
}
