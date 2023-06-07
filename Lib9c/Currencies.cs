#nullable enable

using System;
using System.Globalization;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.TableData;

namespace Lib9c
{
    public static class Currencies
    {
        // NOTE: The minters of the NCG are not same between main-net and other networks
        //       containing local network. So this cannot be defined as a constant.
        //       After the pluggable-lib9c applied, this will be defined and
        //       used all of cases(e.g., main-net, unit tests, local network).
        // NOTE: If there are forked networks and the NCG's minters should be
        //       different, this should be changed and the migration solution
        //       should be applied by the forked network.
        // public static readonly Currency NCG = Currency.Legacy(
        //     "NCG",
        //     2,
        //     new Address("0x47D082a115c63E7b58B1532d20E631538eaFADde"));

        public static readonly Currency Crystal = Currency.Legacy(
            "CRYSTAL",
            18,
            minters: null);

        public static readonly Currency StakeRune = Currency.Legacy(
            "RUNE_GOLDENLEAF",
            0,
            minters: null);

        public static readonly Currency DailyRewardRune = Currency.Legacy(
            "RUNE_ADVENTURER",
            0,
            minters: null);

        public static readonly Currency Garage = Currency.Uncapped(
            "Garage",
            18,
            minters: null);

        // NOTE: This can be a cause of the hard-fork.
        private static readonly Currency[] CurrencyTypesOfFields = typeof(Currencies)
            .GetFields(System.Reflection.BindingFlags.Instance |
                       System.Reflection.BindingFlags.Static |
                       System.Reflection.BindingFlags.Public)
            .Select(fi => fi.GetValue(null))
            .OfType<Currency>()
            .ToArray();

        public static Currency? GetCurrency(string ticker)
        {
            var currencyTypes = CurrencyTypesOfFields.Where(c => c.Ticker == ticker).ToArray();
            if (currencyTypes.Any())
            {
                return currencyTypes.First();
            }

            if (IsRuneTicker(ticker))
            {
                return GetRune(ticker);
            }

            if (IsSoulstoneTicker(ticker))
            {
                return GetSoulStone(ticker);
            }

            return null;
        }

        public static Currency GetRune(string? ticker) =>
            string.IsNullOrEmpty(ticker)
                ? throw new ArgumentNullException(nameof(ticker))
                : Currency.Legacy(
                    ticker,
                    0,
                    minters: null);

        public static IOrderedEnumerable<Currency> GetRunes(params string?[] tickers) =>
            tickers.Select(GetRune).OrderBy(rune => rune.Hash.GetHashCode());

        public static IOrderedEnumerable<Currency> GetRunes(RuneSheet? sheet) =>
            sheet?.OrderedList is null
                ? throw new ArgumentNullException(
                    nameof(sheet),
                    "sheet or sheet.OrderedList is null.")
                : sheet.OrderedList
                    .Select(row => row.Ticker)
                    .Select(GetRune)
                    .OrderBy(rune => rune.Hash.GetHashCode());

        public static bool IsRuneTicker(string ticker)
        {
            ticker = ticker.ToLower(CultureInfo.InvariantCulture);
            return ticker.StartsWith("rune_") || ticker.StartsWith("runestone_");
        }

        public static bool IsSoulstoneTicker(string ticker) =>
            ticker.ToLower(CultureInfo.InvariantCulture).StartsWith("soulstone_");

        public static Currency GetSoulStone(string? ticker) =>
            string.IsNullOrEmpty(ticker)
                ? throw new ArgumentNullException(nameof(ticker))
                : Currency.Legacy(
                    ticker,
                    0,
                    minters: null);

        public static IOrderedEnumerable<Currency> GetSoulStones(params string?[] tickers) =>
            tickers.Select(GetSoulStone).OrderBy(soulStone => soulStone.Hash.GetHashCode());

        public static IOrderedEnumerable<Currency> GetSoulStones(PetSheet? sheet) =>
            sheet?.OrderedList is null
                ? throw new ArgumentNullException(
                    nameof(sheet),
                    "sheet or sheet.OrderedList is null.")
                : sheet.OrderedList
                    .Select(row => row.SoulStoneTicker)
                    .Select(GetSoulStone)
                    .OrderBy(soulStone => soulStone.Hash.GetHashCode());
    }
}
