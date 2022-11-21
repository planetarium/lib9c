using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface IRuneEnhancement: IAction
    {
        public Address AvatarAddress { get; set; }
        public int RuneId { get; set; }
        public int TryCount { get; set; }
    }
}
