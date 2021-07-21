namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public class StatOptionTest
    {
        private static readonly StatType[] StatTypes = Enum.GetValues(typeof(StatType))
            .Cast<StatType>()
            .Where(e => e != StatType.NONE)
            .ToArray();

        [Fact]
        public void Serialize()
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            foreach (var grade in Enumerable.Range(1, 10))
            {
                foreach (var statType in StatTypes)
                {
                    var statValue = (decimal)random.NextDouble() * 1000m;
                    var option = new StatOption(grade, statType, statValue);
                    var serialized = option.Serialize();
                    var deserialized = new StatOption(serialized);
                    Assert.Equal(option.Grade, deserialized.Grade);
                    Assert.Equal(option.StatType, deserialized.StatType);
                    Assert.Equal(option.statValue, deserialized.statValue);
                }
            }
        }

        [Theory]
        [InlineData(.1, 1, 1.1)]
        [InlineData(.9, 1, 1.9)]
        public void Enhance(decimal ratio, decimal from, decimal to)
        {
            var option = new StatOption(default, default, from);
            option.Enhance(ratio);
            Assert.Equal(to, option.statValue);
        }
    }
}
