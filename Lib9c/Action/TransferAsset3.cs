using System.Collections.Generic;
using System.Linq;
using Lib9c.Helper;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    public static class TransferAsset3
    {
        // FIXME justify this policy.
        public const long CrystalTransferringRestrictionStartIndex = 6_220_000L;

        // FIXME justify this policy.
        public static readonly IReadOnlyList<Address> AllowedCrystalTransfers = new Address[]
        {
            // world boss service
            new Address("CFCd6565287314FF70e4C4CF309dB701C43eA5bD"),
            // world boss ops
            new Address("3ac40802D359a6B51acB0AC0710cc90de19C9B81"),
        };

        public static void CheckCrystalSender(Currency currency, long blockIndex, Address sender)
        {
            if (currency.Equals(CrystalCalculator.CRYSTAL) &&
                blockIndex >= CrystalTransferringRestrictionStartIndex && !AllowedCrystalTransfers.Contains(sender))
            {
                throw new InvalidTransferCurrencyException($"transfer crystal not allowed {sender}");
            }
        }
    }
}
