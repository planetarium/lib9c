#nullable enable
using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.Delegation;
using static Nekoyume.ValidatorDelegation.ValidatorSettings;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorDelegatee
        : Delegatee<ValidatorRepository, ValidatorDelegatee, ValidatorDelegator>, IEquatable<ValidatorDelegatee>, IBencodable
    {
        // TODO: After guild-PoS implemented, delegation currency have to be changed into guild gold.
        public ValidatorDelegatee(
            Address address,
            PublicKey publicKey,
            BigInteger commissionPercentage,
            long creationHeight,
            ValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: ValidatorDelegationCurrency,
                  rewardCurrency: ValidatorRewardCurrency,
                  delegationPoolAddress: UnbondedPoolAddress,
                  rewardPoolAddress: DelegationAddress.RewardPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: ValidatorUnbondingPeriod,
                  maxUnbondLockInEntries: ValidatorMaxUnbondLockInEntries,
                  maxRebondGraceEntries: ValidatorMaxRebondGraceEntries,
                  repository: repository)
        {
            if (!address.Equals(publicKey.Address))
            {
                throw new ArgumentException("The address and the public key do not match.");
            }

            if (commissionPercentage < MinCommissionPercentage || commissionPercentage > MaxCommissionPercentage)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(commissionPercentage),
                    $"The commission percentage must be between {MinCommissionPercentage} and {MaxCommissionPercentage}.");
            }

            PublicKey = publicKey;
            IsBonded = false;
            CommissionPercentage = commissionPercentage;
            CommissionPercentageLastUpdateHeight = creationHeight;
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
            CommissionPercentageLastUpdateHeight = (Integer)bencoded[3];
        }

        public BigInteger CommissionPercentage { get; private set; }

        public long CommissionPercentageLastUpdateHeight { get; private set; }

        public List Bencoded => List.Empty
            .Add(PublicKey.Format(true))
            .Add(IsBonded)
            .Add(CommissionPercentage)
            .Add(CommissionPercentageLastUpdateHeight);

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
            ValidatorRepository repository = Repository;

            FungibleAssetValue rewardAllocated
                = (rewardToAllocate * validatorPower).DivRem(validatorSetPower).Quotient;
            FungibleAssetValue commission
                = (rewardAllocated * CommissionPercentage).DivRem(100).Quotient;
            FungibleAssetValue delegationRewards = rewardAllocated - commission;

            if (commission.Sign > 0)
            {
                repository.TransferAsset(RewardSource, Address, commission);
            }

            if (delegationRewards.Sign > 0)
            {
                repository.TransferAsset(RewardSource, RewardPoolAddress, delegationRewards);
            }

            CollectRewards(height);
        }

        public void SetCommissionPercentage(BigInteger percentage, long height)
        {
            if (height - CommissionPercentageLastUpdateHeight < CommissionPercentageUpdateCooldown)
            {
                throw new InvalidOperationException(
                    $"The commission percentage can be updated only once in {CommissionPercentageUpdateCooldown} blocks.");
            }

            if (percentage < MinCommissionPercentage || percentage > MaxCommissionPercentage)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(percentage),
                    $"The commission percentage must be between {MinCommissionPercentage} and {MaxCommissionPercentage}.");
            }

            if (BigInteger.Abs(CommissionPercentage - percentage) > CommissionPercentageMaxChange)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(percentage),
                    $"The commission percentage can be changed by at most {CommissionPercentageMaxChange}.");
            }

            CommissionPercentage = percentage;
            CommissionPercentageLastUpdateHeight = height;
        }

        public new void Unjail(long height)
        {
            ValidatorRepository repository = Repository;
            var selfDelegation = FAVFromShare(repository.GetBond(this, Address).Share);
            if (MinSelfDelegation > selfDelegation)
            {
                throw new InvalidOperationException("The self-delegation is still below the minimum.");
            }

            base.Unjail(height);
        }

        public bool Equals(ValidatorDelegatee? other)
            => other is ValidatorDelegatee validatorDelegatee
            && Metadata.Equals(validatorDelegatee.Metadata)
            && PublicKey.Equals(validatorDelegatee.PublicKey)
            && IsBonded == validatorDelegatee.IsBonded
            && CommissionPercentage == validatorDelegatee.CommissionPercentage
            && CommissionPercentageLastUpdateHeight == validatorDelegatee.CommissionPercentageLastUpdateHeight;

        public bool Equals(IDelegatee? other)
            => Equals(other as ValidatorDelegatee);

        public override bool Equals(object? obj)
            => Equals(obj as ValidatorDelegatee);

        public override int GetHashCode()
            => HashCode.Combine(Address, AccountAddress);

        protected override void OnDelegationChanged(DelegationChangedEventArgs e)
        {
            base.OnDelegationChanged(e);

            ValidatorRepository repository = Repository;
            var height = e.Height;

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
            if (MinSelfDelegation > selfDelegation && !Jailed)
            {
                Jail(height);
            }
        }

        protected override void OnEnjailed(EventArgs e)
        {
            base.OnEnjailed(e);
            ValidatorRepository repository = Repository;
            repository.SetValidatorList(repository.GetValidatorList().RemoveValidator(Validator.PublicKey));
        }

        protected override void OnUnjailed(EventArgs e)
        {
            base.OnUnjailed(e);
            ValidatorRepository repository = Repository;
            repository.SetValidatorList(repository.GetValidatorList().SetValidator(Validator));
        }
    }
}
