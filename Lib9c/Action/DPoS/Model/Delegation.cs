#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Model
{
    public class Delegation : IEquatable<Delegation>
    {
        public Delegation(Address delegatorAddress, Address validatorAddress)
        {
            Address = DeriveAddress(delegatorAddress, validatorAddress);
            DelegatorAddress = delegatorAddress;
            ValidatorAddress = validatorAddress;
            LatestDistributeHeight = 0;
        }

        public Delegation(IValue serialized)
        {
            List serializedList = (List)serialized;
            Address = serializedList[0].ToAddress();
            DelegatorAddress = serializedList[1].ToAddress();
            ValidatorAddress = serializedList[2].ToAddress();
            LatestDistributeHeight = serializedList[3].ToLong();
        }

        public Delegation(Delegation delegation)
        {
            Address = delegation.Address;
            DelegatorAddress = delegation.DelegatorAddress;
            ValidatorAddress = delegation.ValidatorAddress;
            LatestDistributeHeight = delegation.LatestDistributeHeight;
        }

        public Address Address { get; }

        public Address DelegatorAddress { get; }

        public Address ValidatorAddress { get; }

        public long LatestDistributeHeight { get; set; }

        public FungibleAssetValue GetShares(IWorldState worldState)
        {
            return worldState.GetBalance(Address, Asset.Share);
        }

        public static bool operator ==(Delegation obj, Delegation other)
        {
            return obj.Equals(other);
        }

        public static bool operator !=(Delegation obj, Delegation other)
        {
            return !(obj == other);
        }

        public static Address DeriveAddress(Address delegatorAddress, Address validatorAddress)
        {
            return AddressHelper.Derive(AddressHelper.Derive(delegatorAddress, validatorAddress.ToByteArray()), "Delegation");
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(DelegatorAddress.Serialize())
                .Add(ValidatorAddress.Serialize())
                .Add(LatestDistributeHeight.Serialize());
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Delegation);
        }

        public bool Equals(Delegation? other)
        {
            return !(other is null) &&
                   Address.Equals(other.Address) &&
                   DelegatorAddress.Equals(other.DelegatorAddress) &&
                   ValidatorAddress.Equals(other.ValidatorAddress) &&
                   LatestDistributeHeight.Equals(other.LatestDistributeHeight);
        }

        public override int GetHashCode()
        {
            return ByteUtil.CalculateHashCode(Address.ToByteArray());
        }
    }
}
