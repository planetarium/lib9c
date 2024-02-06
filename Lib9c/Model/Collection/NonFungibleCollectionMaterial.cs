using System;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Collection
{
    public class NonFungibleCollectionMaterial : ICollectionMaterial
    {
        public MaterialType Type => MaterialType.NonFungible;
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public Guid NonFungibleId { get; set; }
        public int Level { get; set; }
        public bool SkillContains { get; set; }

        public IValue Bencoded => List.Empty
            .Add((int)Type)
            .Add(ItemId)
            .Add(ItemCount)
            .Add(NonFungibleId.Serialize())
            .Add(Level)
            .Add(SkillContains.Serialize());

        public NonFungibleCollectionMaterial(List serialized)
        {
            ItemId = (Integer)serialized[1];
            ItemCount = (Integer)serialized[2];
            NonFungibleId = serialized[3].ToGuid();
            Level = (Integer)serialized[4];
            SkillContains = serialized[5].ToBoolean();
        }

        public NonFungibleCollectionMaterial()
        {
            ItemCount = 1;
        }
    }
}
