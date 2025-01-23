using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Types.Assets;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Swap
{
    public class SwapRateSheet : Sheet<SwapRateSheet.CurrencyPair, SwapRateSheet.Row>
    {
        public class Row : SheetRow<CurrencyPair>
        {
            public override CurrencyPair Key => Pair;

            public CurrencyPair Pair { get; private set; }

            public BigInteger RateNumerator { get; private set; }

            public BigInteger RateDenominator { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                var currencyFrom = ParseCurrency(fields[0]);
                var currencyTo = ParseCurrency(fields[1]);
                Pair = new CurrencyPair(currencyFrom, currencyTo);
                RateNumerator = ParseBigInteger(fields[2]);
                RateDenominator = ParseBigInteger(fields[3]);
            }
        }

        public class CurrencyPair : IEquatable<CurrencyPair>
        {
            public Currency From { get; }
            public Currency To { get; }

            public CurrencyPair(Currency from, Currency to)
            {
                From = from;
                To = to;
            }

            public override bool Equals(object obj)
            {
                if (obj is CurrencyPair other)
                {
                    return Equals(other);
                }

                return false;
            }

            public bool Equals(CurrencyPair other)
                => From.Equals(other.From) && To.Equals(other.To);

            public override int GetHashCode()
            {
                return HashCode.Combine(From, To);
            }
        }

        public SwapRateSheet() : base(nameof(SwapRateSheet))
        {
        }
    }
}
