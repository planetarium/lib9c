namespace Lib9c.Tests.Action
{
    using Xunit;

    public class DamageHelperTest
    {
        [Theory]
        [InlineData(long.MaxValue, 0.5)]
        [InlineData(4000L, 0.6)]
        [InlineData(long.MinValue, 0)]
        public void GetDamageReductionRate(long drr, decimal expected)
        {
            Assert.Equal(expected, Nekoyume.Battle.DamageHelper.GetDamageReductionRate(drr));
        }
    }
}
