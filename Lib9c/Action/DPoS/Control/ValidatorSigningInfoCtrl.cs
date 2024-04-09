#nullable enable
using Nekoyume.Action.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Exception;
using System;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class ValidatorSigningInfoCtrl
    {
        internal static ValidatorSigningInfo? GetSigningInfo(IWorldState states, Address validatorAddress)
        {
            var address = ValidatorSigningInfo.DeriveAddress(validatorAddress);
            if (states.GetDPoSState(address) is { } value)
            {
                return new ValidatorSigningInfo(value);
            }

            return null;
        }

        internal static IWorld SetSigningInfo(IWorld world, ValidatorSigningInfo signingInfo)
        {
            var address = ValidatorSigningInfo.DeriveAddress(signingInfo.Address);
            var value = signingInfo.Serialize();
            return world.SetDPoSState(address, value);
        }


        internal static IWorld Tombstone(IWorld world, Address validatorAddress)
        {
            if (!(GetSigningInfo(world, validatorAddress) is { } signInfo))
            {
                throw new NullValidatorException(validatorAddress);
            }
            if (signInfo.Tombstoned)
            {
                throw new InvalidOperationException();
            }

            signInfo.Tombstoned = true;
            return SetSigningInfo(world, signInfo);
        }

        internal static bool IsTombstoned(IWorld world, Address validatorAddress)
        {
            if (!(GetSigningInfo(world, validatorAddress) is { } signInfo))
            {
                throw new NullValidatorException(validatorAddress);
            }

            return signInfo.Tombstoned;
        }

        internal static IWorld JailUntil(IWorld world, Address validatorAddress, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(blockHeight),
                    message: "blockHeight must be greater than or equal to 0.");
            }

            if (!(GetSigningInfo(world, validatorAddress) is { } signInfo))
            {
                throw new NullValidatorException(validatorAddress);
            }

            signInfo.JailedUntil = blockHeight;
            return SetSigningInfo(world, signInfo);
        }
    }
}
