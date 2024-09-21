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
    public sealed class ValidatorDelegatee
        : Delegatee<ValidatorDelegator, ValidatorDelegatee>, IEquatable<ValidatorDelegatee>, IBencodable
    {
        // TODO: After guild-PoS implemented, delegation currency have to be changed into guild gold.
        public ValidatorDelegatee(
            Address address,
            PublicKey publicKey,
            Currency rewardCurrency,
            ValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: rewardCurrency,
                  rewardCurrency: rewardCurrency,
                  delegationPoolAddress: UnbondedPoolAddress,
                  unbondingPeriod: ValidatorUnbondingPeriod,
                  maxUnbondLockInEntries: ValidatorMaxUnbondLockInEntries,
                  maxRebondGraceEntries: ValidatorMaxRebondGraceEntries,
                  repository: repository)
        {
            if (!address.Equals(publicKey.Address))
            {
                throw new ArgumentException("The address and the public key do not match.");
            }

            PublicKey = publicKey;
            IsBonded = false;
            DelegationChanged += OnDelegationChanged;
            Enjailed += OnEnjailed;
            Unjailed += OnUnjailed;
        }

        public ValidatorDelegatee(
            Address address,
            IValue bencoded,
            ValidatorRepository repository)
            : this(
                  address: address,
                  bencoded: (List)bencoded,
                  repository: repository)
        {
        }

        public ValidatorDelegatee(
            Address address,
            List bencoded,
            ValidatorRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
            PublicKey = new PublicKey(((Binary)bencoded[0]).ByteArray);
            IsBonded = (Bencodex.Types.Boolean)bencoded[1];
            DelegationChanged += OnDelegationChanged;
        }

        public static Currency ValidatorDelegationCurrency => Currencies.GuildGold;

        public static long ValidatorUnbondingPeriod => 0L;

        public static int ValidatorMaxUnbondLockInEntries => 10;

        public static int ValidatorMaxRebondGraceEntries => 10;

        public static BigInteger BaseProposerRewardNumerator => 1;

        public static BigInteger BaseProposerRewardDenominator => 100;

        public static BigInteger BonusProposerRewardNumerator => 4;

        public static BigInteger BonusProposerRewardDenominator => 100;

        public static BigInteger CommissionNumerator => 1;

        public static BigInteger CommissionDenominator => 10;

        public static double CommissionMaxRate => 0.2;

        public static double CommissionMaxChangeRate => 0.01;

        public List Bencoded => List.Empty
            .Add(PublicKey.Format(true))
            .Add(IsBonded);

        IValue IBencodable.Bencoded => Bencoded;

        public PublicKey PublicKey { get; }

        public bool IsBonded { get; private set; }

        public BigInteger Power => TotalDelegated.RawValue;

        public Validator Validator => new(PublicKey, Power);

        public FungibleAssetValue MinSelfDelegation => DelegationCurrency * 10;

        public void AllocateReward(
            FungibleAssetValue rewardToAllocate,
            BigInteger validatorPower,
            BigInteger validatorSetPower,
            Address RewardSource,
            long height)
        {
            ValidatorRepository repository = (ValidatorRepository)Repository;

            FungibleAssetValue rewardAllocated
                = (rewardToAllocate * validatorPower).DivRem(validatorSetPower).Quotient;
            FungibleAssetValue commission
                = (rewardAllocated * CommissionNumerator).DivRem(CommissionDenominator).Quotient;
            FungibleAssetValue delegationRewards = rewardAllocated - commission;

            repository.TransferAsset(RewardSource, Address, commission);
            repository.TransferAsset(RewardSource, RewardCollectorAddress, delegationRewards);
            CollectRewards(height);
        }

        public void OnDelegationChanged(object? sender, long height)
        {
            ValidatorRepository repository = (ValidatorRepository)Repository;

            if (Jailed)
            {
                return;
            }

            if (Validator.Power.IsZero)
            {
                repository.SetValidatorList(repository.GetValidatorList().RemoveValidator(Validator.PublicKey));
            }
            else
            {
                repository.SetValidatorList(repository.GetValidatorList().SetValidator(Validator));
            }

            var selfDelegation = FAVFromShare(repository.GetBond(this, Address).Share);
            if (MinSelfDelegation > selfDelegation)
            {
                Jail(height, height);
            }
        }

        public void OnEnjailed(object? sender, long height)
        {
            ValidatorRepository repository = (ValidatorRepository)Repository;
            repository.SetValidatorList(repository.GetValidatorList().RemoveValidator(Validator.PublicKey));
        }

        public void OnUnjailed(object? sender, long height)
        {
            ValidatorRepository repository = (ValidatorRepository)Repository;
            repository.SetValidatorList(repository.GetValidatorList().SetValidator(Validator));
        }

        public bool Equals(ValidatorDelegatee? other)
            => other is ValidatorDelegatee validatorDelegatee
            && Metadata.Equals(validatorDelegatee.Metadata)
            && PublicKey.Equals(validatorDelegatee.PublicKey)
            && IsBonded == validatorDelegatee.IsBonded;

        public bool Equals(IDelegatee? other)
            => Equals(other as ValidatorDelegatee);

        public override bool Equals(object? obj)
            => Equals(obj as ValidatorDelegatee);

        public override int GetHashCode()
            => HashCode.Combine(Address, AccountAddress);

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
