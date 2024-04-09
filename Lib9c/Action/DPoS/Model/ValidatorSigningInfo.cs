#nullable enable
using Bencodex.Types;
using Nekoyume.Action.DPoS.Util;
using Libplanet.Crypto;
using System;
using Libplanet.Common;

namespace Nekoyume.Action.DPoS.Model
{
    public class ValidatorSigningInfo : IEquatable<ValidatorSigningInfo>
    {
        public ValidatorSigningInfo()
        {
        }

        public ValidatorSigningInfo(IValue serialized)
        {
            var dict = (Bencodex.Types.Dictionary)serialized;
            Address = dict["addr"].ToAddress();
            StartHeight = dict["start_height"].ToLong();
            IndexOffset = dict["index_offset"].ToLong();
            JailedUntil = dict["jailed_until"].ToLong();
            Tombstoned = dict["tombstoned"].ToBoolean();
            MissedBlocksCounter = dict["missed_blocks_counter"].ToLong();
        }

        public Address Address { get; set; }

        public long StartHeight { get; set; }

        public long IndexOffset { get; set; }

        public long JailedUntil { get; set; }

        public bool Tombstoned { get; set; }

        public long MissedBlocksCounter { get; set; }

        public static bool operator ==(ValidatorSigningInfo obj, ValidatorSigningInfo other)
        {
            return obj.Equals(other);
        }

        public static bool operator !=(ValidatorSigningInfo obj, ValidatorSigningInfo other)
        {
            return !(obj == other);
        }

        public static Address DeriveAddress(Address validatorAddress)
        {
            return validatorAddress.Derive(nameof(ValidatorSigningInfo));
        }

        public IValue Serialize()
        {
            return Dictionary.Empty
                .Add("addr", Address.Serialize())
                .Add("start_height", StartHeight.Serialize())
                .Add("index_offset", IndexOffset.Serialize())
                .Add("jailed_until", JailedUntil.Serialize())
                .Add("tombstoned", Tombstoned.Serialize())
                .Add("missed_blocks_counter", MissedBlocksCounter.Serialize());
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ValidatorSigningInfo);
        }

        public bool Equals(ValidatorSigningInfo? other)
        {
            return !(other is null) &&
                   Address.Equals(other.Address) &&
                   StartHeight.Equals(other.StartHeight) &&
                   IndexOffset.Equals(other.IndexOffset) &&
                   JailedUntil.Equals(other.JailedUntil) &&
                   Tombstoned.Equals(other.Tombstoned) &&
                   MissedBlocksCounter.Equals(other.MissedBlocksCounter);
        }

        public override int GetHashCode()
        {
            return ByteUtil.CalculateHashCode(Address.ToByteArray());
        }
    }
}
