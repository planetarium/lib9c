namespace Lib9c.Tests
{
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Store.Trie;

    public static class AccountExtensions
    {
        private static readonly byte[] _conversionTable =
        {
            48,  // '0'
            49,  // '1'
            50,  // '2'
            51,  // '3'
            52,  // '4'
            53,  // '5'
            54,  // '6'
            55,  // '7'
            56,  // '8'
            57,  // '9'
            97,  // 'a'
            98,  // 'b'
            99,  // 'c'
            100, // 'd'
            101, // 'e'
            102, // 'f'
        };

        /// <summary>
        /// A rather shrewed method of removing value from an account.
        /// This can be very slow depending on the size of the state.
        /// For test backward compatibility only.  Should not be used in production.
        /// </summary>
        public static IAccount SetNull(this IAccount account, Address address)
        {
            var trie = MockState.Empty.Trie;
            var path = ToStateKey(address);
            foreach (var kv in account.Trie.IterateValues())
            {
                trie = kv.Path.Equals(path)
                    ? trie
                    : trie.Set(kv.Path, kv.Value);
            }

            return new Account(new AccountState(trie));
        }

        private static KeyBytes ToStateKey(Address address)
        {
            var addressBytes = address.ByteArray;
            byte[] buffer = new byte[addressBytes.Length * 2];
            for (int i = 0; i < addressBytes.Length; i++)
            {
                buffer[i * 2] = _conversionTable[addressBytes[i] >> 4];
                buffer[i * 2 + 1] = _conversionTable[addressBytes[i] & 0xf];
            }

            return new KeyBytes(buffer);
        }
    }
}
