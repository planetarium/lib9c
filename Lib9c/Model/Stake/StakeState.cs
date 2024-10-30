using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stake
{
    public readonly struct StakeState : IState
    {
        public const string StateTypeName = "stake_state";
        public const int LatestStateTypeVersion = 3;

        public static Address DeriveAddress(Address address) =>
            LegacyStakeState.DeriveAddress(address);

        public readonly int StateVersion;
        public readonly Contract Contract;
        public readonly long StartedBlockIndex;
        public readonly long ReceivedBlockIndex;

        [Obsolete("Not used because of guild system")]
        public long CancellableBlockIndex =>
            StartedBlockIndex + Contract.LockupInterval;

        public long ClaimedBlockIndex => ReceivedBlockIndex == 0
            ? StartedBlockIndex
            : StartedBlockIndex + Math.DivRem(
                ReceivedBlockIndex - StartedBlockIndex,
                Contract.RewardInterval,
                out _
            ) * Contract.RewardInterval;

        public long ClaimableBlockIndex =>
            ClaimedBlockIndex + Contract.RewardInterval;

        public StakeState(
            Contract contract,
            long startedBlockIndex,
            long receivedBlockIndex = 0,
            int stateVersion = LatestStateTypeVersion)
        {
            if (startedBlockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startedBlockIndex),
                    startedBlockIndex,
                    "startedBlockIndex should be greater than or equal to 0.");
            }

            if (receivedBlockIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(receivedBlockIndex),
                    receivedBlockIndex,
                    "receivedBlockIndex should be greater than or equal to 0.");
            }

            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            StartedBlockIndex = startedBlockIndex;
            ReceivedBlockIndex = receivedBlockIndex;
            StateVersion = stateVersion;
        }

        // Migration constructor V1 to V2.
        public StakeState(
            LegacyStakeState legacyStakeState,
            Contract contract
        ) : this(
            contract,
            legacyStakeState?.StartedBlockIndex ?? throw new ArgumentNullException(nameof(legacyStakeState)),
            legacyStakeState.ReceivedBlockIndex,
            stateVersion: 2
        )
        {
        }

        public StakeState(IValue serialized)
        {
            if (serialized is not List list)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"{nameof(serialized)} should be List type.");
            }

            if (list[0] is not Text stateTypeNameValue ||
                stateTypeNameValue != StateTypeName ||
                list[1] is not Integer stateTypeVersionValue ||
                stateTypeVersionValue.Value == 1 ||
                stateTypeVersionValue.Value > LatestStateTypeVersion)
            {
                throw new ArgumentException(
                    nameof(serialized),
                    $"{nameof(serialized)} doesn't have valid header.");
            }

            const int reservedCount = 2;

            StateVersion = stateTypeVersionValue;
            Contract = new Contract(list[reservedCount]);
            StartedBlockIndex = (Integer)list[reservedCount + 1];
            ReceivedBlockIndex = (Integer)list[reservedCount + 2];
        }

        public IValue Serialize() => new List(
            (Text)StateTypeName,
            (Integer)StateVersion,
            Contract.Serialize(),
            (Integer)StartedBlockIndex,
            (Integer)ReceivedBlockIndex
        );

        public bool Equals(StakeState other)
        {
            return Equals(Contract, other.Contract) &&
                   StartedBlockIndex == other.StartedBlockIndex &&
                   ReceivedBlockIndex == other.ReceivedBlockIndex &&
                   StateVersion == other.StateVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is StakeState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Contract != null ? Contract.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ StateVersion.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(StakeState left, StakeState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StakeState left, StakeState right)
        {
            return !(left == right);
        }
    }
}
