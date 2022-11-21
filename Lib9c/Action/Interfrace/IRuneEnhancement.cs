using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface IRuneEnhancement: IAction
    {
        static int Version { get; }
        static long AvailableBlockIndex { get; }
        Address AvatarAddress { get; set; }
        int RuneId { get; set; }
        int TryCount { get; set; }
    }
}
