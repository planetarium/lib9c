using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Nekoyume.Action.Extensions
{
    public static class ActionContextExtensions
    {
        public static bool IsMainNet(this IActionContext context)
        {
            var goldCurrency = context.PreviousState.GetAccount(ReservedAddresses.LegacyAccount).GetGoldCurrency();
            return goldCurrency.Minters
                       .Contains(new Address("47d082a115c63e7b58b1532d20e631538eafadde"))
                   && goldCurrency.Ticker == "NCG"
                   && goldCurrency.DecimalPlaces == 2;
        }

        public static bool Since(this IActionContext context, long blockIndex)
        {
            return blockIndex <= context.BlockIndex;
        }
    }
}
