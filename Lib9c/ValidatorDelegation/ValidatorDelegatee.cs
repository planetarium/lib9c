#nullable enable
using System;
using System.Collections.Immutable;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.Delegation;

namespace Nekoyume.ValidatorDelegation
{
    public class ValidatorDelegatee : Delegatee<ValidatorDelegator, ValidatorDelegatee>, IEquatable<ValidatorDelegatee>, IBencodable
    {
        public ValidatorDelegatee(Address address, PublicKey publicKey, Currency rewardCurrency, ValidatorRepository? repository = null)
            : base(address, repository)
        {
            if (!address.Equals(publicKey.Address))
            {
                throw new ArgumentException("The address and the public key do not match.");
            }

            Publickey = publicKey;
            IsBonded = false;
            RewardCurrency = rewardCurrency;
        }

        public ValidatorDelegatee(Address address, IValue bencoded, Currency rewardCurrency, ValidatorRepository? repository = null)
            : this(address, (List)bencoded, rewardCurrency, repository)
        {
        }

        public ValidatorDelegatee(Address address, List bencoded, Currency rewardCurrency, ValidatorRepository? repository = null)
            : base(address, bencoded[0], repository)
        {
            Publickey = new PublicKey(((Binary)bencoded[1]).ByteArray);
            IsBonded = (Bencodex.Types.Boolean)bencoded[2];
            RewardCurrency = rewardCurrency;
        }

        public override byte[] DelegateeId => new byte[] { 0x56 }; // `V`

        public override Address DelegationPoolAddress => IsBonded
            ? BondedPoolAddress
            : UnbondedPoolAddress;

        public override Currency DelegationCurrency => Currencies.GuildGold;

        public override Currency RewardCurrency { get; }

        public override long UnbondingPeriod => 0L;

        public override int MaxUnbondLockInEntries => 10;

        public override int MaxRebondGraceEntries => 10;

        public override BigInteger SlashFactor => 10;

        public override List Bencoded => List.Empty
            .Add(base.Bencoded)
            .Add(Publickey.Format(true))
            .Add(IsBonded);

        IValue IBencodable.Bencoded => Bencoded;

        public PublicKey Publickey { get; }

        public bool IsBonded { get; private set; }

        public BigInteger Power => TotalDelegated.RawValue;

        public Validator Validator => new(Publickey, Power);

        public static BigInteger BaseProposerRewardNumerator => 1;

        public static BigInteger BaseProposerRewardDenominator => 100;

        public static BigInteger BonusProposerRewardNumerator => 4;

        public static BigInteger BonusProposerRewardDenominator => 100;

        public static BigInteger CommissionNumerator => 1;

        public static BigInteger CommissionDenominator => 10;

        public static double CommissionMaxRate => 0.2;

        public static double CommissionMaxChangeRate => 0.01;

        public override BigInteger Bond(ValidatorDelegator delegator, FungibleAssetValue fav, long height)
        {
            BigInteger share = base.Bond(delegator, fav, height);
            ValidatorRepository repo = (ValidatorRepository)Repository!;
            repo.SetValidatorList(repo.GetValidatorList().SetValidator(Validator));
            return share;
        }

        public override FungibleAssetValue Unbond(ValidatorDelegator delegator, BigInteger share, long height)
        {
            FungibleAssetValue fav = base.Unbond(delegator, share, height);
            ValidatorRepository repo = (ValidatorRepository)Repository!;

            if (Validator.Power.IsZero)
            {
                repo.SetValidatorList(repo.GetValidatorList().RemoveValidator(Validator.PublicKey));
                return fav;
            }

            repo.SetValidatorList(repo.GetValidatorList().SetValidator(Validator));
            return fav;
        }

        public void AllocateReward(
            FungibleAssetValue rewardToAllocate,
            BigInteger validatorPower,
            BigInteger validatorSetPower,
            Address RewardSource,
            long height)
        {
            ValidatorRepository repo = (ValidatorRepository)Repository!;

            FungibleAssetValue rewardAllocated
                = (rewardToAllocate * validatorPower).DivRem(validatorSetPower).Quotient;
            FungibleAssetValue commission
                = (rewardAllocated * CommissionNumerator).DivRem(CommissionDenominator).Quotient;
            FungibleAssetValue delegationRewards = rewardAllocated - commission;

            repo.TransferAsset(RewardSource, Address, commission);
            repo.TransferAsset(RewardSource, RewardCollectorAddress, delegationRewards);
            CollectRewards(height);
        }

        public bool Equals(ValidatorDelegatee? other)
            => base.Equals(other)
            && Publickey.Equals(other.Publickey)
            && IsBonded == other.IsBonded;

        public override bool Equals(IDelegatee? other)
            => Equals(other as ValidatorDelegatee);

        public override bool Equals(object? obj)
            => Equals(obj as ValidatorDelegatee);

        public override int GetHashCode()
            => base.GetHashCode();

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
