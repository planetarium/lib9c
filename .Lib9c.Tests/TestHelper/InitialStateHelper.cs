using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using Nekoyume.Module;

public static class InitialStateHelper
{
    /// <summary>
    /// Randomly generates a <see cref=Currency"/> with "NCG" as its ticker, two decimal places,
    /// and a random address as its only minter.
    /// </summary>
    /// <remarks>
    /// The minter is changed every time this is called.
    /// </remarks>
    public static Currency RandomNCGTypeCurrency => Currency.Legacy("NCG", 2, new PrivateKey().Address);

    public static MockWorldState EmptyWorldState => MockWorldState.CreateModern();

    public static MockWorldState WithGoldCurrencyState(this MockWorldState mock) =>
        WithGoldCurrencyState(mock, RandomNCGTypeCurrency);

    public static MockWorldState WithGoldCurrencyState(this MockWorldState mock, Currency currency) =>
        WithGoldCurrencyState(mock, new GoldCurrencyState(currency));

    /// <summary>
    /// Creates a new <see cref="MockWorldState"/> with its gold currency state set with .
    /// </summary>
    /// <param name="mock"></param>
    /// <param name="goldCurrencyState"></param>
    /// <returns>A <see cref="MockWorldState"/> with <paramref name="goldCurrencyState"/>
    /// set as its <see cref="GoldCurrencyState"/> and the supply amount given
    /// to <see cref="GoldCurrencyState.Address"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when one of the following is true:
    /// <list type="bullet">
    /// <item><description>
    ///     <paramref name="mock"/> already has its gold currency state set.
    /// </description></item>
    /// <item><description>
    ///     <paramref name="goldCurrencyState"/>'s <see cref="Currency"/> has ticker that is not NCG.
    /// </description></item>
    /// <item><description>
    ///     <paramref name="goldCurrencyState"/>'s <see cref="Currency"/> has decimal places that is not 2.
    /// </description></item>
    /// </list>
    /// </exception>
    public static MockWorldState WithGoldCurrencyState(this MockWorldState mock, GoldCurrencyState goldCurrencyState)
    {
        if (mock.GetAccountState(ReservedAddresses.LegacyAccount).GetState(GoldCurrencyState.Address) is { } state)
        {
            throw new ArgumentException(
                $"Given {nameof(mock)} already has its gold currency state set: {state}",
                nameof(mock));
        }
        else if (!goldCurrencyState.Currency.Ticker.Equals("NCG"))
        {
            throw new ArgumentException(
                $"Given {nameof(goldCurrencyState)} must have currency with ticker NCG: {goldCurrencyState.Currency.Ticker}",
                nameof(goldCurrencyState));
        }
        else if (!goldCurrencyState.Currency.DecimalPlaces.Equals(2))
        {
            throw new ArgumentException(
                $"Given {nameof(goldCurrencyState)} must have currency with decimal places 2: {goldCurrencyState.Currency.DecimalPlaces}",
                nameof(goldCurrencyState));
        }

        mock = mock.SetAccount(
            ReservedAddresses.LegacyAccount,
            new Account(mock.GetAccountState(ReservedAddresses.LegacyAccount))
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize()));
        mock = mock.SetBalance(
            GoldCurrencyState.Address,
            goldCurrencyState.Currency * goldCurrencyState.InitialSupply);
        return mock;
    }
}
