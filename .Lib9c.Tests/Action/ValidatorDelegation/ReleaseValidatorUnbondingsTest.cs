#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using Nekoyume.Action.ValidatorDelegation;
    using Xunit;

    public class ReleaseValidatorUnbondingsTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new ReleaseValidatorUnbondings();
            var plainValue = action.PlainValue;

            var deserialized = new ReleaseValidatorUnbondings();
            deserialized.LoadPlainValue(plainValue);
        }
    }
}
