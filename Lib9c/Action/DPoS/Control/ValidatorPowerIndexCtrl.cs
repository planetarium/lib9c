#nullable enable
using System.Collections.Generic;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class ValidatorPowerIndexCtrl
    {
        internal static ValidatorPowerIndex? GetValidatorPowerIndex(IWorldState states)
        {
            if (states.GetDPoSState(ReservedAddress.ValidatorPowerIndex) is { } value)
            {
                return new ValidatorPowerIndex(value);
            }

            return null;
        }

        internal static (IWorld, ValidatorPowerIndex) FetchValidatorPowerIndex(IWorld states)
        {
            ValidatorPowerIndex validatorPowerIndex;
            if (states.GetDPoSState(ReservedAddress.ValidatorPowerIndex) is { } value)
            {
                validatorPowerIndex = new ValidatorPowerIndex(value);
            }
            else
            {
                validatorPowerIndex = new ValidatorPowerIndex();
                states = states.SetDPoSState(
                    validatorPowerIndex.Address, validatorPowerIndex.Serialize());
            }

            return (states, validatorPowerIndex);
        }

        internal static IWorld Update(
            IWorld states,
            Address validatorAddress)
        {
            ValidatorPowerIndex validatorPowerIndex;
            (states, validatorPowerIndex) = FetchValidatorPowerIndex(states);
            validatorPowerIndex.Index.RemoveWhere(
                key => key.ValidatorAddress.Equals(validatorAddress));
            if (!(ValidatorCtrl.GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (validator.Jailed)
            {
                return states;
            }

            FungibleAssetValue consensusToken = states.GetBalance(
                validatorAddress, Asset.ConsensusToken);
            ValidatorPower validatorPower
                = new ValidatorPower(validatorAddress, validator.OperatorPublicKey, consensusToken);
            validatorPowerIndex.Index.Add(validatorPower);
            states = states.SetDPoSState(validatorPowerIndex.Address, validatorPowerIndex.Serialize());
            return states;
        }

        internal static IWorld Update(IWorld states, IEnumerable<Address> validatorAddresses)
        {
            foreach (Address validatorAddress in validatorAddresses)
            {
                states = Update(states, validatorAddress);
            }

            return states;
        }

        internal static IWorld Remove(IWorld states, Address validatorAddress)
        {
            ValidatorPowerIndex validatorPowerIndex;
            (states, validatorPowerIndex) = FetchValidatorPowerIndex(states);
            var index = validatorPowerIndex.Index.RemoveWhere(
                key => key.ValidatorAddress.Equals(validatorAddress));
            if (index < 0)
            {
                throw new NullValidatorException(validatorAddress);
            }
            states = states.SetDPoSState(validatorPowerIndex.Address, validatorPowerIndex.Serialize());
            return states;
        }
    }
}
