using System;
using Libplanet.Crypto;

namespace Lib9c.Action.Factory
{
    public static class ClaimStakeRewardFactory
    {
        // NOTE: This method does not return a type of `ClaimStakeReward1`.
        //       Because it is not obsoleted yet.
        public static IClaimStakeReward CreateByBlockIndex(
            long blockIndex,
            Address avatarAddress)
        {
            if (blockIndex > ClaimStakeReward8.ObsoleteBlockIndex)
            {
                return new ClaimStakeReward(avatarAddress);
            }

            throw new ArgumentOutOfRangeException(
                $"Invalid block index: {blockIndex}");
        }

        public static IClaimStakeReward CreateByVersion(
            int version,
            Address avatarAddress) => version switch
        {
            9 => new ClaimStakeReward(avatarAddress),
            _ => throw new ArgumentOutOfRangeException(
                $"Invalid version: {version}"),
        };
    }
}
