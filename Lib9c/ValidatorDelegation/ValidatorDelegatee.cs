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
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
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
            CommissionPercentage = DefaultCommissionPercentage;
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
            CommissionPercentage = (Integer)bencoded[2];
            DelegationChanged += OnDelegationChanged;
        }

        public static Currency ValidatorDelegationCurrency => Currencies.GuildGold;

        public static long ValidatorUnbondingPeriod => 0L;

        public static int ValidatorMaxUnbondLockInEntries => 10;

        public static int ValidatorMaxRebondGraceEntries => 10;

        public static BigInteger BaseProposerRewardPercentage => 1;

        public static BigInteger BonusProposerRewardPercentage => 4;

        public static BigInteger DefaultCommissionPercentage => 10;

        public static BigInteger MaxCommissionPercentage => 20;

        public static BigInteger CommissionPercentageMaxChange => 1;

        public BigInteger CommissionPercentage { get; private set; }

        public List Bencoded => List.Empty
            .Add(PublicKey.Format(true))
            .Add(IsBonded)
            .Add(CommissionPercentage);

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
                = (rewardAllocated * CommissionPercentage).DivRem(100).Quotient;
            FungibleAssetValue delegationRewards = rewardAllocated - commission;

            repository.TransferAsset(RewardSource, Address, commission);
            repository.TransferAsset(RewardSource, RewardPoolAddress, delegationRewards);
            CollectRewards(height);
        }

        public void SetCommissionPercentage(BigInteger percentage)
        {
            if (percentage < 0 || percentage > MaxCommissionPercentage)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(percentage),
                    $"The commission percentage must be between 0 and {MaxCommissionPercentage}.");
            }

            if (BigInteger.Abs(CommissionPercentage - percentage) > CommissionPercentageMaxChange)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(percentage),
                    $"The commission percentage can be changed by at most {CommissionPercentageMaxChange}.");
            }

            CommissionPercentage = percentage;
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
            && IsBonded == validatorDelegatee.IsBonded
            && CommissionPercentage == validatorDelegatee.CommissionPercentage;

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
