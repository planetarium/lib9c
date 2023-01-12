using Lib9c.Model.BattleStatus.Arena;
using Libplanet.Action;

namespace Lib9c.Arena
{
    public interface IArenaSimulator
    {
        public ArenaLog Log { get; }
        public IRandom Random { get; }
        public int Turn { get; }
    }
}
