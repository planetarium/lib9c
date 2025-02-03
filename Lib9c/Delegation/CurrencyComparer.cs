#nullable enable
using System.Collections.Generic;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    /// <summary>
    /// Represents a comparer for <see cref="Currency"/>.
    /// </summary>
    public abstract class CurrencyComparer : IComparer<Currency>
    {
        private static readonly CurrencyComparer _byteComparer = new CurrencyByteComparer();

        /// <summary>
        /// Gets a <see cref="CurrencyComparer"/> that compares <see cref="Currency"/> by their byte representation.
        /// </summary>
        public static CurrencyComparer Byte => _byteComparer;

        /// <inheritdoc/>
        public abstract int Compare(Currency x, Currency y);
    }

    /// <summary>
    /// Represents a comparer for <see cref="Currency"/> that compares by their byte representation.
    /// </summary>
    internal sealed class CurrencyByteComparer : CurrencyComparer
    {
        /// <inheritdoc/>
        public override int Compare(Currency x, Currency y)
            => ByteArrayCompare(x.Hash.ToByteArray(), y.Hash.ToByteArray());

        private static int ByteArrayCompare(byte[] x, byte[] y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] < y[i])
                {
                    return -1;
                }
                else if (x[i] > y[i])
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
