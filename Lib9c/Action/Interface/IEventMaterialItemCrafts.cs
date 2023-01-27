using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action.Interface
{
    public interface IEventMaterialItemCrafts : IEventMaterialItemCraftsFamily
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventMaterialItemRecipeId { get; }
        Dictionary<int, int> MaterialsToUse { get; }
    }
}
