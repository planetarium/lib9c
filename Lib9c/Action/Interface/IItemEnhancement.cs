using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action.Interface
{
    public interface IItemEnhancement : IItemEnhancementFamily
    {
        Guid ItemId { get; }
        IEnumerable<Guid> MaterialIds { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
