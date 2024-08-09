using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class LumpSumRewardRecordCtrl
    {
        internal static (IWorld, FungibleAssetValue) Reward(
            this IWorld states,
            IActionContext context,
            Delegation delegation,
            Validator validator,
            Currency nativeToken)
        {
            FungibleAssetValue reward = nativeToken * 0;
            long? linkedStartHeight = null;
            var lumpSumRewardsRecords = GetLumpSumRewardsRecords(
                states, validator.Address, delegation.LatestDistributeHeight);

            foreach (LumpSumRewardsRecord record in lumpSumRewardsRecords)
            {
                if (!(record.StartHeight is long startHeight))
                {
                    throw new ArgumentException("lump sum reward record wasn't started.");
                }

                if (linkedStartHeight is long startHeightFromHigher
                    && startHeightFromHigher != startHeight)
                {
                    throw new ArgumentException("lump sum reward record was started.");
                }

                reward += record.RewardsDuringPeriod(
                    states.GetBalance(delegation.Address, Asset.Share));
                linkedStartHeight = record.LastStartHeight;

                if (linkedStartHeight == -1)
                {
                    break;
                }
            }

            states = StartNew(states, context, nativeToken, validator.Address, validator.DelegatorShares);

            return (states, reward);
        }
     
        internal static IWorld AddRewardToWipRecord(
            this IWorld states, Address validatorAddress, FungibleAssetValue reward)
        {
            var wipRecord =
                GetWipLumpSumRewardsRecord(states, validatorAddress)
                .AddLumpSumReward(reward);
            return states.SetLumpSumRewardsRecord(wipRecord);
        }
        
        private static IWorld StartNew(
            this IWorld states,
            IActionContext context,
            Currency nativeToken,
            Address validtorAddress,
            FungibleAssetValue totalShares)
        {
            var wipRecordToSave = GetWipLumpSumRewardsRecord(states, validtorAddress);
            var newRecord = new LumpSumRewardsRecord(
                WipAddress(validtorAddress),
                totalShares,
                wipRecordToSave.StartHeight,
                nativeToken * 0,
                context.BlockIndex);

            states = states.SaveWipLumpSumRewardsRecord(validtorAddress, wipRecordToSave);
            states = states.SetLumpSumRewardsRecord(newRecord);
            return states;
        }

        private static List<LumpSumRewardsRecord> GetLumpSumRewardsRecords(
            this IWorldState world, Address validatorAddress, long startHeight)
        {
            var records = new List<LumpSumRewardsRecord>();
            LumpSumRewardsRecord record = world.GetWipLumpSumRewardsRecord(validatorAddress);
            records.Add(record);
            long height = record.LastStartHeight;

            while (height >= startHeight)
            {
                record = world.GetLumpSumRewardsRecord(validatorAddress, height);
                records.Add(record);
                height = record.LastStartHeight;
            }

            return records;
        }

        private static IWorld SetLumpSumRewardsRecord(
            this IWorld states, LumpSumRewardsRecord record)
            => states.SetDPoSState(record.Address, record.Bencoded);

        private static IWorld SaveWipLumpSumRewardsRecord(
            this IWorld states, Address validatorAddress, LumpSumRewardsRecord record)
            => states.SetDPoSState(DeriveAddress(validatorAddress, record.StartHeight), record.Bencoded);

        
        private static LumpSumRewardsRecord GetLumpSumRewardsRecord(
            this IWorldState states, Address validatorAddress, long height)
            => GetLumpSumRewardRecordByAddress(states, DeriveAddress(validatorAddress, height));

        private static LumpSumRewardsRecord GetWipLumpSumRewardsRecord(
            this IWorldState states, Address validatorAddress)
            => GetLumpSumRewardRecordByAddress(states, WipAddress(validatorAddress));

        private static LumpSumRewardsRecord GetLumpSumRewardRecordByAddress(
            this IWorldState states, Address address)
        {
            if (states.GetDPoSState(address) is { } value)
            {
                return new LumpSumRewardsRecord(address, value);
            }

            return null;
        }

        private static Address WipAddress(Address ValidatorAddress) => DeriveAddress(ValidatorAddress, -1);

        private static Address DeriveAddress(Address validatorAddress, long height)
        {
            byte[] hashed;
            using (var hmac = new HMACSHA1(validatorAddress.ToByteArray()))
            {
                hashed = hmac.ComputeHash(
                    BitConverter.GetBytes(height).ToArray());
            }

            return new Address(hashed);
        }
    }
}
