namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Model.State;
    using Xunit;

    public class CollectionStateTest
    {
        [Fact]
        public void Bencoded()
        {
            var state = new CollectionState
            {
                Ids = new SortedSet<int>
                {
                    1,
                    3,
                    1,
                    2,
                    2,
                },
            };

            var serialized = state.Bencoded;
            var expected = List.Empty
                .Add(List.Empty.Add(1).Add(2).Add(3));
            Assert.Equal(expected, serialized);

            var deserialized = new CollectionState(serialized);
            Assert.Equal(new SortedSet<int> { 1, 2, 3, }, deserialized.Ids);
        }
    }
}
