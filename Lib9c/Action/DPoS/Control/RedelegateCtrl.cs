#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class RedelegateCtrl
    {
        internal static Redelegation? GetRedelegation(
            IWorldState states,
            Address redelegationAddress)
        {
            if (states.GetDPoSState(redelegationAddress) is { } value)
            {
                return new Redelegation(value);
            }

            return null;
        }

        internal static Redelegation? GetRedelegation(
            IWorldState states,
            Address delegatorAddress, Address srcValidatorAddress, Address dstValidatorAddress)
        {
            Address redelegationAddress = Redelegation.DeriveAddress(
                delegatorAddress, srcValidatorAddress, dstValidatorAddress);
            return GetRedelegation(states, redelegationAddress);
        }

        internal static Redelegation[] GetRedelegationsByDelegator(IWorldState worldState, Address delegatorAddress)
        {
            var redelegationList = new List<Redelegation>();
            var unbondingSet = UnbondingSetCtrl.GetUnbondingSet(worldState)!;
            foreach (var item in unbondingSet.RedelegationAddressSet)
            {
                if (GetRedelegation(worldState, item) is not { } redelegation)
                {
                    throw new InvalidOperationException("undelegation is null.");
                }

                if (redelegation.DelegatorAddress.Equals(delegatorAddress))
                {
                    redelegationList.Add(redelegation);
                }
            }

            return redelegationList.ToArray();
        }

        internal static RedelegationEntry? GetRedelegationEntry(IWorldState worldState, Address redelegationEntryAddress)
        {
            if (worldState.GetDPoSState(redelegationEntryAddress) is { } value)
            {
                return new RedelegationEntry(value);
            }

            return null;
        }

        internal static (IWorld, Redelegation) FetchRedelegation(
            IWorld states,
            Address delegatorAddress,
            Address srcValidatorAddress,
            Address dstValidatorAddress)
        {
            Address redelegationAddress = Redelegation.DeriveAddress(
                delegatorAddress, srcValidatorAddress, dstValidatorAddress);

            Redelegation redelegation;
            if (states.GetDPoSState(redelegationAddress) is { } value)
            {
                redelegation = new Redelegation(value);
            }
            else
            {
                redelegation = new Redelegation(
                    delegatorAddress,
                    srcValidatorAddress,
                    dstValidatorAddress);
                states = states.SetDPoSState(redelegation.Address, redelegation.Serialize());
            }

            return (states, redelegation);
        }

        internal static IWorld Execute(
            IWorld states,
            IActionContext ctx,
            Address delegatorAddress,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            FungibleAssetValue redelegatingShare,
            IImmutableSet<Currency> nativeTokens)
        {
            // TODO: Failure condition
            // 1. Delegation does not exist
            // 2. Source validator does not exist
            // 3. Target validator does not exist
            // 3. Delegation has less shares than worth of amount
            // 4. Existing redelegation has maximum entries
            // 5?. Delegation does not have sufficient token (fail or apply maximum)
            long blockHeight = ctx.BlockIndex;
            if (!redelegatingShare.Currency.Equals(Asset.Share))
            {
                throw new Exception.InvalidCurrencyException(Asset.Share, redelegatingShare.Currency);
            }

            if (ValidatorCtrl.GetValidator(states, srcValidatorAddress) is null)
            {
                throw new NullValidatorException(srcValidatorAddress);
            }

            if (ValidatorCtrl.GetValidator(states, dstValidatorAddress) is null)
            {
                throw new NullValidatorException(dstValidatorAddress);
            }

            Redelegation redelegation;
            (states, redelegation) = FetchRedelegation(
                states,
                delegatorAddress,
                srcValidatorAddress,
                dstValidatorAddress);

            if (redelegation.RedelegationEntryAddresses.Count
                >= Redelegation.MaximumRedelegationEntries)
            {
                throw new MaximumRedelegationEntriesException(
                    redelegation.Address, redelegation.RedelegationEntryAddresses.Count);
            }

            // Add new destination delegation, if not exist
            (states, _) = DelegateCtrl.FetchDelegation(
                states, delegatorAddress, dstValidatorAddress);
            FungibleAssetValue unbondingConsensusToken;
            FungibleAssetValue issuedShare;
            (states, unbondingConsensusToken) = Bond.Cancel(
                states,
                ctx,
                redelegatingShare,
                srcValidatorAddress,
                redelegation.SrcDelegationAddress,
                nativeTokens);
            (states, issuedShare) = Bond.Execute(
                states,
                ctx,
                unbondingConsensusToken,
                dstValidatorAddress,
                redelegation.DstDelegationAddress,
                nativeTokens);

            if (!(ValidatorCtrl.GetValidator(states, srcValidatorAddress) is { } srcValidator))
            {
                throw new NullValidatorException(srcValidatorAddress);
            }

            if (!(ValidatorCtrl.GetValidator(states, dstValidatorAddress) is { } dstValidator))
            {
                throw new NullValidatorException(dstValidatorAddress);
            }

            states = (srcValidator.Status, dstValidator.Status) switch
            {
                (BondingStatus.Bonded, BondingStatus.Unbonding) => states.TransferAsset(
                    ctx,
                    ReservedAddress.BondedPool,
                    ReservedAddress.UnbondedPool,
                    Asset.GovernanceFromConsensus(unbondingConsensusToken)),
                (BondingStatus.Bonded, BondingStatus.Unbonded) => states.TransferAsset(
                    ctx,
                    ReservedAddress.BondedPool,
                    ReservedAddress.UnbondedPool,
                    Asset.GovernanceFromConsensus(unbondingConsensusToken)),
                (BondingStatus.Unbonding, BondingStatus.Bonded) => states.TransferAsset(
                    ctx,
                    ReservedAddress.UnbondedPool,
                    ReservedAddress.BondedPool,
                    Asset.GovernanceFromConsensus(unbondingConsensusToken)),
                (BondingStatus.Unbonded, BondingStatus.Bonded) => states.TransferAsset(
                    ctx,
                    ReservedAddress.UnbondedPool,
                    ReservedAddress.BondedPool,
                    Asset.GovernanceFromConsensus(unbondingConsensusToken)),
                _ => states,
            };

            RedelegationEntry redelegationEntry = new RedelegationEntry(
                redelegation.Address,
                redelegatingShare,
                unbondingConsensusToken,
                issuedShare,
                redelegation.RedelegationEntryIndex,
                blockHeight);
            redelegation.RedelegationEntryAddresses.Add(
                redelegationEntry.Index, redelegationEntry.Address);
            redelegation.RedelegationEntryIndex += 1;

            states = states.SetDPoSState(redelegationEntry.Address, redelegationEntry.Serialize());
            states = states.SetDPoSState(redelegation.Address, redelegation.Serialize());

            states = UnbondingSetCtrl.AddRedelegationAddressSet(states, redelegation.Address);

            return states;
        }

        // This have to be called for each block,
        // to update staking status and generate block with updated validators.
        // Would it be better to declare this on out of this class?
        internal static IWorld Complete(
            IWorld states,
            IActionContext ctx,
            Address redelegationAddress)
        {
            long blockHeight = ctx.BlockIndex;
            if (!(GetRedelegation(states, redelegationAddress) is { } redelegation))
            {
                throw new NullRedelegationException(redelegationAddress);
            }

            List<long> completedIndices = new List<long>();
#pragma warning disable LAA1002
            foreach (KeyValuePair<long, Address> redelegationEntryAddressKV
                in redelegation.RedelegationEntryAddresses)
            {
                IValue? serializedRedelegationEntry
                    = states.GetDPoSState(redelegationEntryAddressKV.Value);
                if (serializedRedelegationEntry == null)
                {
                    continue;
                }

                RedelegationEntry redelegationEntry
                    = new RedelegationEntry(serializedRedelegationEntry);

                if (redelegationEntry.IsMatured(blockHeight))
                {
                    completedIndices.Add(redelegationEntry.Index);
                }
            }
#pragma warning restore LAA1002

            foreach (long index in completedIndices)
            {
                redelegation.RedelegationEntryAddresses.Remove(index);
            }

            states = states.SetDPoSState(redelegation.Address, redelegation.Serialize());

            if (redelegation.RedelegationEntryAddresses.Count == 0)
            {
                states = UnbondingSetCtrl.RemoveRedelegationAddressSet(
                    states, redelegation.Address);
            }

            return states;
        }
    }
}
