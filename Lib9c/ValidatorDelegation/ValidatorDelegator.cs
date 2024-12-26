#nullable enable
using System;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorDelegator
        : Delegator<ValidatorRepository, ValidatorDelegatee, ValidatorDelegator>,
        IEquatable<ValidatorDelegator>
    {
        public ValidatorDelegator(
            Address address,
            Address delegationPoolAddress,
            ValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegatorAccountAddress,
                  delegationPoolAddress: delegationPoolAddress,
                  rewardAddress: address,
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

        public bool Equals(ValidatorDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
