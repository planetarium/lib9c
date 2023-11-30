using System.Collections.Immutable;
using Lib9c.PluginBase;
using Libplanet.RocksDBStore;
using Libplanet.Store.Trie;

namespace Libplanet.Extensions.PluginActionEvaluator
{
    public class PluginRocksDBKeyValueStore : IPluginKeyValueStore
    {
        private readonly RocksDBKeyValueStore _rocksDbKeyValueStore;

        public PluginRocksDBKeyValueStore(RocksDBKeyValueStore rocksDbKeyValueStore)
        {
            _rocksDbKeyValueStore = rocksDbKeyValueStore;
        }
        public byte[] Get(in ImmutableArray<byte> key) =>
            _rocksDbKeyValueStore.Get(new KeyBytes(key));

        public void Set(in ImmutableArray<byte> key, byte[] value) =>
            _rocksDbKeyValueStore.Set(new KeyBytes(key), value);

        public void Set(IDictionary<ImmutableArray<byte>, byte[]> values) =>
            _rocksDbKeyValueStore.Set(
                values.ToDictionary(kv =>
                    new KeyBytes(kv.Key), kv => kv.Value));

        public void Delete(in ImmutableArray<byte> key) =>
            _rocksDbKeyValueStore.Delete(new KeyBytes(key));

        public void Delete(IEnumerable<ImmutableArray<byte>> keys) =>
            _rocksDbKeyValueStore.Delete(
                keys.Select(key => new KeyBytes(key)));

        public bool Exists(in ImmutableArray<byte> key) =>
            _rocksDbKeyValueStore.Exists(new KeyBytes(key));

        public IEnumerable<ImmutableArray<byte>> ListKeys() =>
            _rocksDbKeyValueStore.ListKeys().Select(key => key.ByteArray);

        public void Dispose() =>
            _rocksDbKeyValueStore.Dispose();
    }
}
