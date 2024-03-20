using Lib9c.DPoS.Model;
using Xunit;

namespace Lib9c.DPoS.Tests.Model
{
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
