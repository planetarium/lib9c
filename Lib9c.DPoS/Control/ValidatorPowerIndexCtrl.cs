using Lib9c.DPoS.Action;
using Lib9c.DPoS.Exception;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;

namespace Lib9c.DPoS.Control
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
    }
}
