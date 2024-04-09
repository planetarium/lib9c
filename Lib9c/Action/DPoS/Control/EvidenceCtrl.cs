#nullable enable
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using System.Collections.Immutable;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class EvidenceCtrl
    {
        internal static Evidence? GetEvidence(IWorldState states, Address validatorAddress)
        {
            var address = Evidence.DeriveAddress(validatorAddress);
            if (states.GetDPoSState(address) is { } value)
            {
                return new Evidence(value);
            }

            return null;
        }

        internal static IWorld SetEvidence(IWorld world, Evidence evidence)
        {
            var address = Evidence.DeriveAddress(evidence.Address);
            var value = evidence.Serialize();
            return world.SetDPoSState(address, value);
        }

        internal static IWorld Remove(IWorld world, Evidence evidence)
        {
            var address = Evidence.DeriveAddress(evidence.Address);
            return world.RemoveDPoSState(address);
        }

        internal static IWorld Execute(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress,
            Evidence evidence,
            IImmutableSet<Currency> nativeTokens)
        {
            if (!(ValidatorCtrl.GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (validator.Status == BondingStatus.Unbonded)
            {
                return world;
            }

            var blockHeight = actionContext.BlockIndex;
            var infractionHeight = evidence.Height;
            var ageBlocks = blockHeight - infractionHeight;

            if (ageBlocks > Environment.MaxAgeNumBlocks)
            {
                return world;
            }

            if (ValidatorCtrl.IsTombstoned(world, validatorAddress))
            {
                return world;
            }

            var distributionHeight = infractionHeight - Environment.ValidatorUpdateDelay;
            var slashFractionDoubleSign = Environment.SlashFractionDoubleSign;

            world = SlashCtrl.SlashWithInfractionReason(
                world,
                actionContext,
                validatorAddress,
                distributionHeight,
                evidence.Power,
                slashFractionDoubleSign,
                Infraction.DoubleSign,
                nativeTokens
            );

            if (!validator.Jailed)
            {
                world = ValidatorCtrl.Jail(world, validatorAddress);
            }

            world = ValidatorCtrl.JailUntil(
                world: world,
                validatorAddress: validatorAddress,
                blockHeight: long.MaxValue);
            world = ValidatorCtrl.Tombstone(world, validatorAddress);
            world = Remove(world, evidence);
            return world;
        }
    }
}
