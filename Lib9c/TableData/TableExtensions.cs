#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

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

        public static Currency ParseCurrency(string value)
        {
            if (TryParseCurrency(value, out var result))
            {
                return result;
            }

            throw new ArgumentException(value);
        }

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

        private static bool TryParseMinters(string value, out ImmutableHashSet<Address>? result)
        {
            result = null;

            if (value.ToLowerInvariant() == "null")
            {
                return true;
            }

            List<string> mintersStrings = value.Split("/").ToList();

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

        private static bool TryParseMaximumSupply(string value, out (BigInteger Major, BigInteger Minor)? result)
        {
            result = null;

            if (value.ToLowerInvariant() == "null")
            {
                return true;
            }

            List<string> maximumSupplyStrings = value.Split("/").ToList();
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
    }
}
