using Libplanet.Action;
using Libplanet.Crypto;

namespace Nekoyume.Extensions
{
    public static class RandomExtensions
    {
        public static Address GenerateAddress(this IRandom random)
        {
            var buffer = new byte[Address.Size];
            random.NextBytes(buffer);

            return new Address(buffer);
        }
    }
}
