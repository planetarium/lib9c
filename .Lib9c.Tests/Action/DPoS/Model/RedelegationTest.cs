namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

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
