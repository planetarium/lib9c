
using Libplanet.Action;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.TableData;

namespace Nekoyume.Arena
{
    public interface IArenaSimulator
    {
        public ArenaLog Log { get; }
        public IRandom Random { get; }
        public int Turn { get; }
        public long ShatterStrikeMaxDamage { get; }
        public BuffLimitSheet BuffLimitSheet { get; }
        BuffLinkSheet BuffLinkSheet { get; set; }
    }
}
