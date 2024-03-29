#nullable enable
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
    internal static class UndelegateCtrl
    {
        internal static Undelegation? GetUndelegation(
            IWorldState state,
            Address undelegationAddress)
        {
            if (state.GetDPoSState(undelegationAddress) is { } value)
            {
                return new Undelegation(value);
            }

            return null;
        }

        internal static (IWorld, Undelegation) FetchUndelegation(
            IWorld state,
            Address delegatorAddress,
            Address validatorAddress)
        {
            Address undelegationAddress = Undelegation.DeriveAddress(
                delegatorAddress, validatorAddress);
            Undelegation undelegation;
            if (state.GetDPoSState(undelegationAddress) is { } value)
            {
                undelegation = new Undelegation(value);
            }
            else
            {
                undelegation = new Undelegation(delegatorAddress, validatorAddress);
                state = state.SetDPoSState(undelegation.Address, undelegation.Serialize());
            }

            return (state, undelegation);
        }

        internal static IWorld Execute(
            IWorld states,
            IActionContext ctx,
            Address delegatorAddress,
            Address validatorAddress,
            FungibleAssetValue share,
            IImmutableSet<Currency> nativeTokens)
        {
            // TODO: Failure condition
            // 1. Delegation does not exist
            // 2. Validator does not exist
            // 3. Delegation has less shares than worth of amount
            // 4. Existing undelegation has maximum entries

            long blockHeight = ctx.BlockIndex;

            // Currency check
            if (!share.Currency.Equals(Asset.Share))
            {
                throw new Exception.InvalidCurrencyException(Asset.Share, share.Currency);
            }

            Undelegation undelegation;
            (states, undelegation) = FetchUndelegation(states, delegatorAddress, validatorAddress);

            if (undelegation.UndelegationEntryAddresses.Count
                >= Undelegation.MaximumUndelegationEntries)
            {
                throw new MaximumUndelegationEntriesException(
                    undelegation.Address, undelegation.UndelegationEntryAddresses.Count);
            }

            // Validator loading
            if (!(ValidatorCtrl.GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            // Unbonding
            FungibleAssetValue unbondingConsensusToken;
            (states, unbondingConsensusToken) = Bond.Cancel(
                states,
                ctx,
                share,
                undelegation.ValidatorAddress,
                undelegation.DelegationAddress,
                nativeTokens);

            // Governance token pool transfer
            if (validator.Status == BondingStatus.Bonded)
            {
                states = states.TransferAsset(
                        ctx,
                        ReservedAddress.BondedPool,
                        ReservedAddress.UnbondedPool,
                        Asset.GovernanceFromConsensus(unbondingConsensusToken));
            }

            // Entry register
            UndelegationEntry undelegationEntry = new UndelegationEntry(
                    undelegation.Address,
                    unbondingConsensusToken,
                    undelegation.UndelegationEntryIndex,
                    blockHeight);
            undelegation.UndelegationEntryAddresses.Add(
                undelegationEntry.Index, undelegationEntry.Address);
            undelegation.UndelegationEntryIndex += 1;

            // TODO: Global state indexing is also needed
            states = states.SetDPoSState(undelegationEntry.Address, undelegationEntry.Serialize());

            states = states.SetDPoSState(undelegation.Address, undelegation.Serialize());

            states = UnbondingSetCtrl.AddUndelegationAddressSet(states, undelegation.Address);

            return states;
        }

        internal static IWorld Cancel(
            IWorld states,
            IActionContext ctx,
            Address undelegationAddress,
            FungibleAssetValue cancelledConsensusToken,
            IImmutableSet<Currency> nativeTokens)
        {
            long blockHeight = ctx.BlockIndex;

            // Currency check
            if (!cancelledConsensusToken.Currency.Equals(Asset.ConsensusToken))
            {
                throw new Exception.InvalidCurrencyException(
                    Asset.ConsensusToken, cancelledConsensusToken.Currency);
            }

            if (!(GetUndelegation(states, undelegationAddress) is { } undelegation))
            {
                throw new NullUndelegationException(undelegationAddress);
            }

            // Validator loading
            if (!(ValidatorCtrl.GetValidator(
                states, undelegation.ValidatorAddress) is { } validator))
            {
                throw new NullValidatorException(undelegation.ValidatorAddress);
            }

            // Copy of cancelling amount
            FungibleAssetValue cancellingConsensusToken =
                new FungibleAssetValue(
                    Asset.ConsensusToken,
                    cancelledConsensusToken.MajorUnit,
                    cancelledConsensusToken.MinorUnit);

            // Iterate all entries
            List<long> undelegationEntryIndices = new List<long>();
#pragma warning disable LAA1002
            foreach (KeyValuePair<long, Address> undelegationEntryAddressKV
                in undelegation.UndelegationEntryAddresses)
            {
                // Load entry
                IValue? serializedUndelegationEntry
                    = states.GetDPoSState(undelegationEntryAddressKV.Value);

                // Skip empty entry
                if (serializedUndelegationEntry == null)
                {
                    continue;
                }

                UndelegationEntry undelegationEntry
                    = new UndelegationEntry(serializedUndelegationEntry);

                // Double check for unbonded entry
                if (blockHeight >= undelegationEntry.CompletionBlockHeight)
                {
                    throw new PostmatureUndelegationEntryException(
                        blockHeight,
                        undelegationEntry.CompletionBlockHeight,
                        undelegationEntry.Address);
                }

                // Check if cancelledConsensusToken is less than total undelegation
                if (cancellingConsensusToken.RawValue < 0)
                {
                    throw new InsufficientFungibleAssetValueException(
                        cancelledConsensusToken,
                        cancelledConsensusToken + cancellingConsensusToken,
                        $"Undelegation {undelegationAddress} has insufficient consensusToken");
                }

                // Apply unbonding
                if (cancellingConsensusToken < undelegationEntry.UnbondingConsensusToken)
                {
                    undelegationEntry.UnbondingConsensusToken -= cancellingConsensusToken;
                    states = states.SetDPoSState(
                        undelegationEntry.Address, undelegationEntry.Serialize());
                    break;
                }

                // If cancelling amount is more than current entry, save and skip
                else
                {
                    cancellingConsensusToken -= undelegationEntry.UnbondingConsensusToken;
                    undelegationEntryIndices.Add(undelegationEntry.Index);
                }
            }
#pragma warning restore LAA1002

            (states, _) = Bond.Execute(
                states,
                ctx,
                cancelledConsensusToken,
                undelegation.ValidatorAddress,
                undelegation.DelegationAddress,
                nativeTokens);

            if (validator.Status == BondingStatus.Bonded)
            {
                states = states.TransferAsset(
                    ctx,
                    ReservedAddress.UnbondedPool,
                    ReservedAddress.BondedPool,
                    Asset.GovernanceFromConsensus(cancelledConsensusToken));
            }

            undelegationEntryIndices.ForEach(
                idx => undelegation.UndelegationEntryAddresses.Remove(idx));

            states = states.SetDPoSState(undelegation.Address, undelegation.Serialize());

            if (undelegation.UndelegationEntryAddresses.Count == 0)
            {
                states = UnbondingSetCtrl.RemoveUndelegationAddressSet(
                    states, undelegation.Address);
            }

            return states;
        }

        // This have to be called for each block,
        // to update staking status and generate block with updated validators.
        // Would it be better to declare this on out of this class?
        internal static IWorld Complete(
            IWorld states,
            IActionContext ctx,
            Address undelegationAddress)
        {
            long blockHeight = ctx.BlockIndex;
            if (!(GetUndelegation(states, undelegationAddress) is { } undelegation))
            {
                throw new NullUndelegationException(undelegationAddress);
            }

            List<long> completedIndices = new List<long>();

            // Iterate all entries
#pragma warning disable LAA1002
            foreach (KeyValuePair<long, Address> undelegationEntryAddressKV
                in undelegation.UndelegationEntryAddresses)
            {
                // Load entry
                IValue? serializedUndelegationEntry
                    = states.GetDPoSState(undelegationEntryAddressKV.Value);

                // Skip empty entry
                if (serializedUndelegationEntry == null)
                {
                    continue;
                }

                UndelegationEntry undelegationEntry
                    = new UndelegationEntry(serializedUndelegationEntry);

                // Complete matured entries
                if (undelegationEntry.IsMatured(blockHeight))
                {
                    // Pay back governance token to delegator
                    states = states.TransferAsset(
                        ctx,
                        ReservedAddress.UnbondedPool,
                        undelegation.DelegatorAddress,
                        Asset.GovernanceFromConsensus(undelegationEntry.UnbondingConsensusToken));

                    // Remove entry
                    completedIndices.Add(undelegationEntry.Index);
                }
            }
#pragma warning restore LAA1002

            foreach (long index in completedIndices)
            {
                undelegation.UndelegationEntryAddresses.Remove(index);
            }

            states = states.SetDPoSState(undelegation.Address, undelegation.Serialize());

            if (undelegation.UndelegationEntryAddresses.Count == 0)
            {
                states = UnbondingSetCtrl.RemoveUndelegationAddressSet(
                    states, undelegation.Address);
            }

            return states;
        }
    }
}
