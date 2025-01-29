using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Swap
{
    public class SwapRateSheet : Sheet<SwapRateSheet.CurrencyPair, SwapRateSheet.Row>
    {
        public class Row : SheetRow<CurrencyPair>
        {
            public override CurrencyPair Key => Pair;

            public CurrencyPair Pair { get; private set; }

            public Fraction Rate { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                var currencyFrom = ParseCurrency(fields[0]);
                var currencyTo = ParseCurrency(fields[1]);
                Pair = new CurrencyPair(currencyFrom, currencyTo);
                Rate = ParseFraction(fields[2]);
            }
        }

        public class CurrencyPair : IEquatable<CurrencyPair>, IComparable<CurrencyPair>
        {
            public Currency From { get; }

            public Currency To { get; }

            public CurrencyPair(Currency from, Currency to)
            {
                From = from;
                To = to;
            }

            public static bool operator ==(CurrencyPair left, CurrencyPair right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CurrencyPair left, CurrencyPair right)
            {
                return !left.Equals(right);
            }

            public static bool operator <(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) < 0;
            }

            public static bool operator >(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) > 0;
            }

            public static bool operator <=(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) <= 0;
            }

            public static bool operator >=(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) >= 0;
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

            public int CompareTo(CurrencyPair other)
            {
                var fromComparison = CurrencyComparer.Byte.Compare(From, other.From);
                if (fromComparison != 0)
                {
                    return fromComparison;
                }
        
                return CurrencyComparer.Byte.Compare(To, other.To);
            }
        }

        public class Fraction : IEquatable<Fraction>
        {
            public BigInteger Numerator { get; }
            public BigInteger Denominator { get; }

            public Fraction(BigInteger numerator, BigInteger denominator)
            {
                Numerator = numerator;
                Denominator = denominator;
            }

            public override bool Equals(object obj)
            {
                if (obj is Fraction other)
                {
                    return Equals(other);
                }

                return false;
            }

            public bool Equals(Fraction other)
                => Numerator.Equals(other.Numerator) && Denominator.Equals(other.Denominator);

            public override int GetHashCode()
            {
                return HashCode.Combine(Numerator, Denominator);
            }
        }

        public SwapRateSheet() : base(nameof(SwapRateSheet))
        {
        }
    }
}
