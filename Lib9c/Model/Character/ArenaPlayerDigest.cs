using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
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
            Equipments = avatarState.GetNonFungibleItems<Equipment>(arenaAvatarState.Equipments);;
        }

        public ArenaPlayerDigest(AvatarState avatarState)
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
                (Costume)ItemFactory.Deserialize((Dictionary)c)).ToList();
            Equipments = ((List)serialized[8]).Select(e =>
                (Equipment)ItemFactory.Deserialize((Dictionary)e)).ToList();
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
                    (current, equipment) => current.Add(equipment.Serialize())));
        }
    }
}
