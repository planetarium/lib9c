#nullable enable
using System.Collections.Generic;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class CurrencyComparer : IComparer<Currency>
    {
        private static readonly CurrencyComparer _byteComparer = new CurrencyByteComparer();

        public static CurrencyComparer Byte => _byteComparer;

        public abstract int Compare(Currency x, Currency y);
    }

    internal sealed class CurrencyByteComparer : CurrencyComparer
    {
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
