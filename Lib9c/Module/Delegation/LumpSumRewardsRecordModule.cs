#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.Module.Delegation
{
    public static class LumpSumRewardsRecordModule
    {
        public static LumpSumRewardsRecord GetCurrentLumpSumRewardsRecord(
            this IWorldState world, IDelegatee delegatee)
            => GetLumpSumRewardsRecord(world, delegatee.CurrentLumpSumRewardsRecordAddress());

        public static LumpSumRewardsRecord GetLumpSumRewardsRecord(
            this IWorldState world, IDelegatee delegatee, long height)
            => GetLumpSumRewardsRecord(world, delegatee.LumpSumRewardsRecordAddress(height));

        public static LumpSumRewardsRecord GetLumpSumRewardsRecord(
            this IWorldState world, Address address)
            => TryGetLumpSumRewardsRecord(world, address, out var lumpSumRewardsRecord)
                ? lumpSumRewardsRecord!
                : throw new InvalidOperationException("Failed to get LumpSumRewardsRecord.");

        public static bool TryGetLumpSumRewardsRecord(
            this IWorldState world, Address address, out LumpSumRewardsRecord? lumpSumRewardsRecord)
        {
            try
            {
                var value = world.GetAccountState(Addresses.LumpSumRewardsRecord).GetState(address);
                if (!(value is List list))
                {
                    lumpSumRewardsRecord = null;
                    return false;
                }

                lumpSumRewardsRecord = new LumpSumRewardsRecord(address, list);
                return true;
            }
            catch
            {
                lumpSumRewardsRecord = null;
                return false;
            }
        }
    }
}
