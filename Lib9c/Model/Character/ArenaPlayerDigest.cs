using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1156
    /// </summary>
    public readonly struct ArenaPlayerDigest : IState
    {
        public readonly string NameWithHash;
        public readonly int CharacterId;
        public readonly int Level;
        public readonly int HairIndex;
        public readonly int LensIndex;
        public readonly int EarIndex;
        public readonly int TailIndex;

        public readonly List<Costume> Costumes;
        public readonly List<Equipment> Equipments;
        public readonly AllRuneState Runes;
        public readonly RuneSlotState RuneSlotState;

        [Obsolete("Do not use it")]
        public ArenaPlayerDigest(AvatarState avatarState, ArenaAvatarState arenaAvatarState)
        {
            NameWithHash = avatarState.NameWithHash;
            CharacterId = avatarState.characterId;
            HairIndex = avatarState.hair;
            LensIndex = avatarState.lens;
            EarIndex = avatarState.ear;
            TailIndex = avatarState.tail;

            Level = avatarState.level;
            Costumes = avatarState.GetNonFungibleItems<Costume>(arenaAvatarState.Costumes);
            Equipments = avatarState.GetNonFungibleItems<Equipment>(arenaAvatarState.Equipments);
            Runes = new AllRuneState();
            RuneSlotState = new RuneSlotState(BattleType.Arena);
        }

        public ArenaPlayerDigest(
            AvatarState avatarState,
            List<Guid> equipments,
            List<Guid> costumes,
            AllRuneState runes,
            RuneSlotState runeSlotState
        )
        {
            NameWithHash = avatarState.NameWithHash;
            CharacterId = avatarState.characterId;
            HairIndex = avatarState.hair;
            LensIndex = avatarState.lens;
            EarIndex = avatarState.ear;
            TailIndex = avatarState.tail;

            Level = avatarState.level;
            Costumes = avatarState.GetNonFungibleItems<Costume>(costumes);
            Equipments = avatarState.GetNonFungibleItems<Equipment>(equipments);
            Runes = runes;
            RuneSlotState = runeSlotState;
        }

        public ArenaPlayerDigest(
            AvatarState avatarState,
            AllRuneState runes,
            RuneSlotState runeSlotState
        )
        {
            NameWithHash = avatarState.NameWithHash;
            CharacterId = avatarState.characterId;
            HairIndex = avatarState.hair;
            LensIndex = avatarState.lens;
            EarIndex = avatarState.ear;
            TailIndex = avatarState.tail;
            Level = avatarState.level;

            var costumes = avatarState.inventory.Costumes
                .Where(x => x.equipped)
                .ToList();
            Costumes = costumes;
            var equipments = avatarState.inventory.Equipments
                .Where(x => x.equipped)
                .ToList();
            Equipments = equipments;
            Runes = runes;
            RuneSlotState = runeSlotState;
        }

        public ArenaPlayerDigest(
            AvatarState avatarState,
            List<Costume> costumes,
            List<Equipment> equipments,
            AllRuneState runes,
            RuneSlotState runeSlotState
        )
        {
            NameWithHash = avatarState.NameWithHash;
            CharacterId = avatarState.characterId;
            HairIndex = avatarState.hair;
            LensIndex = avatarState.lens;
            EarIndex = avatarState.ear;
            TailIndex = avatarState.tail;
            Level = avatarState.level;
            Costumes = costumes;
            Equipments = equipments;
            Runes = runes;
            RuneSlotState = runeSlotState;
        }

        public ArenaPlayerDigest(List serialized)
        {
            NameWithHash = serialized[0].ToDotnetString();
            CharacterId = serialized[1].ToInteger();
            Level = serialized[2].ToInteger();
            HairIndex = serialized[3].ToInteger();
            LensIndex = serialized[4].ToInteger();
            EarIndex = serialized[5].ToInteger();
            TailIndex = serialized[6].ToInteger();
            Costumes = ((List)serialized[7]).Select(c =>
                (Costume)ItemFactory.Deserialize(c)).ToList();
            Equipments = ((List)serialized[8]).Select(e =>
                (Equipment)ItemFactory.Deserialize(e)).ToList();
            Runes = new AllRuneState((List)serialized[9]);
            RuneSlotState = new RuneSlotState((List)serialized[10]);
        }

        public IValue Serialize()
        {
            return List.Empty
                    .Add(NameWithHash.Serialize())
                    .Add(CharacterId.Serialize())
                    .Add(Level.Serialize())
                    .Add(HairIndex.Serialize())
                    .Add(LensIndex.Serialize())
                    .Add(EarIndex.Serialize())
                    .Add(TailIndex.Serialize())
                    .Add(Costumes.Aggregate(List.Empty,
                        (current, costume) => current.Add(costume.Serialize())))
                    .Add(Equipments.Aggregate(List.Empty,
                        (current, equipment) => current.Add(equipment.Serialize())))
                    .Add(Runes.Serialize())
                    .Add(RuneSlotState.Serialize())
                ;
        }
    }
}
