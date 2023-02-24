namespace Lib9c.Tests.VersionedStates
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;
    using Xunit;

    public class VersionedStateTest
    {
        [Fact]
        public void Serialize_And_Deconstruct_As_IVersionedState()
        {
            IVersionedState vs = new VersionedStateImpl();
            Assert.Equal(IVersionedStateImpl.MonikerCache, vs.Moniker);
            Assert.Equal(IVersionedStateImpl.VersionCache, vs.Version);
            var serialized = vs.Serialize();
            var staticSerialized = IVersionedState.Serialize(
                vs.Moniker,
                vs.Version,
                vs.Data);
            Assert.Equal(serialized, staticSerialized);
            var (m, v, d) = IVersionedState.Deconstruct(serialized);
            Assert.Equal(vs.Moniker, m);
            Assert.Equal(vs.Version, v);
            Assert.Equal(vs.Data, d);
            Assert.True(IVersionedState.TryDeconstruct(
                serialized,
                out m,
                out v,
                out d));
            Assert.Equal(vs.Moniker, m);
            Assert.Equal(vs.Version, v);
            Assert.Equal(vs.Data, d);
        }

        [Fact]
        public void Serialize_And_Deconstruct_As_IVersionedStateImpl()
        {
            IVersionedStateImpl vsi = new VersionedStateImpl();
            Assert.Equal(IVersionedStateImpl.MonikerCache, vsi.Moniker);
            Assert.Equal(IVersionedStateImpl.VersionCache, vsi.Version);
            var serialized = vsi.Serialize();
            var staticSerialized = IVersionedState.Serialize(
                vsi.Moniker,
                vsi.Version,
                vsi.Data);
            Assert.Equal(serialized, staticSerialized);
            var (m, v, d) = IVersionedState.Deconstruct(serialized);
            Assert.Equal(vsi.Moniker, m);
            Assert.Equal(vsi.Version, v);
            Assert.Equal(vsi.Data, d);
            Assert.True(IVersionedStateImpl.TryDeconstruct(
                serialized,
                out m,
                out v,
                out var value1,
                out var value2));
            Assert.Equal(vsi.Moniker, m);
            Assert.Equal(vsi.Version, v);
            Assert.Equal(vsi.Value1?.Serialize() ?? Null.Value, value1);
            Assert.Equal(vsi.Value2, value2);
        }

        [Fact]
        public void Serialize_And_Deconstruct_As_VersionedStateImpl()
        {
            var obj = new VersionedStateImpl();
            var vsi = (IVersionedStateImpl)obj;
            Assert.Equal(IVersionedStateImpl.MonikerCache, vsi.Moniker);
            Assert.Equal(IVersionedStateImpl.VersionCache, vsi.Version);
            var serialized = obj.Serialize();
            var staticSerialized = IVersionedState.Serialize(
                vsi.Moniker,
                vsi.Version,
                vsi.Data);
            Assert.Equal(serialized, staticSerialized);
            var (m, v, d) = IVersionedState.Deconstruct(serialized);
            Assert.Equal(vsi.Moniker, m);
            Assert.Equal(vsi.Version, v);
            Assert.Equal(vsi.Data, d);
            Assert.True(IVersionedStateImpl.TryDeconstruct(
                serialized,
                out m,
                out v,
                out var value1,
                out var value2));
            Assert.Equal(vsi.Moniker, m);
            Assert.Equal(vsi.Version, v);
            Assert.Equal(obj.Value1?.Serialize() ?? Null.Value, value1);
            Assert.Equal(obj.Value2, value2);
        }

        [Fact]
        public void Serialize_And_Create()
        {
            var obj = new VersionedStateImpl();
            var serialized = obj.Serialize();
            var created = VersionedStateImplFactory.Create(serialized);
            var vsi = (IVersionedStateImpl)obj;
            var createdVsi = (IVersionedStateImpl)created;
            Assert.Equal(vsi.Moniker, createdVsi.Moniker);
            Assert.Equal(vsi.Version, createdVsi.Version);
            Assert.Equal(obj.Value1, created.Value1);
            Assert.Equal(obj.Value2, created.Value2);
        }
    }
}
