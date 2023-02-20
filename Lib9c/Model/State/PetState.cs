using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    [VersionedState(Moniker, Version)]
    public interface IPetStateV1
    {
        public const string Moniker = "pet";
        public const int Version = 1;

        Integer PetId { get; }
        Integer Level { get; }
        Integer UnlockedBlockIndex { get; }
    }

    [VersionedStateImpl(typeof(IPetStateV1))]
    public class PetState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int petId) =>
            avatarAddress.Derive($"pet-{petId}");

        public int PetId { get; }
        public int Level { get; private set; }
        public long UnlockedBlockIndex { get; private set; }

        public PetState() : this(0)
        {
        }

        public PetState(int petId)
        {
            PetId = petId;
            Level = 0;
            UnlockedBlockIndex = 0;
        }

        public PetState(List serialized)
        {
            PetId = (Integer)serialized[0];
            Level = (Integer)serialized[1];
            UnlockedBlockIndex = (Integer)serialized[2];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add((Integer)PetId)
                .Add((Integer)Level)
                .Add((Integer)UnlockedBlockIndex);
        }

        public void LevelUp()
        {
            if (Level == int.MaxValue)
            {
                throw new System.InvalidOperationException("Pet level is already max.");
            }

            Level++;
        }

        public void Update(long unlockedIndex)
        {
            UnlockedBlockIndex = unlockedIndex;
        }

        public bool Validate(long blockIndex)
        {
            return blockIndex >= UnlockedBlockIndex;
        }
    }
}
