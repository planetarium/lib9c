#nullable enable
using System.Numerics;
using Bencodex.Types;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class SlashCtrl
    {
        public static IWorld Slash(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress,
            long infractionHeight,
            BigInteger power,
            BigInteger slashFactor,
            IImmutableSet<Currency> nativeTokens)
        {
            if (slashFactor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slashFactor), "Slash factor must be greater than or equal to 0.");
            }

            if (!(ValidatorCtrl.GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            var amount = FungibleAssetValue.FromRawValue(Asset.ConsensusToken, power);
            var (slashAmount, slashRemainder) = amount.DivRem(slashFactor);

            if (validator.Status == BondingStatus.Unbonded)
            {
                throw new InvalidOperationException("should not be slashing unbonded validator");
            }

            var remainingSlashAmount = slashAmount;

            if (infractionHeight > actionContext.BlockIndex)
            {
                throw new ArgumentOutOfRangeException(
                    "impossible attempt to slash future infraction",
                    nameof(infractionHeight));
            }
            else if (infractionHeight < actionContext.BlockIndex)
            {
                world = SlashUndelegations(world, actionContext, validatorAddress,
                    infractionHeight, slashFactor, ref remainingSlashAmount);
                world = SlashRedelegations(world, actionContext, validatorAddress,
                    infractionHeight, slashFactor, nativeTokens, ref remainingSlashAmount);
            }

            if (ValidatorCtrl.ConsensusTokenFromShare(world, validatorAddress, validator.DelegatorShares) is not { } consensusToken)
            {
                throw new InvalidOperationException();
            }

            var tokensToBurn = Min(remainingSlashAmount, consensusToken);

            if (tokensToBurn.RawValue == 0)
            {
                return world;
            }

            world = ValidatorCtrl.RemoveValidatorTokens(world, actionContext, validatorAddress, tokensToBurn);

            world = validator.Status switch
            {
                BondingStatus.Bonded
                    => BurnBondedTokens(world, actionContext, amount: tokensToBurn),
                BondingStatus.Unbonding or BondingStatus.Unbonded
                    => BurnNotBondedTokens(world, actionContext, amount: tokensToBurn),
                _
                    => throw new InvalidOperationException("Invalid validator status"),
            };

            return world;
        }

        public static IWorld SlashWithInfractionReason(
           IWorld world,
           IActionContext actionContext,
           Address validatorAddress,
           long infractionHeight,
           BigInteger power,
           BigInteger slashFractionDowntime,
           Infraction infraction,
           IImmutableSet<Currency> nativeTokens)
        {
            return Slash(world, actionContext, validatorAddress, infractionHeight, power, slashFractionDowntime, nativeTokens);
        }

        public static IWorld Execute(
            IWorld world,
            IActionContext actionContext,
            Address operatorAddress,
            BigInteger power,
            bool signed,
            IImmutableSet<Currency> nativeTokens)
        {
            var height = actionContext.BlockIndex;
            var validatorAddress = Validator.DeriveAddress(operatorAddress);

            if (!(ValidatorCtrl.GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (validator.Jailed)
            {
                return world;
            }

            if (!(ValidatorSigningInfoCtrl.GetSigningInfo(world, validatorAddress) is { } signInfo))
            {
                throw new NullValidatorException(validatorAddress);
            }

            var signedBlocksWindow = Environment.SignedBlocksWindow;

            var index = signInfo.IndexOffset % signedBlocksWindow;
            signInfo.IndexOffset++;

            var previous = GetMissedBlockBitmapValue(world, validatorAddress, index);
            var missed = signed;
            if (!previous && missed)
            {
                SetMissedBlockBitmapValue(world, validatorAddress, index, true);
                signInfo.MissedBlocksCounter++;
            }
            else if (previous && !missed)
            {
                SetMissedBlockBitmapValue(world, validatorAddress, index, false);
                signInfo.MissedBlocksCounter--;
            }

            var minSignedPerWindow = Environment.MinSignedPerWindow;
            var minHeight = signInfo.StartHeight + signedBlocksWindow;
            var maxMissed = signedBlocksWindow - minSignedPerWindow;

            if (height > minHeight && signInfo.MissedBlocksCounter > maxMissed)
            {
                if (!validator.Jailed)
                {
                    var distributionHeight = height - Environment.ValidatorUpdateDelay - 1;
                    var slashFractionDowntime = Environment.SlashFractionDowntime;
                    world = SlashCtrl.SlashWithInfractionReason(
                        world,
                        actionContext,
                        validatorAddress,
                        distributionHeight,
                        power,
                        slashFractionDowntime,
                        Infraction.Downtime,
                        nativeTokens
                    );
                    world = ValidatorCtrl.Jail(world, validatorAddress);

                    var downtimeJailDur = Environment.DowntimeJailDuration;
                    // signInfo.JailedUntil = blockContext.Timestamp + downtimeJailDur;
                    signInfo.MissedBlocksCounter = 0;
                    signInfo.IndexOffset = 0;
                    DeleteMissedBlockBitmap(world);
                }
            }

            return ValidatorSigningInfoCtrl.SetSigningInfo(world, signInfo);
        }

        public static IWorld Unjail(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress
        )
        {
            if (!(ValidatorCtrl.GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (!validator.Jailed)
            {
                throw new InvalidOperationException("validator is not jailed");
            }

            var signingInfo = ValidatorSigningInfoCtrl.GetSigningInfo(world, validatorAddress);
            if (signingInfo is null)
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (signingInfo.Tombstoned)
            {
                throw new InvalidOperationException("validator is tombstoned");
            }

            if (actionContext.BlockIndex < signingInfo.JailedUntil)
            {
                throw new InvalidOperationException("validator is still jailed");
            }

            var consensusToken = world.GetBalance(validatorAddress, Asset.ConsensusToken);
            if (consensusToken < Validator.MinSelfDelegation)
            {
                throw new InvalidOperationException("validator has insufficient self-delegation");
            }

            var operatorAddress = validator.OperatorAddress;
            var delegationAddress = Delegation.DeriveAddress(operatorAddress, validator.Address);
            if (!(DelegateCtrl.GetDelegation(world, delegationAddress) is { }))
            {
                throw new NullDelegationException(delegationAddress);
            }

            return ValidatorCtrl.Unjail(world, validatorAddress);
        }

        private static IWorld SlashUnbondingDelegation(
            IWorld world,
            IActionContext actionContext,
            Undelegation undelegation,
            long infractionHeight,
            BigInteger slashFactor,
            out FungibleAssetValue amountSlashed)
        {
            var totalSlashAmount = new FungibleAssetValue(Asset.ConsensusToken);
            var burnedAmount = new FungibleAssetValue(Asset.ConsensusToken);

#pragma warning disable LAA1002
            foreach (var (index, entryAddress) in undelegation.UndelegationEntryAddresses)
            {
                var entryValue = world.GetDPoSState(entryAddress)!;
                var entry = new UndelegationEntry(entryValue);

                if (entry.CreationHeight < infractionHeight)
                {
                    continue;
                }
                if (entry.IsMatured(infractionHeight))
                {
                    continue;
                }

                var (slashAmount, slashRemainder) = entry.InitialConsensusToken.DivRem(slashFactor);
                totalSlashAmount += slashAmount;

                var unbondingSlashAmount = Min(slashAmount, entry.UnbondingConsensusToken);
                burnedAmount += unbondingSlashAmount;
                entry.UnbondingConsensusToken -= unbondingSlashAmount;
                world = world.SetDPoSState(entry.Address, entry.Serialize());
            }
#pragma warning restore LAA1002

            if (burnedAmount.RawValue > 0)
            {
                world = BurnNotBondedTokens(world, actionContext, amount: burnedAmount);
            }

            amountSlashed = burnedAmount;
            return world;
        }

        private static IWorld SlashRedelegation(
            IWorld world,
            IActionContext actionContext,
            Redelegation redelegation,
            long infractionHeight,
            BigInteger slashFactor,
            IImmutableSet<Currency> nativeTokens,
            out FungibleAssetValue amountSlashed)
        {
            var totalSlashAmount = new FungibleAssetValue(Asset.ConsensusToken);
            var bondedBurnedAmount = new FungibleAssetValue(Asset.ConsensusToken);
            var notBondedBurnedAmount = new FungibleAssetValue(Asset.ConsensusToken);

            var valDstAddr = redelegation.DstValidatorAddress;
            var delegatorAddress = redelegation.DelegatorAddress;

#pragma warning disable LAA1002
            foreach (var (index, entryAddress) in redelegation.RedelegationEntryAddresses)
            {
                var entryValue = world.GetDPoSState(entryAddress)!;
                var entry = new RedelegationEntry(entryValue);

                if (entry.CreationHeight < infractionHeight)
                {
                    continue;
                }
                if (entry.IsMatured(infractionHeight))
                {
                    continue;
                }

                var (slashAmount, _) = entry.InitialConsensusToken.DivRem(slashFactor);
                totalSlashAmount += slashAmount;

                var (sharesToUnbond, _) = entry.RedelegatingShare.DivRem(slashFactor);
                if (sharesToUnbond.RawValue == 0)
                {
                    continue;
                }

                var delegationAddress = Delegation.DeriveAddress(delegatorAddress, valDstAddr);
                var delegation = DelegateCtrl.GetDelegation(world, delegationAddress)!;

                if (sharesToUnbond > delegation.GetShares(world))
                {
                    sharesToUnbond = delegation.GetShares(world);
                }

                FungibleAssetValue tokensToBurn;
                (world, tokensToBurn) = Bond.Cancel(world, actionContext, sharesToUnbond, valDstAddr, delegationAddress, nativeTokens);

                if (ValidatorCtrl.GetValidator(world, valDstAddr) is not { } dstValidator)
                {
                    throw new NullValidatorException(valDstAddr);
                }

                if (dstValidator.Status == BondingStatus.Bonded)
                {
                    bondedBurnedAmount += tokensToBurn;
                }
                else if (dstValidator.Status == BondingStatus.Unbonding || dstValidator.Status == BondingStatus.Unbonded)
                {
                    notBondedBurnedAmount += tokensToBurn;
                }
                else
                {
                    throw new InvalidOperationException("unknown validator status");
                }

                if (bondedBurnedAmount.RawValue > 0)
                {
                    world = BurnBondedTokens(world, actionContext, amount: bondedBurnedAmount);
                }

                if (notBondedBurnedAmount.RawValue > 0)
                {
                    world = BurnNotBondedTokens(world, actionContext, amount: notBondedBurnedAmount);
                }
            }
#pragma warning restore LAA1002

            amountSlashed = totalSlashAmount;
            return world;
        }

        private static FungibleAssetValue Min(FungibleAssetValue f1, FungibleAssetValue f2)
        {
            return f1 < f2 ? f1 : f2;
        }

        private static IWorld SlashUndelegations(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress,
            long infractionHeight,
            BigInteger slashFactor,
            ref FungibleAssetValue remainingSlashAmount)
        {
            var unbondingSet = UnbondingSetCtrl.GetUnbondingSet(world)!;
            foreach (var item in unbondingSet.UndelegationAddressSet)
            {
                var undelegation = UndelegateCtrl.GetUndelegation(world, item)!;
                if (undelegation.ValidatorAddress == validatorAddress)
                {
                    world = SlashUnbondingDelegation(world, actionContext, undelegation, infractionHeight, slashFactor, out var amountSlashed);
                    remainingSlashAmount -= amountSlashed;
                }
            }

            return world;
        }

        private static IWorld SlashRedelegations(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress,
            long infractionHeight,
            BigInteger slashFactor,
            IImmutableSet<Currency> nativeTokens,
            ref FungibleAssetValue remainingSlashAmount)
        {
            var unbondingSet = UnbondingSetCtrl.GetUnbondingSet(world)!;
            foreach (var item in unbondingSet.RedelegationAddressSet)
            {
                var redelegation = RedelegateCtrl.GetRedelegation(world, item)!;
                if (redelegation.SrcValidatorAddress == validatorAddress)
                {
                    world = SlashRedelegation(world, actionContext, redelegation, infractionHeight, slashFactor, nativeTokens, out var amountSlashed);
                    remainingSlashAmount -= amountSlashed;
                }
            }

            return world;
        }

        private static bool GetMissedBlockBitmapValue(
            IWorld world,
            Address validatorAddress,
            long index)
        {
            var chunkIndex = index / Environment.MissedBlockBitmapChunkSize;
            var chunk = GetMissedBlockBitmapChunk(world, validatorAddress, chunkIndex);
            if (chunk == null)
            {
                return false;
            }

            var bitIndex = index % Environment.MissedBlockBitmapChunkSize;
            return chunk[bitIndex] == 1;
        }

        private static IWorld SetMissedBlockBitmapValue(
            IWorld world,
            Address validatorAddress,
            long index,
            bool missed)
        {
            var chunkIndex = index / Environment.MissedBlockBitmapChunkSize;
            var chunk = GetMissedBlockBitmapChunk(world, validatorAddress, chunkIndex) ?? new byte[Environment.MissedBlockBitmapChunkSize];
            var bitIndex = index % Environment.MissedBlockBitmapChunkSize;
            chunk[bitIndex] = (byte)(missed ? 1 : 0);

            return SetMissedBlockBitmapChunk(world, validatorAddress, chunkIndex, chunk);
        }

        private static IWorld DeleteMissedBlockBitmap(IWorld world)
        {
            return world;
        }

        private static byte[]? GetMissedBlockBitmapChunk(
            IWorld world,
            Address validatorAddress,
            long chunkIndex)
        {
            var address = validatorAddress.Derive($"{chunkIndex}");
            if (world.GetDPoSState(address) is Binary binary)
            {
                return binary.ByteArray.ToArray();
            }
            return null;
        }

        private static IWorld SetMissedBlockBitmapChunk(
            IWorld world,
            Address validatorAddress,
            long chunkIndex,
            byte[] chunk)
        {
            var address = validatorAddress.Derive($"{chunkIndex}");
            return world.SetDPoSState(address, new Binary(chunk.ToArray()));
        }

        private static IWorld BurnBondedTokens(
            IWorld world,
            IActionContext actionContext,
            FungibleAssetValue amount)
        {
            if (!amount.Currency.Equals(Asset.ConsensusToken))
            {
                throw new Exception.InvalidCurrencyException(Asset.ConsensusToken, amount.Currency);
            }

            var tokensToBurn = Asset.GovernanceFromConsensus(amount);
            return world.TransferAsset(actionContext, ReservedAddress.BondedPool, ReservedAddress.CommunityPool, tokensToBurn);
        }

        private static IWorld BurnNotBondedTokens(
            IWorld world,
            IActionContext actionContext,
            FungibleAssetValue amount)
        {
            if (!amount.Currency.Equals(Asset.ConsensusToken))
            {
                throw new Exception.InvalidCurrencyException(Asset.ConsensusToken, amount.Currency);
            }

            var tokensToBurn = Asset.GovernanceFromConsensus(amount);
            return world.TransferAsset(actionContext, ReservedAddress.UnbondedPool, ReservedAddress.CommunityPool, tokensToBurn);
        }
    }
}
