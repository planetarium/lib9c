using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Nekoyume.Model.State
{
    public class ArenaAvatarState : IState
    {
        public static Address DeriveAddress(Address avatarAddress) => avatarAddress.Derive("arena_avatar");

        public class ArenaRecords
        {
            private readonly Dictionary<long, ArenaRecord> _records;

            public ArenaRecords()
            {
                _records = new Dictionary<long, ArenaRecord>();
            }

            public ArenaRecords(Dictionary serialized)
            {
                _records = serialized.ToDictionary(
                    pair => pair.Key.ToLong(),
                    pair => new ArenaRecord((List)pair.Value));
            }

            public IValue Serialize() =>
                new Dictionary(_records.ToDictionary(
                    pair => (IKey)pair.Key.Serialize(),
                    pair => pair.Value.Serialize()));

            public void Add(long index)
            {
                if (_records.ContainsKey(index))
                {
                    throw new AlreadyContainsException($"ArenaRecord is already exist : {index}");
                }

                _records.Add(index, new ArenaRecord());
            }

            public void Update(long index, int score, bool isWin)
            {
                if (!_records.ContainsKey(index))
                {
                    throw new NotExistException($"ArenaRecord does not exist : {index}");
                }

                _records[index].Update(score, isWin);
            }
        }

        public ArenaRecords Records { get; }
        public List<Guid> Costumes { get; }
        public List<Guid> Equipments { get; }
        public int Ticket { get; private set; }
        public int NcgTicket { get; private set; }
        public int Level { get; private set; }

        public string NameWithHash { get; }
        public int CharacterId { get; }
        public int HairIndex { get; }
        public int LensIndex { get; }
        public int EarIndex { get; }
        public int TailIndex { get; }

        public ArenaAvatarState(AvatarState avatarState)
        {
            Records = new ArenaRecords();
            Costumes = new List<Guid>();
            Equipments = new List<Guid>();
            Ticket = GameConfig.ArenaChallengeCountMax;
            NcgTicket = 0;
            Level = avatarState.level;

            NameWithHash = avatarState.NameWithHash;
            CharacterId = avatarState.characterId;
            HairIndex = avatarState.hair;
            LensIndex = avatarState.lens;
            EarIndex = avatarState.ear;
            TailIndex = avatarState.tail;
        }

        public ArenaAvatarState(List serialized)
        {
            Records = new ArenaRecords((Dictionary)serialized[0]);
            Costumes = serialized[1].ToList(StateExtensions.ToGuid);
            Equipments = serialized[2].ToList(StateExtensions.ToGuid);
            Ticket = (Integer)serialized[3];
            NcgTicket = (Integer)serialized[4];
            Level = (Integer)serialized[5];

            NameWithHash = serialized[6].ToDotnetString();
            CharacterId = (Integer)serialized[7];
            HairIndex = (Integer)serialized[8];
            LensIndex = (Integer)serialized[9];
            EarIndex = (Integer)serialized[10];
            TailIndex = (Integer)serialized[11];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Records.Serialize())
                .Add(Costumes.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Equipments.OrderBy(x => x).Select(x => x.Serialize()).Serialize())
                .Add(Ticket)
                .Add(NcgTicket)
                .Add(Level)
                .Add(NameWithHash)
                .Add(CharacterId)
                .Add(HairIndex)
                .Add(LensIndex)
                .Add(EarIndex)
                .Add(TailIndex);
        }

        public bool TryUseTicket(int value)
        {
            var sum = Ticket + NcgTicket;
            if (sum < value)
            {
                return false;
            }

            if (value > Ticket)
            {
                var remainTicket = value - Ticket;
                NcgTicket -= remainTicket;
                Ticket = 0;
            }
            else
            {
                Ticket -= value;
            }

            return true;
        }

        public void AddTicket(int value)
        {
            Ticket += value;
        }

        public void AddNcgTicket(int value)
        {
            NcgTicket += value;
        }

        public void UpdateCostumes([NotNull] List<Guid> costumes)
        {
            if (costumes == null)
            {
                throw new ArgumentNullException(nameof(costumes));
            }

            Costumes.Clear();
            Costumes.AddRange(costumes);
        }

        public void UpdateEquipment([NotNull] List<Guid> equipments)
        {
            if (equipments == null)
            {
                throw new ArgumentNullException(nameof(equipments));
            }

            Equipments.Clear();
            Equipments.AddRange(equipments);
        }

        public void UpdateLevel(int level)
        {
            Level = level;
        }
    }
}
