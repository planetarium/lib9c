using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface IEventMaterialItemCrafts : IAction
    {
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventMaterialItemRecipeId { get; }
        Dictionary<int, int> MaterialsToUse { get; }
    }
}
