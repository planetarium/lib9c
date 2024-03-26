using Lib9c.DPoS.Action;
using Lib9c.DPoS.Exception;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Module;

namespace Lib9c.DPoS.Control
{
    internal static class ValidatorSetCtrl
    {
        internal static ValidatorSet? GetValidatorSet(IWorldState states, Address address)
        {
            if (states.GetDPoSState(address) is { } value)
            {
                return new ValidatorSet(value);
            }

            return null;
        }

        internal static (IWorld, ValidatorSet) FetchValidatorSet(IWorld states, Address address)
        {
            ValidatorSet validatorSet;
            if (states.GetDPoSState(address) is { } value)
            {
                validatorSet = new ValidatorSet(value);
            }
            else
            {
                validatorSet = new ValidatorSet();
                states = states.SetDPoSState(
                    address, validatorSet.Serialize());
            }

            return (states, validatorSet);
        }

        internal static (IWorld, ValidatorSet) FetchBondedValidatorSet(IWorld states)
            => FetchValidatorSet(states, ReservedAddress.BondedValidatorSet);

        // Have to be called on tip changed
        internal static IWorld Update(IWorld states, IActionContext ctx)
        {
            states = UpdateSets(states);
            states = UpdateBondedSetElements(states, ctx);
            states = UpdateUnbondedSetElements(states, ctx);
            states = UnbondingSetCtrl.Complete(states, ctx);

            return states;
        }

        internal static IWorld UpdateSets(IWorld states)
        {
            ValidatorSet previousBondedSet;
            (states, previousBondedSet) = FetchValidatorSet(
                states, ReservedAddress.BondedValidatorSet);
            ValidatorSet bondedSet = new ValidatorSet();
            ValidatorSet unbondedSet = new ValidatorSet();
            ValidatorPowerIndex validatorPowerIndex;
            (states, validatorPowerIndex)
                = ValidatorPowerIndexCtrl.FetchValidatorPowerIndex(states);

            foreach (var item in validatorPowerIndex.Index.Select((value, index) => (value, index)))
            {
                if (!(ValidatorCtrl.GetValidator(
                    states, item.value.ValidatorAddress) is { } validator))
                {
                    throw new NullValidatorException(item.value.ValidatorAddress);
                }

                if (validator.Jailed)
                {
                    throw new JailedValidatorException(validator.Address);
                }

                if (item.index >= ValidatorSet.MaxBondedSetSize ||
                    states.GetBalance(item.value.ValidatorAddress, Asset.ConsensusToken)
                    <= Asset.ConsensusToken * 0)
                {
                    unbondedSet.Add(item.value);
                }
                else
                {
                    bondedSet.Add(item.value);
                }
            }

            states = states.SetDPoSState(
                ReservedAddress.PreviousBondedValidatorSet, previousBondedSet.Serialize());
            states = states.SetDPoSState(
                ReservedAddress.BondedValidatorSet, bondedSet.Serialize());
            states = states.SetDPoSState(
                ReservedAddress.UnbondedValidatorSet, unbondedSet.Serialize());

            return states;
        }

        internal static IWorld UpdateBondedSetElements(IWorld states, IActionContext ctx)
        {
            ValidatorSet bondedSet;
            (states, bondedSet) = FetchBondedValidatorSet(states);
            foreach (ValidatorPower validatorPower in bondedSet.Set)
            {
                if (!(ValidatorCtrl.GetValidator(
                    states, validatorPower.ValidatorAddress) is { } validator))
                {
                    throw new NullValidatorException(validatorPower.ValidatorAddress);
                }

                states = ValidatorCtrl.Bond(states, ctx, validatorPower.ValidatorAddress);
            }

            return states;
        }

        internal static IWorld UpdateUnbondedSetElements(IWorld states, IActionContext ctx)
        {
            ValidatorSet unbondedSet;
            (states, unbondedSet) = FetchValidatorSet(states, ReservedAddress.UnbondedValidatorSet);
            foreach (ValidatorPower validatorPower in unbondedSet.Set)
            {
                if (!(ValidatorCtrl.GetValidator(
                    states, validatorPower.ValidatorAddress) is { } validator))
                {
                    throw new NullValidatorException(validatorPower.ValidatorAddress);
                }

                states = ValidatorCtrl.Unbond(states, ctx, validatorPower.ValidatorAddress);
            }

            return states;
        }
    }
}
