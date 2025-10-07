namespace Lib9c.Tests.Module
{
    using System.Collections.Generic;
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class CollectionModuleTest
    {
        [Fact]
        public void CollectionState()
        {
            IWorld states = new World(MockUtil.MockModernWorldState);
            var address = new PrivateKey().Address;
            Assert.Throws<FailedLoadStateException>(() => states.GetCollectionState(address));
            Assert.False(states.TryGetCollectionState(address, out _));

            var state = new CollectionState
            {
                Ids = new SortedSet<int>
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
            IWorld states = new World(MockUtil.MockModernWorldState);
            var address = new PrivateKey().Address;
            var address2 = new PrivateKey().Address;
            var addresses = new[] { address, address2, };
            var result = states.GetCollectionStates(addresses);
            Assert.Empty(result);

            var state = new CollectionState
            {
                Ids = new SortedSet<int>
                {
                    1,
                },
            };
            states = states.SetCollectionState(address, state);
            result = states.GetCollectionStates(addresses);
            Assert.Contains(address, result.Keys);
            Assert.Equal(state.Ids, result[address].Ids);
            Assert.DoesNotContain(address2, result.Keys);
        }
    }
}
