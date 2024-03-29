#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Model
{
    public class Undelegation : IEquatable<Undelegation>
    {
        public Undelegation(Address delegatorAddress, Address validatorAddress)
        {
            Address = DeriveAddress(delegatorAddress, validatorAddress);
            DelegatorAddress = delegatorAddress;
            ValidatorAddress = validatorAddress;
            UndelegationEntryIndex = 0;
            UndelegationEntryAddresses = new SortedList<long, Address>();
        }

        public Undelegation(IValue serialized)
        {
            List serializedList = (List)serialized;
            Address = serializedList[0].ToAddress();
            DelegatorAddress = serializedList[1].ToAddress();
            ValidatorAddress = serializedList[2].ToAddress();
            UndelegationEntryIndex = serializedList[3].ToLong();
            UndelegationEntryAddresses = new SortedList<long, Address>();
            foreach (
                IValue serializedUndelegationEntryAddress
                in (List)serializedList[4])
            {
                List items = (List)serializedUndelegationEntryAddress;
                UndelegationEntryAddresses.Add(items[0].ToLong(), items[1].ToAddress());
            }
        }

        public Undelegation(Undelegation undelegation)
        {
            Address = undelegation.Address;
            DelegatorAddress = undelegation.DelegatorAddress;
            ValidatorAddress = undelegation.ValidatorAddress;
            UndelegationEntryIndex = undelegation.UndelegationEntryIndex;
            UndelegationEntryAddresses = undelegation.UndelegationEntryAddresses;
        }

        // TODO: Better structure
        // This hard coding will cause some problems when it's modified
        // May be it would be better to be serialized
        public static int MaximumUndelegationEntries { get => 10; }

        public Address Address { get; }

        public Address DelegatorAddress { get; }

        public Address ValidatorAddress { get; }

        public Address DelegationAddress
        {
            get => Delegation.DeriveAddress(DelegatorAddress, ValidatorAddress);
        }

        public long UndelegationEntryIndex { get; set; }

        public SortedList<long, Address> UndelegationEntryAddresses { get; set; }

        public static bool operator ==(Undelegation obj, Undelegation other)
        {
            return obj.Equals(other);
        }

        public static bool operator !=(Undelegation obj, Undelegation other)
        {
            return !(obj == other);
        }

        public static Address DeriveAddress(Address delegatorAddress, Address validatorAddress)
        {
            return AddressHelper.Derive(AddressHelper.Derive(delegatorAddress, validatorAddress.ToByteArray()), "Undelegation");
        }

        public IValue Serialize()
        {
            List serializedUndelegationEntryAddresses = List.Empty;
#pragma warning disable LAA1002
            foreach (
                KeyValuePair<long, Address> undelegationEntryAddressKV
                in UndelegationEntryAddresses)
            {
                serializedUndelegationEntryAddresses =
                    serializedUndelegationEntryAddresses.Add(
                        List.Empty
                        .Add(undelegationEntryAddressKV.Key.Serialize())
                        .Add(undelegationEntryAddressKV.Value.Serialize()));
            }
#pragma warning restore LAA1002

            return List.Empty
                .Add(Address.Serialize())
                .Add(DelegatorAddress.Serialize())
                .Add(ValidatorAddress.Serialize())
                .Add(UndelegationEntryIndex.Serialize())
                .Add(serializedUndelegationEntryAddresses);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Undelegation);
        }

        public bool Equals(Undelegation? other)
        {
            return !(other is null) &&
                   Address.Equals(other.Address) &&
                   DelegatorAddress.Equals(other.DelegatorAddress) &&
                   ValidatorAddress.Equals(other.ValidatorAddress) &&
                   DelegationAddress.Equals(other.DelegationAddress) &&
                   UndelegationEntryIndex == other.UndelegationEntryIndex &&
#pragma warning disable LAA1002
                   UndelegationEntryAddresses.SequenceEqual(other.UndelegationEntryAddresses);
#pragma warning restore LAA1002
        }

        public override int GetHashCode()
        {
            return ByteUtil.CalculateHashCode(Address.ToByteArray());
        }
    }
}
