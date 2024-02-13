namespace Lib9c.Tests.Module
{
    using System.Collections.Generic;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class CollectionModuleTest
    {
        [Fact]
        public void CollectionState()
        {
            IWorld states = new World(new MockWorldState());
            var address = new PrivateKey().Address;
            Assert.Throws<FailedLoadStateException>(() => states.GetCollectionState(address));
            Assert.False(states.TryGetCollectionState(address, out _));

            var state = new CollectionState
            {
                Ids = new List<int>
                {
                    1,
                },
            };
            states = states.SetCollectionState(address, state);
            var result = states.GetCollectionState(address);
            Assert.Equal(state.Ids, result.Ids);

            Assert.True(states.TryGetCollectionState(address, out var result2));
            Assert.Equal(state.Ids, result2.Ids);
        }

        [Fact]
        public void CollectionStates()
        {
            IWorld states = new World(new MockWorldState());
            var address = new PrivateKey().Address;
            var address2 = new PrivateKey().Address;
            var addresses = new[] { address, address2 };
            var result = states.GetCollectionStates(addresses);
            Assert.Equal(addresses.Length, result.Count);
            Assert.All(result, Assert.Null);

            var state = new CollectionState
            {
                Ids = new List<int>
                {
                    1,
                },
            };
            states = states.SetCollectionState(address, state);
            result = states.GetCollectionStates(addresses);
            for (int i = 0; i < addresses.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        Assert.NotNull(result[i]);
                        Assert.Equal(state.Ids, result[i].Ids);
                        break;
                    case 1:
                        Assert.Null(result[i]);
                        break;
                }
            }
        }
    }
}
