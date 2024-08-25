#nullable enable
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Consensus;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorListModule
    {
        public static ProposerInfo GetProposerInfo(this IWorldState worldState)
            => worldState.TryGetProposerInfo(ProposerInfo.Address, out var proposerInfo)
                ? proposerInfo
                : throw new FailedLoadStateException("There is no such proposer info");

        public static bool TryGetProposerInfo(
            this IWorldState worldState,
            Address address,
            [NotNullWhen(true)] out ProposerInfo? proposerInfo)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorList).GetState(address);
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

        public static IWorld SetProposerInfo(
            this IWorld world,
            ProposerInfo proposerInfo)
            => world.MutateAccount(
                Addresses.ValidatorList,
                state => state.SetState(ProposerInfo.Address, proposerInfo.Bencoded));

        public static ValidatorList GetValidatorList(this IWorldState worldState)
            => worldState.TryGetValidatorList(out var validatorList)
                ? validatorList
                : throw new FailedLoadStateException("There is no such validator list");

        public static bool TryGetValidatorList(
            this IWorldState worldState,
            [NotNullWhen(true)] out ValidatorList? validatorList)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorList).GetState(ValidatorList.Address);
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
