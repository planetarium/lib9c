namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class CollectionStateTest
    {
        [Fact]
        public void Bencoded()
        {
            var state = new CollectionState
            {
                Ids = new List<int>
                {
                    1,
                    2,
                    1,
                    2,
                },
            };

            var serialized = state.Bencoded;
            var expected = List.Empty
                .Add(List.Empty.Add(1).Add(2));
            Assert.Equal(expected, serialized);

            var deserialized = new CollectionState(serialized);
            Assert.Equal(new List<int> { 1, 2, }, deserialized.Ids);
        }
    }
}
