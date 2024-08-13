using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Bencodex;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class LumpSumRewardRecordCtrl
    {
        internal static FungibleAssetValue Reward(
            this IWorld states,
            IActionContext context,
            Delegation delegation,
            Validator validator,
            Currency nativeToken)
        {
            FungibleAssetValue reward = nativeToken * 0;
            long? linkedStartHeight = null;
            var lumpSumRewardsRecords = GetLumpSumRewardsRecords(
                states, validator.Address, nativeToken, delegation.LatestDistributeHeight);

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

            return reward;
        }
     
        internal static IWorld AddRewardToWipRecord(
            this IWorld states, Address validatorAddress, Currency currency, FungibleAssetValue reward)
        {
            var wipRecord =
                GetWipLumpSumRewardsRecord(states, validatorAddress, currency)
                .AddLumpSumReward(reward);
            return states.SetLumpSumRewardsRecord(wipRecord);
        }
        
        public static IWorld StartNew(
            this IWorld states,
            IActionContext context,
            Currency currency,
            Address validtorAddress,
            FungibleAssetValue totalShares)
        {
            long lastStartHeight;
            if (GetWipLumpSumRewardsRecord(states, validtorAddress, currency) is { } wipRecordToSave)
            {
                lastStartHeight = wipRecordToSave.StartHeight == context.BlockIndex
                    ? wipRecordToSave.LastStartHeight
                    : wipRecordToSave.StartHeight;
                states = states.SaveWipLumpSumRewardsRecord(validtorAddress, currency, wipRecordToSave);
            }
            else
            {
                lastStartHeight = -1;
            }

            var newRecord = new LumpSumRewardsRecord(
                WipAddress(validtorAddress, currency),
                totalShares,
                lastStartHeight,
                currency * 0,
                context.BlockIndex);

            states = states.SetLumpSumRewardsRecord(newRecord);
            return states;
        }

        private static List<LumpSumRewardsRecord> GetLumpSumRewardsRecords(
            this IWorldState world, Address validatorAddress, Currency currency, long startHeight)
        {
            var records = new List<LumpSumRewardsRecord>();
            if (startHeight == -1)
            {
                return records;
            }

            LumpSumRewardsRecord record = world.GetWipLumpSumRewardsRecord(validatorAddress, currency);
            if (record is null)
            {
                return records;
            }

            records.Add(record);
            long height = record.LastStartHeight;

            while (height >= startHeight)
            {
                record = world.GetLumpSumRewardsRecord(validatorAddress, currency, height);
                records.Add(record);
                height = record.LastStartHeight;
            }

            return records;
        }

        private static IWorld SetLumpSumRewardsRecord(
            this IWorld states, LumpSumRewardsRecord record)
            => states.SetDPoSState(record.Address, record.Bencoded);

        private static IWorld SaveWipLumpSumRewardsRecord(
            this IWorld states, Address validatorAddress, Currency currency, LumpSumRewardsRecord record)
            => states.SetDPoSState(DeriveAddress(validatorAddress, currency, record.StartHeight), record.Bencoded);

        
        private static LumpSumRewardsRecord GetLumpSumRewardsRecord(
            this IWorldState states, Address validatorAddress, Currency currency, long height)
            => GetLumpSumRewardRecordByAddress(states, DeriveAddress(validatorAddress, currency, height));

        public static LumpSumRewardsRecord GetWipLumpSumRewardsRecord(
            this IWorldState states, Address validatorAddress, Currency currency)
            => GetLumpSumRewardRecordByAddress(states, WipAddress(validatorAddress, currency));

        private static LumpSumRewardsRecord GetLumpSumRewardRecordByAddress(
            this IWorldState states, Address address)
        {
            if (states.GetDPoSState(address) is { } value)
            {
                return new LumpSumRewardsRecord(address, value);
            }

            return null;
        }

        private static Address WipAddress(Address ValidatorAddress, Currency currency)
            => DeriveAddress(ValidatorAddress, currency, -1);

        private static Address DeriveAddress(Address validatorAddress, Currency currency, long height)
        {
            byte[] hashed;
            using (var hmac = new HMACSHA1(validatorAddress.ToByteArray()))
            {
                hashed = hmac.ComputeHash(
                    new Codec().Encode(currency.Serialize()).Concat(
                    BitConverter.GetBytes(height).ToArray()).ToArray());
            }

            return new Address(hashed);
        }
    }
}
