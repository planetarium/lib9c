#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public static class DelegationAddress
    {
        public static Address DelegateeMetadataAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.DelegateeMetadata,
                delegateeAddress,
                delegateeAccountAddress.ByteArray);

        public static Address DelegatorMetadataAddress(
            Address delegatorAddress, Address delegatorAccountAddress)
            => DeriveAddress(
                DelegationElementType.DelegatorMetadata,
                delegatorAddress,
                delegatorAccountAddress.ByteArray);

        public static Address BondAddress(
            Address delegateeAddress, Address delegateeAccountAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.Bond,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress),
                delegatorAddress.ByteArray);

        public static Address BondAddress(
            Address delegateeMetadataAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.Bond,
                delegateeMetadataAddress,
                delegatorAddress.ByteArray);

        public static Address UnbondLockInAddress(
            Address delegateeAddress, Address delegateeAccountAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.UnbondLockIn,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress),
                delegatorAddress.ByteArray);

        public static Address UnbondLockInAddress(
            Address delegateeMetadataAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.UnbondLockIn,
                delegateeMetadataAddress,
                delegatorAddress.ByteArray);

        public static Address RebondGraceAddress(
            Address delegateeAddress, Address delegateeAccountAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.RebondGrace,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress),
                delegatorAddress.ByteArray);

        public static Address RebondGraceAddress(
            Address delegateeMetadataAddress, Address delegatorAddress)
            => DeriveAddress(
                DelegationElementType.RebondGrace,
                delegateeMetadataAddress,
                delegatorAddress.ByteArray);

        public static Address CurrentLumpSumRewardsRecordAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.LumpSumRewardsRecord,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress));

        public static Address CurrentLumpSumRewardsRecordAddress(
            Address delegateeMetadataAddress)
            => DeriveAddress(
                DelegationElementType.LumpSumRewardsRecord,
                delegateeMetadataAddress);

        public static Address LumpSumRewardsRecordAddress(
            Address delegateeAddress, Address delegateeAccountAddress, long height)
            => DeriveAddress(
                DelegationElementType.LumpSumRewardsRecord,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress),
                BitConverter.GetBytes(height));

        public static Address LumpSumRewardsRecordAddress(
            Address delegateeMetadataAddress, long height)
            => DeriveAddress(
                DelegationElementType.LumpSumRewardsRecord,
                delegateeMetadataAddress,
                BitConverter.GetBytes(height));

        public static Address RewardPoolAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.RewardPool,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress));

        public static Address RewardPoolAddress(
            Address delegateeMetadataAddress)
            => DeriveAddress(
                DelegationElementType.RewardPool,
                delegateeMetadataAddress);

        public static Address DelegationPoolAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.DelegationPool,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress));

        public static Address DelegationPoolAddress(
            Address delegateeMetadataAddress)
            => DeriveAddress(
                DelegationElementType.DelegationPool,
                delegateeMetadataAddress);

        private static Address DeriveAddress(
            DelegationElementType identifier,
            Address address,
            IEnumerable<byte>? bytes = null)
        {
            byte[] hashed;
            using (HMACSHA1 hmac = new(
                BitConverter.GetBytes((int)identifier).ToArray()))
            {
                hashed = hmac.ComputeHash(
                    address.ByteArray.Concat(bytes ?? Array.Empty<byte>()).ToArray());
            }

            return new Address(hashed);
        }

        private enum DelegationElementType
        {
            DelegateeMetadata,
            DelegatorMetadata,
            Bond,
            UnbondLockIn,
            RebondGrace,
            LumpSumRewardsRecord,
            RewardPool,
            DelegationPool,
        }
    }
}
