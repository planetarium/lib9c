#nullable enable
using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.Extensions;

namespace Nekoyume.Module.Delegation
{
    public static class LumpSumRewardsRecordModule
    {
        public static List<LumpSumRewardsRecord> GetLumpSumRewardsRecords(
            this IWorldState world, IDelegatee delegatee, long height, long startHeight)
        {
            var records = new List<LumpSumRewardsRecord>();
            LumpSumRewardsRecord record;
            while (height >= startHeight)
            {
                record = world.GetLumpSumRewardsRecord(delegatee, height);
                records.Add(record);
                height = record.LastStartHeight;
            }

            return records;
        }

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

        public static IWorld SetLumpSumRewardsRecord(this IWorld world, LumpSumRewardsRecord lumpSumRewardsRecord)
            => world.MutateAccount(
                Addresses.LumpSumRewardsRecord,
                account => account.SetState(lumpSumRewardsRecord.Address, lumpSumRewardsRecord.Bencoded));
    }
}
