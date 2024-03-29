namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class ValidatorSetTest : PoSTest
    {
        private readonly ValidatorSet _validatorSet;

        public ValidatorSetTest()
        {
            _validatorSet = new ValidatorSet();
        }

        [Fact]
        public void MarshallingTest()
        {
            ValidatorSet newValidatorSet = new ValidatorSet(
                _validatorSet.Serialize());
            Assert.Equal(
                _validatorSet.Set,
                newValidatorSet.Set);
        }
    }
}
