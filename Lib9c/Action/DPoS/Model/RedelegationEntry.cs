#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Model
{
    public class RedelegationEntry : IEquatable<RedelegationEntry>
    {
        private FungibleAssetValue _redelegatingShare;
        private FungibleAssetValue _unbondingConsensusToken;
        private FungibleAssetValue _issuedShare;

        public RedelegationEntry(
            Address redelegationAddress,
            FungibleAssetValue redelegatingShare,
            FungibleAssetValue unbondingConsensusToken,
            FungibleAssetValue issuedShare,
            long index,
            long blockHeight)
        {
            Address = DeriveAddress(redelegationAddress, index);
            RedelegationAddress = redelegationAddress;
            RedelegatingShare = redelegatingShare;
            InitialConsensusToken = unbondingConsensusToken;
            UnbondingConsensusToken = unbondingConsensusToken;
            IssuedShare = issuedShare;
            Index = index;
            CompletionBlockHeight = blockHeight + UnbondingSet.Period;
            CreationHeight = blockHeight;
        }

        public RedelegationEntry(IValue serialized)
        {
            List serializedList = (List)serialized;
            Address = serializedList[0].ToAddress();
            RedelegationAddress = serializedList[1].ToAddress();
            RedelegatingShare = serializedList[2].ToFungibleAssetValue();
            InitialConsensusToken = serializedList[3].ToFungibleAssetValue();
            UnbondingConsensusToken = serializedList[4].ToFungibleAssetValue();
            IssuedShare = serializedList[5].ToFungibleAssetValue();
            Index = serializedList[6].ToLong();
            CompletionBlockHeight = serializedList[7].ToLong();
            CreationHeight = serializedList[8].ToLong();
        }

        public Address Address { get; set; }

        public Address RedelegationAddress { get; set; }

        public FungibleAssetValue RedelegatingShare
        {
            get => _redelegatingShare;
            set
            {
                if (!value.Currency.Equals(Asset.Share))
                {
                    throw new Exception.InvalidCurrencyException(Asset.Share, value.Currency);
                }

                _redelegatingShare = value;
            }
        }

        public FungibleAssetValue InitialConsensusToken { get; set; }

        public FungibleAssetValue UnbondingConsensusToken
        {
            get => _unbondingConsensusToken;
            set
            {
                if (!value.Currency.Equals(Asset.ConsensusToken))
                {
                    throw new Exception.InvalidCurrencyException(Asset.ConsensusToken, value.Currency);
                }

                _unbondingConsensusToken = value;
            }
        }

        public FungibleAssetValue IssuedShare
        {
            get => _issuedShare;
            set
            {
                if (!value.Currency.Equals(Asset.Share))
                {
                    throw new Exception.InvalidCurrencyException(Asset.Share, value.Currency);
                }

                _issuedShare = value;
            }
        }

        public long Index { get; set; }

        public long CreationHeight { get; set; }

        public long CompletionBlockHeight { get; set; }

        public static bool operator ==(RedelegationEntry obj, RedelegationEntry other)
        {
            return obj.Equals(other);
        }

        public static bool operator !=(RedelegationEntry obj, RedelegationEntry other)
        {
            return !(obj == other);
        }

        public static Address DeriveAddress(Address redelegationAddress, long index)
        {
            return AddressHelper.Derive(redelegationAddress, $"RedelegationEntry{index}");
        }

        public bool IsMatured(long blockHeight) => blockHeight >= CompletionBlockHeight;

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(RedelegationAddress.Serialize())
                .Add(RedelegatingShare.Serialize())
                .Add(InitialConsensusToken.Serialize())
                .Add(UnbondingConsensusToken.Serialize())
                .Add(IssuedShare.Serialize())
                .Add(Index.Serialize())
                .Add(CompletionBlockHeight.Serialize())
                .Add(CreationHeight.Serialize());
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as RedelegationEntry);
        }

        public bool Equals(RedelegationEntry? other)
        {
            return !(other is null) &&
                   Address.Equals(other.Address) &&
                   RedelegationAddress.Equals(other.RedelegationAddress) &&
                   RedelegatingShare.Equals(other.RedelegatingShare) &&
                   InitialConsensusToken.Equals(other.InitialConsensusToken) &&
                   UnbondingConsensusToken.Equals(other.UnbondingConsensusToken) &&
                   IssuedShare.Equals(other.IssuedShare) &&
                   Index == other.Index &&
                   CompletionBlockHeight == other.CompletionBlockHeight &&
                   CreationHeight == other.CreationHeight;
        }

        public override int GetHashCode()
        {
            return ByteUtil.CalculateHashCode(Address.ToByteArray());
        }
    }
}
