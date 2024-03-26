using Lib9c.DPoS.Action;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.DPoS.Control
{
    internal static class UnbondingSetCtrl
    {
        internal static UnbondingSet? GetUnbondingSet(IWorldState states)
        {
            if (states.GetDPoSState(ReservedAddress.UnbondingSet) is { } value)
            {
                return new UnbondingSet(value);
            }

            return null;
        }

        internal static (IWorld, UnbondingSet) FetchUnbondingSet(IWorld states)
        {
            UnbondingSet unbondingSet;
            if (states.GetDPoSState(ReservedAddress.UnbondingSet) is { } value)
            {
                unbondingSet = new UnbondingSet(value);
            }
            else
            {
                unbondingSet = new UnbondingSet();
                states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            }

            return (states, unbondingSet);
        }

        internal static IWorld CompleteValidatorSet(IWorld states, IActionContext ctx)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            foreach (Address address in unbondingSet.ValidatorAddressSet)
            {
                states = ValidatorCtrl.Complete(states, ctx, address);
            }

            return states;
        }

        internal static IWorld CompleteUndelegationSet(IWorld states, IActionContext ctx)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            foreach (Address address in unbondingSet.UndelegationAddressSet)
            {
                states = UndelegateCtrl.Complete(states, ctx, address);
            }

            return states;
        }

        internal static IWorld CompleteRedelegationSet(IWorld states, IActionContext ctx)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            foreach (Address address in unbondingSet.RedelegationAddressSet)
            {
                states = RedelegateCtrl.Complete(states, ctx, address);
            }

            return states;
        }

        internal static IWorld Complete(IWorld states, IActionContext ctx)
        {
            states = CompleteValidatorSet(states, ctx);
            states = CompleteUndelegationSet(states, ctx);
            states = CompleteRedelegationSet(states, ctx);

            return states;
        }

        internal static IWorld AddValidatorAddressSet(IWorld states, Address validatorAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.ValidatorAddressSet.Add(validatorAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }

        internal static IWorld AddUndelegationAddressSet(IWorld states, Address undelegationAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.UndelegationAddressSet.Add(undelegationAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }

        internal static IWorld AddRedelegationAddressSet(IWorld states, Address redelegationAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.RedelegationAddressSet.Add(redelegationAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }

        internal static IWorld RemoveValidatorAddressSet(IWorld states, Address validatorAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.ValidatorAddressSet.Remove(validatorAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }

        internal static IWorld RemoveUndelegationAddressSet(
            IWorld states, Address undelegationAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.UndelegationAddressSet.Remove(undelegationAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }

        internal static IWorld RemoveRedelegationAddressSet(
            IWorld states, Address redelegationAddress)
        {
            UnbondingSet unbondingSet;
            (states, unbondingSet) = FetchUnbondingSet(states);
            unbondingSet.RedelegationAddressSet.Remove(redelegationAddress);
            states = states.SetDPoSState(unbondingSet.Address, unbondingSet.Serialize());
            return states;
        }
    }
}
