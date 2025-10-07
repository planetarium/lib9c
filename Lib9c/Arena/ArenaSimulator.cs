using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.BattleStatus.Arena;
using Lib9c.Model.Character;
using Lib9c.Model.Stat;
using Lib9c.TableData;
using Lib9c.TableData.Skill;
using Libplanet.Action;
using Priority_Queue;

namespace Lib9c.Arena
{
    /// <summary>
    /// Changed at https://github.com/planetarium/lib9c/pull/2229
    /// </summary>
    public class ArenaSimulator : IArenaSimulator
    {
        private const decimal TurnPriority = 100m;
        private const int MaxTurn = 200;

        public IRandom Random { get; }
        public int Turn { get; private set; }
        public ArenaLog Log { get; private set; }
        public int HpModifier { get; }

        public long ShatterStrikeMaxDamage { get; private set; }
        public BuffLimitSheet BuffLimitSheet { get; private set; }
        public BuffLinkSheet BuffLinkSheet { get; set; }

        public ArenaSimulator(IRandom random,
            int hpModifier = 2,
            long shatterStrikeMaxDamage = 400_000  // 400K is initial limit of ShatterStrike. Use this as default.
        )
        {
            Random = random;
            Turn = 1;
            HpModifier = hpModifier;
            ShatterStrikeMaxDamage = shatterStrikeMaxDamage;
        }

        public ArenaLog Simulate(
            ArenaPlayerDigest challenger,
            ArenaPlayerDigest enemy,
            ArenaSimulatorSheets sheets,
            List<StatModifier> challengerCollectionModifiers,
            List<StatModifier> enemyCollectionModifiers,
            BuffLimitSheet buffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            bool setExtraValueBuffBeforeGetBuffs = false)
        {
            Log = new ArenaLog();
            BuffLimitSheet = buffLimitSheet;
            BuffLinkSheet = buffLinkSheet;
            var players = SpawnPlayers(this, challenger, enemy, sheets, Log, challengerCollectionModifiers, enemyCollectionModifiers, setExtraValueBuffBeforeGetBuffs);
            Turn = 1;

            while (true)
            {
                if (Turn > MaxTurn)
                {
                    // todo : 턴오버일경우 정책 필요함 일단 Lose
                    Log.Result = ArenaLog.ArenaResult.Lose;
                    break;
                }

                if (!players.TryDequeue(out var selectedPlayer))
                {
                    break;
                }

                selectedPlayer.Tick();

                var deadPlayers = players.Where(x => x.IsDead);
                var arenaCharacters = deadPlayers as ArenaCharacter[] ?? deadPlayers.ToArray();
                if (arenaCharacters.Any())
                {
                    var (deadPlayer, result) = GetBattleResult(arenaCharacters);
                    Log.Result = result;
                    Log.Add(new ArenaDead((ArenaCharacter)deadPlayer.Clone()));
                    Log.Add(new ArenaTurnEnd((ArenaCharacter)selectedPlayer.Clone(), Turn));
                    break;
                }

                if (!selectedPlayer.IsEnemy)
                {
                    Log.Add(new ArenaTurnEnd((ArenaCharacter)selectedPlayer.Clone(), Turn));
                    Turn++;
                }

                foreach (var other in players)
                {
                    var spdMultiplier = 0.6m;
                    var current = players.GetPriority(other);
                    if (other.usedSkill is not null && other.usedSkill is not ArenaNormalAttack)
                    {
                        spdMultiplier = 0.9m;
                    }

                    var speed = current * spdMultiplier;
                    players.UpdatePriority(other, speed);
                }

                players.Enqueue(selectedPlayer, TurnPriority / selectedPlayer.SPD);
            }

            return Log;
        }

        private static (ArenaCharacter, ArenaLog.ArenaResult) GetBattleResult(
            IReadOnlyCollection<ArenaCharacter> deadPlayers)
        {
            if (deadPlayers.Count > 1)
            {
                var enemy = deadPlayers.First(x => x.IsEnemy);
                return (enemy, ArenaLog.ArenaResult.Win);
            }

            var player = deadPlayers.First();
            return (player, player.IsEnemy ? ArenaLog.ArenaResult.Win : ArenaLog.ArenaResult.Lose);
        }

        private static SimplePriorityQueue<ArenaCharacter, decimal> SpawnPlayers(
            ArenaSimulator simulator,
            ArenaPlayerDigest challengerDigest,
            ArenaPlayerDigest enemyDigest,
            ArenaSimulatorSheets simulatorSheets,
            ArenaLog log,
            List<StatModifier> challengerCollectionModifiers,
            List<StatModifier> enemyCollectionModifiers,
            bool setExtraValueBuffBeforeGetBuffs = false)
        {
            var challenger = new ArenaCharacter(
                simulator,
                challengerDigest,
                simulatorSheets,
                simulator.HpModifier,
                challengerCollectionModifiers,
                setExtraValueBuffBeforeGetBuffs: setExtraValueBuffBeforeGetBuffs);

            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                simulatorSheets,
                simulator.HpModifier,
                enemyCollectionModifiers,
                isEnemy: true,
                setExtraValueBuffBeforeGetBuffs: setExtraValueBuffBeforeGetBuffs);

            challenger.Spawn(enemy);
            enemy.Spawn(challenger);

            log.Add(new ArenaSpawnCharacter((ArenaCharacter)challenger.Clone()));
            log.Add(new ArenaSpawnCharacter((ArenaCharacter)enemy.Clone()));

            var players = new SimplePriorityQueue<ArenaCharacter, decimal>();
            players.Enqueue(challenger, TurnPriority / challenger.SPD);
            players.Enqueue(enemy, TurnPriority / enemy.SPD);
            return players;
        }
    }
}
