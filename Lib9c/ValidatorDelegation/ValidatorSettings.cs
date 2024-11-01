#nullable enable
using System.Collections.Immutable;
using System.Numerics;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.ValidatorDelegation
{
    public static class ValidatorSettings
    {
        public static Currency ValidatorDelegationCurrency => Currencies.GuildGold;

        public static Currency ValidatorRewardCurrency => Currencies.Mead;

        public static long ValidatorUnbondingPeriod => 10L;

        public static int ValidatorMaxUnbondLockInEntries => 10;

        public static int ValidatorMaxRebondGraceEntries => 10;

        public static BigInteger BaseProposerRewardPercentage => 1;

        public static BigInteger BonusProposerRewardPercentage => 4;

        public static BigInteger DefaultCommissionPercentage => 10;

        public static BigInteger MinCommissionPercentage => 0;

        public static BigInteger MaxCommissionPercentage => 20;

        public static long CommissionPercentageUpdateCooldown => 100;

        public static BigInteger CommissionPercentageMaxChange => 1;

        public static Address BondedPoolAddress => new Address(
            ImmutableArray.Create<byte>(
                0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x42));

        public static Address UnbondedPoolAddress => new Address(
            ImmutableArray.Create<byte>(
                0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55));
    }
}
