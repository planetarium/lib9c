using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.TableData.Item;
using Libplanet.Common;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Item
{
    /// <summary>
    /// Represents a tradable material item that can be traded between players.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// </summary>
    [Serializable]
    public class TradableMaterial : Material, ITradableFungibleItem
    {
        /// <summary>
        /// Gets the tradable ID of this material.
        /// </summary>
        public Guid TradableId { get; }

        /// <summary>
        /// Gets or sets the required block index for trading this material.
        /// </summary>
        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than 0, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        private long _requiredBlockIndex;

        /// <summary>
        /// Derives a tradable ID from a fungible ID.
        /// </summary>
        /// <param name="fungibleId">The fungible ID to derive from</param>
        /// <returns>The derived tradable ID</returns>
        public static Guid DeriveTradableId(HashDigest<SHA256> fungibleId) =>
            new Guid(HashDigest<MD5>.DeriveFrom(fungibleId.ToByteArray()).ToByteArray());

        /// <summary>
        /// Initializes a new instance of the <see cref="TradableMaterial"/> class.
        /// </summary>
        /// <param name="data">The material item sheet row data</param>
        public TradableMaterial(MaterialItemSheet.Row data) : base(data)
        {
            TradableId = DeriveTradableId(ItemId);
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public TradableMaterial(IValue serialized) : base(serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    RequiredBlockIndex = dict.ContainsKey(RequiredBlockIndexKey)
                        ? dict[RequiredBlockIndexKey].ToLong()
                        : 0;
                    break;
                case List list:
                    RequiredBlockIndex = list[7].ToLong();
                    break;
                default:
                    throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
            }
            TradableId = DeriveTradableId(ItemId);
        }

        protected TradableMaterial(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(TradableMaterial other)
        {
            return base.Equals(other) && RequiredBlockIndex == other.RequiredBlockIndex && TradableId.Equals(other.TradableId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TradableMaterial) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ RequiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Serializes the tradable material to List format (new format).
        /// Order: [baseData..., requiredBlockIndex]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize()
        {
            return ((List)base.Serialize()).Add(RequiredBlockIndex.Serialize());
        }

        public override string ToString()
        {
            return base.ToString() +
                   $", {nameof(TradableId)}: {TradableId}" +
                   $", {nameof(RequiredBlockIndex)}: {RequiredBlockIndex}";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
