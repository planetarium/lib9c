namespace Lib9c.Tests.VersionedStates
{
    using Nekoyume.VersionedStates;
    using Xunit;

    public class NonVersionedStateTest
    {
        [Fact]
        public void Serialize_As_INonVersionedState()
        {
            INonVersionedState nvs = new NonVersionedStateImpl();
            var serialized = nvs.Serialize();
            var staticSerialized = INonVersionedState.Serialize(nvs);
            Assert.Equal(serialized, staticSerialized);
        }

        [Fact]
        public void Serialize_And_TryDeconstruct_As_INonVersionedStateImpl()
        {
            INonVersionedStateImpl nvsi = new NonVersionedStateImpl();
            var serialized = nvsi.Serialize();
            var staticSerialized = INonVersionedState.Serialize(nvsi);
            Assert.Equal(serialized, staticSerialized);
            Assert.True(INonVersionedStateImpl.TryDeconstruct(
                serialized,
                out var value));
            Assert.Equal(nvsi.Value, value);
        }

        [Fact]
        public void Serialize_And_TryDeconstruct_As_NonVersionedStateImpl()
        {
            var obj = new NonVersionedStateImpl();
            var nvsi = (INonVersionedStateImpl)obj;
            var serialized = obj.Serialize();
            var staticSerialized = INonVersionedState.Serialize(obj);
            Assert.Equal(serialized, staticSerialized);
            Assert.True(INonVersionedStateImpl.TryDeconstruct(
                serialized,
                out var value));
            Assert.Equal(nvsi.Value, value);
        }

        [Fact]
        public void Serialize_And_Create()
        {
            var obj = new NonVersionedStateImpl();
            var serialized = obj.Serialize();
            var created = NonVersionedStateImplFactory.Create(serialized);
            Assert.Equal(obj.Value, created.Value);
        }
    }
}
