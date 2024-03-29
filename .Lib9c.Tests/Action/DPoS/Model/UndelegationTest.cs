namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

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
