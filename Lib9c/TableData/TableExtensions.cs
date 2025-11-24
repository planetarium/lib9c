#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.TableData.Swap;
using Nekoyume.Model.Item;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace Nekoyume.TableData
{
    public static class TableExtensions
    {
        public static bool TryParseDecimal(string value, out decimal result) =>
            decimal.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseLong(string value, out long result) =>
            long.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseBigInteger(string value, out BigInteger result) =>
            BigInteger.TryParse(value, out result);

        public static bool ParseBool(string value, bool defaultValue) =>
            bool.TryParse(value, out var result) ? result : defaultValue;

        public static int ParseInt(string value)
        {
            if (TryParseInt(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        public static int ParseInt(string value, int defaultValue) =>
            TryParseInt(value, out var result) ? result : defaultValue;

        public static decimal ParseDecimal(string value)
        {
            if (TryParseDecimal(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        public static decimal ParseDecimal(string value, decimal defaultValue) =>
            TryParseDecimal(value, out var result) ? result : defaultValue;

        public static long ParseLong(string value)
        {
            if (TryParseLong(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        public static long ParseLong(string value, long defaultValue) =>
            TryParseLong(value, out var result) ? result : defaultValue;

        public static BigInteger ParseBigInteger(string value)
        {
            if (BigInteger.TryParse(value, out var result))
            {
                return result;
            }

            throw new ArgumentException(value);
        }

        /// <summary>
        /// Parse a fraction from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Fraction should be in the form of "numerator/denominator".
        /// </param>
        /// <returns>
        /// The parsed fraction.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The string is not a valid fraction.
        /// </exception>
        public static SwapRateSheet.Fraction ParseFraction(string value)
        {
            if (TryParseFraction(value, out var result))
            {
                return result;
            }

            throw new ArgumentException(value);
        }

        /// <summary>
        /// Try to parse a fraction from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Fraction should be in the form of "numerator/denominator".
        /// </param>
        /// <param name="result">
        /// The parsed fraction.
        /// </param>
        /// <returns></returns>
        public static bool TryParseFraction(string value, out SwapRateSheet.Fraction result)
        {
            result = new SwapRateSheet.Fraction(BigInteger.One, BigInteger.One);

            List<string> fractionStrings = value.Split("/").ToList();
            if (fractionStrings.Count != 2)
            {
                return false;
            }

            if (!BigInteger.TryParse(fractionStrings[0].Trim(), out var numerator))
            {
                return false;
            }

            if (!BigInteger.TryParse(fractionStrings[1].Trim(), out var denominator))
            {
                return false;
            }

            result = new SwapRateSheet.Fraction(numerator, denominator);
            return true;
        }

        /// <summary>
        /// Parse a currency from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Currency should be in the form of "ticker;decimalPlaces;minters;totalSupplyTractable;maximumSupply".
        /// </param>
        /// <returns>
        /// The parsed currency.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The string is not a valid currency.
        /// </exception>
        public static Currency ParseCurrency(string value)
        {
            if (TryParseCurrency(value, out var result))
            {
                return result;
            }

            throw new ArgumentException(value);
        }

        /// <summary>
        /// Try to parse a currency from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Currency should be in the form of "ticker;decimalPlaces;minters;totalSupplyTractable;maximumSupply".
        /// </param>
        /// <param name="currency">
        /// The parsed currency.
        /// </param>
        /// <returns>
        /// Whether the parsing is successful.
        /// </returns>
        public static bool TryParseCurrency(string value, out Currency currency)
        {
            currency = default;

            List<string> currencyStrings = value.Split(";").Select(v => v.Trim()).ToList();
            if (currencyStrings.Count != 5)
            {
                return false;
            }

            string ticker = currencyStrings[0];

            if (!TryParseInt(currencyStrings[1], out var decimalPlacesInt))
            {
                return false;
            }

            byte decimalPlaces = (byte)decimalPlacesInt;

            if (!TryParseMinters(currencyStrings[2], out var minters))
            {
                return false;
            }

            bool totalSupplyTractable = ParseBool(currencyStrings[3], true);

            if (!TryParseMaximumSupply(currencyStrings[4], out var maximumSupply))
            {
                return false;
            }

            if (!totalSupplyTractable)
            {
                if (maximumSupply.HasValue)
                {
                    return false;
                }

                try
                {
                    currency = Currency.Legacy(ticker, decimalPlaces, minters);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (maximumSupply.HasValue)
            {
                try
                {
                    currency = Currency.Capped(ticker, decimalPlaces, maximumSupply.Value, minters);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                currency = Currency.Uncapped(ticker, decimalPlaces, minters);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try to parse a set of minters from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Minters should be in the form of "minter1:minter2:minter3...".
        /// </param>
        /// <param name="result">
        /// The parsed minters.
        /// </param>
        /// <returns>
        /// Whether the parsing is successful.
        /// </returns>
        private static bool TryParseMinters(string value, out ImmutableHashSet<Address>? result)
        {
            result = null;

            if (value.ToLowerInvariant() == "null")
            {
                return true;
            }

            List<string> mintersStrings = value.Split(":").ToList();

            result = ImmutableHashSet<Address>.Empty;

            foreach (string minterString in mintersStrings)
            {
                if (TryParseAddress(minterString, out var minter))
                {
                    result = result.Add(minter);
                }
                else
                {
                    result = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Try to parse a maximum supply from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// Maximum supply should be in the form of "major:minor".
        /// </param>
        /// <param name="result">
        /// The parsed maximum supply.
        /// </param>
        /// <returns>
        /// Whether the parsing is successful.
        /// </returns>
        private static bool TryParseMaximumSupply(string value, out (BigInteger Major, BigInteger Minor)? result)
        {
            result = null;

            if (value.ToLowerInvariant() == "null")
            {
                return true;
            }

            List<string> maximumSupplyStrings = value.Split(":").ToList();
            if (maximumSupplyStrings.Count != 2)
            {
                return false;
            }

            if (!BigInteger.TryParse(maximumSupplyStrings[0].Trim(), out var major))
            {
                return false;
            }

            if (!BigInteger.TryParse(maximumSupplyStrings[1].Trim(), out var minor))
            {
                return false;
            }

            result = (major, minor);
            return true;
        }

        /// <summary>
        /// Try to parse an address from a string.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// </param>
        /// <param name="result">
        /// The parsed address.
        /// </param>
        /// <returns>
        /// Whether the parsing is successful.
        /// </returns>
        private static bool TryParseAddress(string value, out Address result)
        {
            try
            {
                result = new Address(value);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Parses a colon-separated list of integers.
        /// Returns an empty list for null/empty input. Invalid tokens are ignored.
        /// </summary>
        public static List<int> ParseIntList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<int>();
            }

            return value
                .Split(':')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }

        /// <summary>
        /// Parses a colon-separated list of item subtypes.
        /// Accepts numeric values matching ItemSubType enum.
        /// Returns an empty list if input is null/empty or no valid items.
        /// </summary>
        public static List<ItemSubType> ParseItemSubTypes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<ItemSubType>();
            }

            return value.Split(':')
                .Select(s => s.Trim())
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => (ItemSubType)v!.Value)
                .ToList();
        }

        /// <summary>
        /// Parses a comma-separated list of rune types from a string.
        /// Returns an empty list if input is null/empty or no valid items.
        /// </summary>
        public static List<RuneType> ParseRuneTypes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<RuneType>();
            }

            return value.Split(',')
                .Select(s => s.Trim())
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => (RuneType)v!.Value)
                .ToList();
        }

        /// <summary>
        /// Parses a colon-separated list of elemental types.
        /// Accepts numeric values matching ElementalType enum.
        /// Returns an empty list if input is null/empty or no valid items.
        /// </summary>
        public static List<ElementalType> ParseElementalTypes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<ElementalType>();
            }

            return value.Split(':')
                .Select(s => s.Trim())
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => (ElementalType)v!.Value)
                .ToList();
        }

        /// <summary>
        /// Parses a colon-separated list of skill target types.
        /// Accepts numeric values matching SkillTargetType enum.
        /// Returns an empty list if input is null/empty or no valid items.
        /// </summary>
        public static List<SkillTargetType> ParseSkillTargetTypes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<SkillTargetType>();
            }

            return value.Split(':')
                .Select(s => s.Trim())
                .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => (SkillTargetType)v!.Value)
                .ToList();
        }

        /// <summary>
        /// Parses an integer or returns null for empty input.
        /// </summary>
        public static int? ParseIntOrNull(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return int.TryParse(value.Trim(), out var result) && result > 0 ? result : null;
        }

        /// <summary>
        /// Parses a trimmed string or returns null for empty input.
        /// </summary>
        public static string? ParseStringOrNull(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
