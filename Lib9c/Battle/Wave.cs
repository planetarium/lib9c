using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Battle
{
    /// <summary>
    /// Represents a wave of enemies in a battle.
    /// </summary>
    public class Wave
    {
        private readonly List<Enemy> _enemies = new List<Enemy>();

        /// <summary>
        /// Gets or sets whether this wave contains a boss enemy.
        /// </summary>
        public bool HasBoss;

        /// <summary>
        /// Adds an enemy to this wave.
        /// </summary>
        /// <param name="enemy">The enemy to add.</param>
        public void Add(Enemy enemy)
        {
            _enemies.Add(enemy);
        }

        /// <summary>
        /// Spawns all enemies in this wave into the simulator.
        /// </summary>
        /// <param name="simulator">The simulator to spawn enemies into.</param>
        public void Spawn(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAI();
            }

            if (simulator.LogEvent)
            {
                var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
                var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
                simulator.Log.Add(spawnWave);
            }
        }

        [Obsolete("Use Spawn")]
        public void SpawnV1(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.Stats.SPD);
                enemy.InitAIV1();
            }

            var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
            var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
            simulator.Log.Add(spawnWave);
        }

        [Obsolete("Use Spawn")]
        public void SpawnV2(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAIV2();
            }

            // Skip log event for now due to type conversion issues
            var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
            var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
            simulator.Log.Add(spawnWave);
        }
    }
}
