using Libplanet;

namespace Nekoyume.Action.Interface
{
    public interface IRapidCombination : IRapidCombinationFamily
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
    }
}
