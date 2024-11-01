#nullable enable
using System;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorDelegator : Delegator<ValidatorRepository, ValidatorDelegatee, ValidatorDelegator>, IEquatable<ValidatorDelegator>
    {
        public ValidatorDelegator(
            Address address,
            Address delegationPoolAddress,
            Address rewardAddress,
            ValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegatorAccountAddress,
                  delegationPoolAddress: delegationPoolAddress,
                  rewardAddress: rewardAddress,
                  repository: repository)
        {
        }

        public ValidatorDelegator(
            Address address,
            ValidatorRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
        }

        public override void Delegate(ValidatorDelegatee delegatee, FungibleAssetValue fav, long height)
        {
            if (delegatee.Tombstoned)
            {
                throw new InvalidOperationException("Delegatee is tombstoned.");
            }

            base.Delegate(delegatee, fav, height);
        }

        public override void Redelegate(ValidatorDelegatee srcDelegatee, ValidatorDelegatee dstDelegatee, BigInteger share, long height)
        {
            if (dstDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("Destination delegatee is tombstoned.");
            }

            base.Redelegate(srcDelegatee, dstDelegatee, share, height);
        }

        public bool Equals(ValidatorDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
