#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Swap
{
    /// <summary>
    /// Represents a sheet of swap rates.
    /// </summary>
    public class SwapRateSheet : Sheet<SwapRateSheet.CurrencyPair, SwapRateSheet.Row>
    {
        /// <summary>
        /// Represents a row of <see cref="SwapRateSheet"/>.
        /// </summary>
        public class Row : SheetRow<CurrencyPair>
        {
            /// <inheritdoc/>
            public override CurrencyPair Key => Pair;

            /// <summary>
            /// The currency pair for swap.
            /// </summary>
            public CurrencyPair Pair { get; private set; }

            /// <summary>
            /// The swap rate represented by <see cref="Fraction"/>.
            /// </summary>
            public Fraction Rate { get; private set; }

            /// <inheritdoc/>
            public override void Set(IReadOnlyList<string> fields)
            {
                var currencyFrom = ParseCurrency(fields[0]);
                var currencyTo = ParseCurrency(fields[1]);
                Pair = new CurrencyPair(currencyFrom, currencyTo);
                Rate = ParseFraction(fields[2]);
            }
        }

        /// <summary>
        /// Represents a currency pair for swap.
        /// </summary>
        public class CurrencyPair : IEquatable<CurrencyPair>, IComparable<CurrencyPair>
        {
            /// <summary>
            /// The currency to swap from.
            /// </summary>
            public Currency From { get; }

            /// <summary>
            /// The currency to swap to.
            /// </summary>
            public Currency To { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CurrencyPair"/> class.
            /// </summary>
            /// <param name="from">
            /// The currency to swap from.
            /// </param>
            /// <param name="to">
            /// The currency to swap to.
            /// </param>
            public CurrencyPair(Currency from, Currency to)
            {
                From = from;
                To = to;
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="CurrencyPair"/> are equal.
            /// </summary>
            /// <param name="left">
            /// The first <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <param name="right">
            /// The second <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent the same value;
            /// </returns>
            public static bool operator ==(CurrencyPair left, CurrencyPair right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="CurrencyPair"/> are not equal.
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static bool operator !=(CurrencyPair left, CurrencyPair right)
            {
                return !left.Equals(right);
            }

            /// <summary>
            /// Determines whether one specified <see cref="CurrencyPair"/> is less than another specified <see cref="CurrencyPair"/>.
            /// </summary>
            /// <param name="left">
            /// The first <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static bool operator <(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) < 0;
            }

            /// <summary>
            /// Determines whether one specified <see cref="CurrencyPair"/> is greater than another specified <see cref="CurrencyPair"/>.
            /// </summary>
            /// <param name="left">
            /// The first <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <param name="right">
            /// The second <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>;
            /// </returns>
            public static bool operator >(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) > 0;
            }

            /// <summary>
            /// Determines whether one specified <see cref="CurrencyPair"/> is less than or equal to another specified <see cref="CurrencyPair"/>.
            /// </summary>
            /// <param name="left">
            /// The first <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <param name="right">
            /// The second <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>;
            /// </returns>
            public static bool operator <=(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) <= 0;
            }

            /// <summary>
            /// Determines whether one specified <see cref="CurrencyPair"/> is greater than or equal to another specified <see cref="CurrencyPair"/>.
            /// </summary>
            /// <param name="left">
            /// The first <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <param name="right">
            /// The second <see cref="CurrencyPair"/> to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>;
            /// </returns>
            public static bool operator >=(CurrencyPair left, CurrencyPair right)
            {
                return left.CompareTo(right) >= 0;
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is CurrencyPair other)
                {
                    return Equals(other);
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(CurrencyPair? other)
                => other is CurrencyPair currencyPair
                && From.Equals(currencyPair.From) && To.Equals(currencyPair.To);

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(From, To);
            }

            /// <inheritdoc/>
            public int CompareTo(CurrencyPair? other)
            {
                if (other is null)
                {
                    return 1;
                }

                var fromComparison = CurrencyComparer.Byte.Compare(From, other.From);
                if (fromComparison != 0)
                {
                    return fromComparison;
                }
        
                return CurrencyComparer.Byte.Compare(To, other.To);
            }
        }

        /// <summary>
        /// Represents a fraction.
        /// </summary>
        public class Fraction : IEquatable<Fraction>
        {
            /// <summary>
            /// The numerator of the fraction.
            /// </summary>
            public BigInteger Numerator { get; }

            /// <summary>
            /// The denominator of the fraction.
            /// </summary>
            public BigInteger Denominator { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Fraction"/> class.
            /// </summary>
            /// <param name="numerator">
            /// The numerator of the fraction.
            /// </param>
            /// <param name="denominator">
            /// The denominator of the fraction.
            /// </param>
            public Fraction(BigInteger numerator, BigInteger denominator)
            {
                Numerator = numerator;
                Denominator = denominator;
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is Fraction other)
                {
                    return Equals(other);
                }

                return false;
            }

            /// <inheritdoc/>
            public bool Equals(Fraction? other)
                => other is Fraction fraction
                && Numerator.Equals(fraction.Numerator)
                && Denominator.Equals(fraction.Denominator);

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(Numerator, Denominator);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapRateSheet"/> class.
        /// </summary>
        public SwapRateSheet() : base(nameof(SwapRateSheet))
        {
        }
    }
}
