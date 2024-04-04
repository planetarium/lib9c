#nullable enable
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class ValidatorDelegationSetCtrl
    {
        internal static ValidatorDelegationSet? GetValidatorDelegationSet(
            IWorldState states, Address validatorAddress)
        {
            Address validatorDelegationSetAddress = ValidatorDelegationSet.DeriveAddress(
                validatorAddress);

            if (states.GetDPoSState(validatorDelegationSetAddress) is { } value)
            {
                return new ValidatorDelegationSet(value);
            }

            return null;
        }

        internal static (IWorld, ValidatorDelegationSet) FetchValidatorDelegationSet(
            IWorld states, Address validatorAddress)
        {
            Address validatorDelegationSetAddress = ValidatorDelegationSet.DeriveAddress(
                validatorAddress);

            ValidatorDelegationSet validatorDelegationSet;
            if (states.GetDPoSState(validatorDelegationSetAddress) is { } value)
            {
                validatorDelegationSet = new ValidatorDelegationSet(value);
            }
            else
            {
                validatorDelegationSet = new ValidatorDelegationSet(validatorAddress);
                states = states.SetDPoSState(
                    validatorDelegationSetAddress, validatorDelegationSet.Serialize());
            }

            return (states, validatorDelegationSet);
        }

        internal static IWorld Add(
            IWorld states,
            Address validatorAddress,
            Address delegationAddress)
        {
            ValidatorDelegationSet validatorDelegationSet;
            (states, validatorDelegationSet)
                = FetchValidatorDelegationSet(states, validatorAddress);
            validatorDelegationSet.Add(delegationAddress);
            states = states.SetDPoSState(
                validatorDelegationSet.Address, validatorDelegationSet.Serialize());
            return states;
        }

        internal static IWorld Remove(
            IWorld states,
            Address validatorAddress,
            Address delegationAddress)
        {
            ValidatorDelegationSet validatorDelegationSet;
            (states, validatorDelegationSet)
                = FetchValidatorDelegationSet(states, validatorAddress);
            validatorDelegationSet.Remove(delegationAddress);
            states = states.SetDPoSState(
                validatorDelegationSet.Address, validatorDelegationSet.Serialize());
            return states;
        }
    }
}
