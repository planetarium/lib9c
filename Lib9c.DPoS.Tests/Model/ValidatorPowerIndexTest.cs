using Lib9c.DPoS.Model;
using Xunit;

namespace Lib9c.DPoS.Tests.Model
{
    public class ValidatorPowerIndexTest : PoSTest
    {
        private readonly ValidatorPowerIndex _validatorPowerIndex;

        public ValidatorPowerIndexTest()
        {
            _validatorPowerIndex = new ValidatorPowerIndex();
        }

        [Fact]
        public void MarshallingTest()
        {
            ValidatorPowerIndex newValidatorPowerIndex = new ValidatorPowerIndex(
                _validatorPowerIndex.Serialize());
            Assert.Equal(_validatorPowerIndex.Index, newValidatorPowerIndex.Index);
        }
    }
}
