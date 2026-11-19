using System;
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

        /// <summary>
        /// Returns a value in <c>[<paramref name="minValue"/>, <paramref name="maxValue"/>)</c>,
        /// like <see cref="IRandom.Next(int, int)"/> but for 64-bit bounds.
        /// </summary>
        /// <param name="random">The random source.</param>
        /// <param name="minValue">The inclusive lower bound.</param>
        /// <param name="maxValue">The exclusive upper bound.</param>
        /// <returns>The drawn value.</returns>
        /// <remarks>
        /// When both bounds fit in <see cref="int"/>, this calls
        /// <see cref="IRandom.Next(int, int)"/> and nothing else, so sheet values that already fit
        /// draw exactly what they drew before the sheet columns became <see cref="long"/>.
        /// Only a wider range reads 8 bytes from <see cref="IRandom.NextBytes"/>; its modulo
        /// bias is at most range / 2^64, negligible for sheet-sized ranges.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="minValue"/> is greater than <paramref name="maxValue"/>.
        /// </exception>
        public static long NextLong(this IRandom random, long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minValue),
                    $"{nameof(minValue)}({minValue}) is greater than {nameof(maxValue)}({maxValue}).");
            }

            if (minValue >= int.MinValue && maxValue <= int.MaxValue)
            {
                return random.Next((int)minValue, (int)maxValue);
            }

            var range = (ulong)(maxValue - minValue);
            if (range == 0)
            {
                return minValue;
            }

            var buffer = new byte[sizeof(ulong)];
            random.NextBytes(buffer);
            // Byte order is fixed here rather than taken from BitConverter, so every node
            // decodes the same value whatever its endianness.
            ulong sample = 0;
            foreach (var b in buffer)
            {
                sample = (sample << 8) | b;
            }

            return minValue + (long)(sample % range);
        }
    }
}
