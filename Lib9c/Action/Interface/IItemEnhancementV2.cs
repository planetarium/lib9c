using System;
using Libplanet;

namespace Nekoyume.Action.Interface
{
    public interface IItemEnhancementV2 : IItemEnhancementFamily
    {
        Guid ItemId { get; }
        Guid MaterialId { get; }
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
