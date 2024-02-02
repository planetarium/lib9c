using System;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Collection
{
    public class NonFungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.NonFungible;
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public Guid NonFungibleId { get; set; }
        public int Level { get; set; }
        public int OptionCount { get; set; }
        public bool SkillContains { get; set; }

        public IValue Serialize()
        {
            return List.Empty
                .Add((int)Type)
                .Add(ItemId)
                .Add(ItemCount)
                .Add(NonFungibleId.Serialize())
                .Add(Level)
                .Add(OptionCount)
                .Add(SkillContains.Serialize());
        }

        public NonFungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
            NonFungibleId = serialized[3].ToGuid();
            Level = (Integer)serialized[4];
            OptionCount = (Integer)serialized[5];
            SkillContains = serialized[6].ToBoolean();
        }

        public NonFungibleCollectionMaterial()
        {
            ItemCount = 1;
        }
    }
}
