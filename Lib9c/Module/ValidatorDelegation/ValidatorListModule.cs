#nullable enable
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Extensions;
using Lib9c.ValidatorDelegation;
using Libplanet.Crypto;

namespace Lib9c.Module.ValidatorDelegation
{
    public static class ValidatorListModule
    {
        public static ProposerInfo GetProposerInfo(this ValidatorRepository repository)
            => repository.TryGetProposerInfo(ProposerInfo.Address, out var proposerInfo)
                ? proposerInfo
                : throw new FailedLoadStateException("There is no such proposer info");

        public static bool TryGetProposerInfo(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ProposerInfo? proposerInfo)
        {
            try
            {
                var value = repository.World.GetAccountState(Addresses.ValidatorList).GetState(address);
                if (!(value is List list))
                {
                    proposerInfo = null;
                    return false;
                }

                proposerInfo = new ProposerInfo(list);
                return true;
            }
            catch
            {
                proposerInfo = null;
                return false;
            }
        }

        public static ValidatorRepository SetProposerInfo(
            this ValidatorRepository repository,
            ProposerInfo proposerInfo)
        {
            repository.UpdateWorld(repository.World.MutateAccount(
                Addresses.ValidatorList,
                state => state.SetState(ProposerInfo.Address, proposerInfo.Bencoded)));
            return repository;
        }

        public static ValidatorList GetValidatorList(this ValidatorRepository repository)
            => repository.TryGetValidatorList(out var validatorList)
                ? validatorList
                : throw new FailedLoadStateException("There is no such validator list");

        public static bool TryGetValidatorList(
            this ValidatorRepository repository,
            [NotNullWhen(true)] out ValidatorList? validatorList)
        {
            try
            {
                var value = repository.World.GetAccountState(Addresses.ValidatorList).GetState(ValidatorList.Address);
                if (!(value is List list))
                {
                    validatorList = null;
                    return false;
                }

                validatorList = new ValidatorList(list);
                return true;
            }
            catch
            {
                validatorList = null;
                return false;
            }
        }
    }
}
