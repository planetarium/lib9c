using Libplanet.Action;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Character;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/
    /// </summary>
    public class ArenaSimulator
    {
        private const decimal TurnPriority = 100m;
        private const int MaxTurn = 200;

        public IRandom Random { get; }
        public int Turn { get; private set; }

        public ArenaSimulator(IRandom random)
        {
            Random = random;
            Turn = 1;
        }

        public BattleLog Simulate(
            ArenaPlayerDigest challenger,
            ArenaPlayerDigest enemy,
            ArenaSimulatorSheets sheets)
        {
            var log = new BattleLog();
            var players = SpawnPlayers(this, challenger, enemy, sheets, log);
            Turn = 1;

            while (true)
            {
                if (Turn > MaxTurn)
                {
                    // todo : 턴오버일경우 정책 필요함 일단 Lose
                    log.result = BattleLog.Result.Lose;
                    break;
                }

                if (!players.TryDequeue(out var selectedPlayer))
                {
                    break;
                }

                selectedPlayer.Tick();
                var clone = (ArenaCharacter)selectedPlayer.Clone();
                log.Add(clone.SkillLog);

                if (selectedPlayer.IsDead)
                {
                    log.Add(new Dead((ArenaCharacter)selectedPlayer.Clone()));
                    log.result = selectedPlayer.IsEnemy
                        ? BattleLog.Result.Win
                        : BattleLog.Result.Lose;
                    break;
                }

                if (!selectedPlayer.IsEnemy)
                {
                    Turn++;
                    log.Add(new ArenaTurnEnd((ArenaCharacter)selectedPlayer.Clone(), Turn));
                }

                foreach (var other in players)
                {
                    var current = players.GetPriority(other);
                    var speed = current * 0.6m;
                    players.UpdatePriority(other, speed);
                }

                players.Enqueue(selectedPlayer, TurnPriority / selectedPlayer.SPD);
            }

            return log;
        }

        private static SimplePriorityQueue<ArenaCharacter, decimal> SpawnPlayers(
            ArenaSimulator simulator,
            ArenaPlayerDigest challengerDigest,
            ArenaPlayerDigest enemyDigest,
            ArenaSimulatorSheets simulatorSheets,
            BattleLog log)
        {
            var challenger = new ArenaCharacter(simulator, challengerDigest, simulatorSheets);
            var enemy = new ArenaCharacter(simulator, enemyDigest, simulatorSheets, true);

            challenger.Spawn(enemy);
            enemy.Spawn(challenger);

            log.Add(new SpawnArenaPlayer((ArenaCharacter)challenger.Clone()));
            log.Add(new SpawnArenaPlayer((ArenaCharacter)enemy.Clone()));

            var players = new SimplePriorityQueue<ArenaCharacter, decimal>();
            players.Enqueue(challenger, TurnPriority / challenger.SPD);
            players.Enqueue(enemy, TurnPriority / enemy.SPD);
            return players;
        }
    }
}
