using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class WeeklyArenaState2 : ISerializable
    {
        #region static

        private static Address _baseAddress = Addresses.WeeklyArena;

        public static Address DeriveAddress(int index)
        {
            return _baseAddress.Derive($"weekly_arena_2_{index}");
        }

        #endregion

        public Address address;

        public long ResetIndex;

        public bool Ended;

        private List<Address> _avatarAddresses = new List<Address>();
        public IReadOnlyList<Address> AvatarAddresses => _avatarAddresses;

        public WeeklyArenaState2(int index)
        {
            address = DeriveAddress(index);
        }

        public WeeklyArenaState2(Address address)
        {
            this.address = address;
        }

        public WeeklyArenaState2(List serialized)
        {
            address = serialized[0].ToAddress();
            _avatarAddresses = serialized[1].ToList(StateExtensions.ToAddress);
            ResetIndex = serialized[2].ToLong();
            Ended = serialized[3].ToBoolean();
        }

        protected WeeklyArenaState2(SerializationInfo info, StreamingContext context)
            : this((List)new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public IValue Serialize()
        {
            var addressList =
                _avatarAddresses.Aggregate(List.Empty, (current, scoreInfo) => current.Add(scoreInfo.Serialize()));
            return List.Empty
                .Add(address.Serialize())
#pragma warning disable LAA1002
                .Add(addressList)
#pragma warning restore LAA1002
                .Add(ResetIndex.Serialize())
                .Add(Ended.Serialize());

        }

        public void Update(Address avatarAddress)
        {
            if (!_avatarAddresses.Contains(avatarAddress))
            {
                _avatarAddresses.Add(avatarAddress);
            }
        }

        public void Update(long blockIndex)
        {
            ResetIndex = blockIndex;
        }

        public void End()
        {
            Ended = true;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }

        protected bool Equals(WeeklyArenaState2 other)
        {
            return address.Equals(other.address) && ResetIndex == other.ResetIndex && Ended == other.Ended &&
                   _avatarAddresses.OrderBy(a => a).SequenceEqual(other._avatarAddresses.OrderBy(a => a));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WeeklyArenaState2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = address.GetHashCode();
                hashCode = (hashCode * 397) ^ ResetIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ Ended.GetHashCode();
                hashCode = (hashCode * 397) ^ (_avatarAddresses != null ? _avatarAddresses.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
