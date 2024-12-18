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

        /// <summary>
        /// Get the <see cref="Address"/> of the current <see cref="RewardBase"/>. 
        /// </summary>
        /// <param name="delegateeAddress">
        /// <see cref="Address"/> of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="delegateeAccountAddress">
        /// <see cref="Address"/> of the account of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </returns>
        public static Address CurrentRewardBaseAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.RewardBase,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress));

        /// <summary>
        /// Get the <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="delegateeMetadataAddress">
        /// <see cref="Address"/> of the <see cref="DelegateeMetadata"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the current <see cref="RewardBase"/>.
        /// </returns>
        public static Address CurrentRewardBaseAddress(
            Address delegateeMetadataAddress)
            => DeriveAddress(
                DelegationElementType.RewardBase,
                delegateeMetadataAddress);

        /// <summary>
        /// Get the <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </summary>
        /// <param name="delegateeAddress">
        /// <see cref="Address"/> of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="delegateeAccountAddress">
        /// <see cref="Address"/> of the account of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="height">
        /// The height of the <see cref="RewardBase"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </returns>
        public static Address RewardBaseAddress(
            Address delegateeAddress, Address delegateeAccountAddress, long height)
            => DeriveAddress(
                DelegationElementType.RewardBase,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress),
                BitConverter.GetBytes(height));

        /// <summary>
        /// Get the <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </summary>
        /// <param name="delegateeMetadataAddress">
        /// <see cref="Address"/> of the <see cref="DelegateeMetadata"/>.
        /// </param>
        /// <param name="height">
        /// The height of the <see cref="RewardBase"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the <see cref="RewardBase"/> at the given height.
        /// </returns>
        public static Address RewardBaseAddress(
            Address delegateeMetadataAddress, long height)
            => DeriveAddress(
                DelegationElementType.RewardBase,
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

        /// <summary>
        /// Get the <see cref="Address"/> of the distribution pool
        /// where the rewards are distributed from.
        /// </summary>
        /// <param name="delegateeAddress">
        /// <see cref="Address"/> of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <param name="delegateeAccountAddress">
        /// <see cref="Address"/> of the account of the <see cref="Delegatee{T, TSelf}"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the distribution pool.
        /// </returns>
        public static Address DistributionPoolAddress(
            Address delegateeAddress, Address delegateeAccountAddress)
            => DeriveAddress(
                DelegationElementType.DistributionPool,
                DelegateeMetadataAddress(delegateeAddress, delegateeAccountAddress));

        /// <summary>
        /// Get the <see cref="Address"/> of the distribution pool
        /// where the rewards are distributed from.
        /// </summary>
        /// <param name="delegateeMetadataAddress">
        /// <see cref="Address"/> of the <see cref="DelegateeMetadata"/>.
        /// </param>
        /// <returns>
        /// <see cref="Address"/> of the distribution pool.
        /// </returns>
        public static Address DistributionPoolAddress(
            Address delegateeMetadataAddress)
            => DeriveAddress(
                DelegationElementType.DistributionPool,
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
            RewardBase,
            RewardPool,
            DelegationPool,
            DistributionPool,
        }
    }
}
