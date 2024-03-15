using Lib9c.DPoS.Action;
using Lib9c.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.DPoS.Control
{
    internal static class ValidatorDelegationSetCtrl
    {
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
