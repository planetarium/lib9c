using Lib9c.Model.BattleStatus.Arena;
using Lib9c.TableData.Skill;
using Libplanet.Action;

namespace Lib9c.Arena
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
