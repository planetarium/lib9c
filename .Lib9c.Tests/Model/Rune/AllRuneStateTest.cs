namespace Lib9c.Tests.Model.Rune
{
    using Bencodex.Types;
    using Lib9c.Model.State;
    using Lib9c.Tests.Action;
    using Xunit;

    public class AllRuneStateTest
    {
        [Theory]
        [InlineData(new[] { 10001, })]
        [InlineData(new[] { 10001, 10002, })]
        public void Serialize(int[] runeIds)
        {
            var levelOffset = new TestRandom().Next(0, 100);
            var allRuneState = new AllRuneState();
            foreach (var runeId in runeIds)
            {
                allRuneState.AddRuneState(runeId, runeId % 10 + levelOffset);
            }

            var serialized = allRuneState.Serialize();
            Assert.IsType<List>(serialized);
            Assert.Equal(runeIds.Length, ((List)serialized).Count);
            foreach (var s in (List)serialized)
            {
                Assert.IsType<List>(s);
            }

            var deserialized = new AllRuneState((List)serialized);

            Assert.Equal(deserialized.Runes.Count, runeIds.Length);
            foreach (var runeId in runeIds)
            {
                var expectedLevel = runeId % 10 + levelOffset;
                Assert.Equal(expectedLevel, deserialized.GetRuneState(runeId).Level);
            }
        }
    }
}
