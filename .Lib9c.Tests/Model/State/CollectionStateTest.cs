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
        public void Derive()
        {
            var address = new PrivateKey().Address;
            Assert.Equal(address.Derive(nameof(CollectionState)), CollectionState.Derive(address));
        }

        [Fact]
        public void Serialize()
        {
            var address = new PrivateKey().Address;
            var state = new CollectionState
            {
                Address = address,
                Ids = new List<int>
                {
                    1,
                    2,
                },
            };

            var serialized = (List)state.SerializeList();
            var expected = List.Empty
                .Add(address.Serialize())
                .Add(List.Empty.Add(1).Add(2));
            Assert.Equal(expected, serialized);

            var deserialized = new CollectionState(serialized);
            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.Ids, deserialized.Ids);
        }
    }
}
