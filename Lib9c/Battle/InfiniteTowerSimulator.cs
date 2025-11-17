using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    /// <summary>
    /// Simulator for infinite tower battles with conditions and restrictions.
    /// Handles turn-based combat with wave progression, special conditions, and reward processing.
    /// </summary>
    public class InfiniteTowerSimulator : Simulator, IStageSimulator
    {
        /// <summary>
        /// Gets the stage ID, which is the same as the floor ID for infinite tower.
        /// </summary>
        public int StageId => FloorId;

        /// <summary>
        /// Gets the item map for collection tracking (not used in infinite tower).
        /// </summary>
        public CollectionMap ItemMap => new CollectionMap();

        /// <summary>
        /// Gets the list of food items used by the player.
        /// </summary>
        public List<Guid> Foods { get; }

        /// <summary>
        /// Gets the rune states for the player.
        /// </summary>
        public AllRuneState RuneStates { get; }

        /// <summary>
        /// Gets the rune slot state for the player.
        /// </summary>
        public RuneSlotState RuneSlotState { get; }

        /// <summary>
        /// Gets the infinite tower ID.
        /// </summary>
        public int InfiniteTowerId { get; }

        /// <summary>
        /// Gets the floor ID being challenged.
        /// </summary>
        public int FloorId { get; }

        /// <summary>
        /// Gets the floor data containing rewards and requirements.
        /// </summary>
        public InfiniteTowerFloorSheet.Row FloorRow { get; }

        /// <summary>
        /// Gets the list of wave data for this floor.
        /// </summary>
        public List<InfiniteTowerFloorWaveSheet.WaveData> WaveRows { get; }

        /// <summary>
        /// Gets whether this floor has been cleared before.
        /// </summary>
        public bool IsCleared { get; }

        /// <summary>
        /// Gets the experience reward (always 0 for infinite tower).
        /// </summary>
        public int Exp { get; }

        /// <summary>
        /// Gets the simulator sheets containing game data.
        /// </summary>
        public SimulatorSheets SimulatorSheets { get; }

        /// <summary>
        /// Gets the enemy skill sheet for enemy abilities.
        /// </summary>
        public EnemySkillSheet EnemySkillSheet { get; }

        /// <summary>
        /// Gets the costume stat sheet for costume bonuses.
        /// </summary>
        public CostumeStatSheet CostumeStatSheet { get; }

        /// <summary>
        /// Gets the item sheet for creating reward items.
        /// </summary>
        public ItemSheet ItemSheet { get; }

        /// <summary>
        /// Gets the collection modifiers for stat bonuses.
        /// </summary>
        public List<StatModifier> CollectionModifiers { get; }

        /// <summary>
        /// Gets the infinite tower conditions that apply to this floor.
        /// </summary>
        public List<Model.InfiniteTower.InfiniteTowerCondition> Conditions { get; }

        /// <summary>
        /// Gets the turn limit for all waves combined.
        /// </summary>
        public int TurnLimit { get; }


        /// <summary>
        /// Gets the list of reward items obtained from this floor.
        /// </summary>
        public List<ItemBase> RewardItems { get; private set; } = new List<ItemBase>();

        /// <summary>
        /// Gets the fungible asset rewards (NCG, Crystal, etc.) obtained from this floor.
        /// </summary>
        public Dictionary<string, int> FungibleAssetRewards { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Turn priority constant for character queue ordering.
        /// Higher values mean higher priority (faster turns).
        /// </summary>
        private new const decimal TurnPriority = 100m;

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerSimulator class.
        /// </summary>
        /// <param name="random">Random number generator for battle calculations.</param>
        /// <param name="avatarState">The player's avatar state.</param>
        /// <param name="foods">List of food items to be consumed.</param>
        /// <param name="runeStates">Player's rune states.</param>
        /// <param name="runeSlotState">Player's rune slot configuration.</param>
        /// <param name="infiniteTowerId">The infinite tower ID.</param>
        /// <param name="floorId">The floor ID being challenged.</param>
        /// <param name="floorRow">Floor data containing rewards and requirements.</param>
        /// <param name="waveRows">List of wave data for this floor.</param>
        /// <param name="isCleared">Whether this floor has been cleared before.</param>
        /// <param name="exp">Experience reward (always 0 for infinite tower).</param>
        /// <param name="simulatorSheets">Game data sheets for simulation.</param>
        /// <param name="enemySkillSheet">Enemy skill data.</param>
        /// <param name="costumeStatSheet">Costume stat bonuses.</param>
        /// <param name="itemSheet">Item data for reward creation.</param>
        /// <param name="collectionModifiers">Collection-based stat modifiers.</param>
        /// <param name="buffLimitSheet">Buff limit data.</param>
        /// <param name="buffLinkSheet">Buff link data.</param>
        /// <param name="conditions">Infinite tower conditions for this floor.</param>
        /// <param name="shatterStrikeMaxDamage">Maximum damage for shatter strike.</param>
        /// <param name="turnLimit">Turn limit for all waves combined (default: 200).</param>
        /// <param name="logEvent">Whether to log battle events (default: true).</param>
        public InfiniteTowerSimulator(
            Libplanet.Action.IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            AllRuneState runeStates,
            RuneSlotState runeSlotState,
            int infiniteTowerId,
            int floorId,
            InfiniteTowerFloorSheet.Row floorRow,
            List<InfiniteTowerFloorWaveSheet.WaveData> waveRows,
            bool isCleared,
            int exp,
            SimulatorSheets simulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            ItemSheet itemSheet,
            List<StatModifier> collectionModifiers,
            BuffLimitSheet buffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            List<Model.InfiniteTower.InfiniteTowerCondition> conditions,
            int shatterStrikeMaxDamage,
            int turnLimit = 200,
            bool logEvent = true) : base(random, avatarState, foods, simulatorSheets, logEvent)
        {
            // Store basic properties
            Foods = foods;
            RuneStates = runeStates;
            RuneSlotState = runeSlotState;
            InfiniteTowerId = infiniteTowerId;
            FloorId = floorId;
            FloorRow = floorRow;
            WaveRows = waveRows;
            IsCleared = isCleared;
            Exp = exp;
            SimulatorSheets = simulatorSheets;
            EnemySkillSheet = enemySkillSheet;
            CostumeStatSheet = costumeStatSheet;
            ItemSheet = itemSheet;
            CollectionModifiers = collectionModifiers;
            BuffLimitSheet = buffLimitSheet;
            BuffLinkSheet = buffLinkSheet;
            Conditions = conditions;
            TurnLimit = turnLimit;
            LogEvent = logEvent;

            // Configure player stats and runes
            var runeOptionSheet = simulatorSheets.RuneOptionSheet;
            var skillSheet = simulatorSheets.SkillSheet;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates, simulatorSheets.RuneListSheet, simulatorSheets.RuneLevelBonusSheet
            );

            // Get equipped runes
            var equippedRune = new List<RuneState>();
            foreach (var runeInfo in runeSlotState.GetEquippedRuneSlotInfos())
            {
                if (runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    equippedRune.Add(runeState);
                }
            }

            // Configure player stats with all modifiers
            Player.ConfigureStats(costumeStatSheet, equippedRune, runeOptionSheet, runeLevelBonus,
                skillSheet, collectionModifiers);

            // Set rune skills last, as they depend on total calculated stats
            Player.SetRuneSkills(equippedRune, runeOptionSheet, skillSheet);

            // Set additional player properties
            Player.SetCostumeStat(costumeStatSheet);
            Player.Simulator = this;
        }

        /// <summary>
        /// Sets the infinite tower conditions for this floor.
        /// This method allows setting conditions after simulator creation.
        /// The conditions will be applied when Simulate() is called.
        /// </summary>
        /// <param name="conditions">The infinite tower conditions to apply.</param>
        public void SetConditions(List<Model.InfiniteTower.InfiniteTowerCondition> conditions)
        {
            // This method allows setting conditions after simulator creation
            // The conditions will be applied when Simulate() is called
        }

        /// <summary>
        /// Simulates the infinite tower battle with wave progression and turn-based combat.
        /// Processes all waves sequentially with integrated turn limit and applies conditions.
        /// </summary>
        public void Simulate()
        {
            // Initialize battle log
            Log.stageId = FloorId;
            Log.waveCount = WaveRows.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;

            // Initialize player and reset turn counter
            Player.Spawn();
            TurnNumber = 0;

            // Apply infinite tower conditions to player before simulation
            ApplyConditionsToPlayer();

            // Simulate each wave sequentially with integrated turn limit
            for (int waveIndex = 0; waveIndex < WaveRows.Count; waveIndex++)
            {
                var waveData = WaveRows[waveIndex];
                WaveNumber = waveIndex + 1;
                WaveTurn = 1;

                // Create and initialize character queue for this wave
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                // Create enemies for this wave based on wave data
                var enemies = CreateEnemiesForWave(waveData);

                // Log wave spawn event if logging is enabled
                if (LogEvent)
                {
                    var spawnWave = new SpawnWave(null, WaveNumber, WaveTurn, enemies, false);
                    Log.Add(spawnWave);
                }

                // Clear previous wave targets and add new enemies to player's target list
                Player.Targets.Clear();

                // Apply conditions to enemies and add them to character queue
                foreach (var enemy in enemies)
                {
                    ApplyConditionsToEnemy(enemy);
                    Player.Targets.Add(enemy);
                    Characters.Enqueue(enemy, TurnPriority / enemy.SPD);
                    enemy.InitAI();
                }

                // Simulate the wave battle with integrated turn limit
                var waveResult = SimulateWaveBattle();

                // Check if wave was cleared successfully
                if (!waveResult.IsClear)
                {
                    Log.result = waveResult.Result;
                    return;
                }

                // Wave cleared successfully, update cleared wave number
                Log.clearedWaveNumber = WaveNumber;
            }

            // All waves cleared successfully
            Log.result = BattleLog.Result.Win;
            if (!IsCleared)
            {
                Log.newlyCleared = true;
            }

            // Process rewards for successful floor completion
            ProcessRewards();
        }

        /// <summary>
        /// Applies infinite tower conditions to the player character.
        /// </summary>
        private void ApplyConditionsToPlayer()
        {
            // Apply conditions to player using targeting
            ApplyConditionsToCharacter(Player, isPlayer: true);
        }

        /// <summary>
        /// Processes rewards for successful floor completion.
        /// Handles both fungible asset rewards (NCG, Crystal) and item rewards.
        /// Note: Infinite Tower does not provide experience rewards.
        /// </summary>
        private void ProcessRewards()
        {
            // Process fungible asset rewards (NCG, Crystal, etc.)
            ProcessFungibleAssetRewards();

            // Process item rewards (equipment, materials, costumes, etc.)
            ProcessItemRewards();
        }

        /// <summary>
        /// Processes fungible asset rewards (NCG, Crystal, etc.) from the floor data.
        /// Stores the rewards in FungibleAssetRewards dictionary for later use.
        /// </summary>
        private void ProcessFungibleAssetRewards()
        {
            if (FloorRow == null) return;

            var fungibleAssetRewards = FloorRow.GetFungibleAssetRewards();
            foreach (var (ticker, amount) in fungibleAssetRewards)
            {
                if (amount > 0)
                {
                    FungibleAssetRewards[ticker] = amount;
                }
            }
        }

        /// <summary>
        /// Processes item rewards from the floor data.
        /// Creates items using ItemSheet and stores them in RewardItems list.
        /// Also adds log events for item drops and rewards if LogEvent is enabled.
        /// </summary>
        private void ProcessItemRewards()
        {
            if (FloorRow == null) return;

            var itemRewards = FloorRow.GetItemRewards();
            if (itemRewards.Count > 0)
            {
                foreach (var (itemId, count) in itemRewards)
                {
                    if (count > 0)
                    {
                        // Try to find item in ItemSheet (supports all item types)
                        ItemBase item = null;

                        if (ItemSheet.TryGetValue(itemId, out var itemRow))
                        {
                            item = ItemFactory.CreateItem(itemRow, Random);
                        }

                        if (item != null)
                        {
                            // Add multiple copies if count > 1
                            for (int i = 0; i < count; i++)
                            {
                                RewardItems.Add(item);
                            }
                        }
                    }
                }

                // Add log events for all rewards (items + fungible assets)
                if (LogEvent && (RewardItems.Count > 0 || FungibleAssetRewards.Count > 0))
                {
                    // Add drop box log event for all items
                    if (RewardItems.Count > 0)
                    {
                        var dropBox = new DropBox(null, RewardItems);
                        Log.Add(dropBox);
                    }

                    // Add get reward log event for all items and fungible assets
                    if (RewardItems.Count > 0 || FungibleAssetRewards.Count > 0)
                    {
                        var getReward = new GetReward(null, RewardItems, FungibleAssetRewards);
                        Log.Add(getReward);
                    }
                }
            }
        }


        /// <summary>
        /// Checks if a condition type is a stat modifier (not a restriction).
        /// </summary>
        /// <param name="conditionType">The condition type to check</param>
        /// <returns>True if it's a stat modifier, false if it's a restriction</returns>

        /// <summary>
        /// Creates enemies for a specific wave based on wave data.
        /// Supports multiple monster types per wave, similar to StageSimulator.
        /// </summary>
        /// <param name="waveData">The wave data containing monster information.</param>
        /// <returns>A list of enemies for this wave.</returns>
        /// <exception cref="InvalidOperationException">Thrown when player is not initialized or character is not found.</exception>
        private List<Enemy> CreateEnemiesForWave(InfiniteTowerFloorWaveSheet.WaveData waveData)
        {
            var enemies = new List<Enemy>();

            if (Player == null)
            {
                throw new InvalidOperationException("Player must be initialized before creating enemies.");
            }

            // Create enemies for each monster type in the wave
            foreach (var monsterData in waveData.Monsters)
            {
                // Get character row from the sheet - throw exception if not found
                if (!SimulatorSheets.CharacterSheet.TryGetValue(monsterData.CharacterId, out var characterRow))
                {
                    throw new InvalidOperationException($"Character with ID {monsterData.CharacterId} not found in CharacterSheet.");
                }

                // Create the specified number of enemies for this monster type
                for (int i = 0; i < monsterData.Count; i++)
                {
                    // Create stats with monster data (level, etc.)
                    var stat = new CharacterStats(characterRow, monsterData.Level);

                    // Create enemy with player as target
                    var enemy = new Enemy(Player, stat, characterRow, characterRow.ElementalType);

                    // Apply infinite tower conditions to enemy
                    ApplyConditionsToCharacter(enemy, isPlayer: false);

                    enemies.Add(enemy);
                }
            }

            return enemies;
        }

        /// <summary>
        /// Applies infinite tower conditions to an enemy character.
        /// </summary>
        /// <param name="enemy">The enemy to apply conditions to.</param>
        private void ApplyConditionsToEnemy(Enemy enemy)
        {
            // Apply conditions to enemy using targeting
            ApplyConditionsToCharacter(enemy, isPlayer: false);
        }

        /// <summary>
        /// Applies infinite tower conditions to any character (Player, Enemy, Boss, etc.).
        /// This unified method works for all CharacterBase subclasses and applies stat modifiers
        /// based on the condition's targeting rules.
        /// </summary>
        /// <param name="character">The character to apply conditions to.</param>
        /// <param name="isPlayer">Whether the character is a player (affects targeting rules).</param>
        public void ApplyConditionsToCharacter(CharacterBase character, bool isPlayer)
        {
            if (Conditions == null || !Conditions.Any())
            {
                return;
            }

            // Convert conditions to stat modifiers based on targeting
            var conditionStatModifiers = new List<StatModifier>();

            foreach (var condition in Conditions)
            {
                // Apply targeting logic
                if (!ShouldApplyCondition(condition, isPlayer))
                {
                    continue;
                }

                // Convert condition to stat modifier
                var statModifier = condition.GetStatModifier();
                if (statModifier != null)
                {
                    conditionStatModifiers.Add(statModifier);
                }
            }

            // Apply conditions to character's stats (temporary for simulation only)
            if (conditionStatModifiers.Any())
            {
                character.Stats.SetConditions(conditionStatModifiers);
            }
        }

        /// <summary>
        /// Determines if a condition should be applied to a character based on targeting rules.
        /// Checks if any of the target types in the list match the character type (OR logic).
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="isPlayer">Whether the target character is a player.</param>
        /// <returns>True if the condition should be applied to this character.</returns>
        private bool ShouldApplyCondition(Model.InfiniteTower.InfiniteTowerCondition condition, bool isPlayer)
        {
            if (condition.TargetType == null || !condition.TargetType.Any())
            {
                return false;
            }

            // Check if any target type matches (OR logic)
            return condition.TargetType.Any(targetType => targetType switch
            {
                SkillTargetType.Self => isPlayer,
                SkillTargetType.Enemy => !isPlayer,
                SkillTargetType.Enemies => !isPlayer,
                SkillTargetType.Ally => isPlayer,
                _ => false
            });
        }

        /// <summary>
        /// Simulates a single wave battle with integrated turn limit.
        /// Handles turn-based combat with character queue and checks for win/lose conditions.
        /// </summary>
        /// <returns>The result of the wave battle.</returns>
        private WaveResult SimulateWaveBattle()
        {
            var waveResult = new WaveResult { IsClear = false };

            while (true)
            {
                // Check turn limit for all waves combined
                if (TurnNumber > TurnLimit)
                {
                    waveResult.IsClear = false;
                    waveResult.Result = BattleLog.Result.TimeOver;
                    break;
                }

                // Check if character queue is empty
                if (!Characters.TryDequeue(out var character))
                {
                    break;
                }

                // Execute character's turn
                // TurnNumber is incremented in Player.EndTurn() for player actions
                // Enemy actions don't increment TurnNumber (consistent with other simulators)
                character.Tick();

                // Update character priorities after action
                UpdateCharacterPriorities();

                // Check if player is dead
                if (Player.IsDead)
                {
                    waveResult.IsClear = false;
                    waveResult.Result = BattleLog.Result.Lose;
                    break;
                }

                // Check if all enemies are dead (wave cleared)
                var aliveEnemies = Characters.Where(c => c is Enemy && !c.IsDead).ToList();
                if (!aliveEnemies.Any())
                {
                    waveResult.IsClear = true;
                    waveResult.Result = BattleLog.Result.Win;
                    break;
                }

                // Re-enqueue character for next turn
                Characters.Enqueue(character, TurnPriority / character.SPD);
            }

            return waveResult;
        }

        /// <summary>
        /// Updates character priorities based on their actions and speed.
        /// Applies different speed multipliers based on character type and actions taken.
        /// Based on the pattern from other simulators.
        /// </summary>
        private void UpdateCharacterPriorities()
        {
            foreach (var other in Characters)
            {
                var spdMultiplier = 0.6m;
                var current = Characters.GetPriority(other);

                // Apply different speed multipliers based on character type and actions
                if (other == Player && other.usedSkill is not null &&
                    other.usedSkill is not Nekoyume.Model.BattleStatus.NormalAttack)
                {
                    spdMultiplier = 0.9m;
                }

                var speed = current * spdMultiplier;
                Characters.UpdatePriority(other, speed);
            }
        }

        /// <summary>
        /// Gets the reward items obtained from this floor.
        /// Returns the list of items that were dropped during the battle.
        /// </summary>
        public override IEnumerable<ItemBase> Reward => RewardItems;
    }

    /// <summary>
    /// Log for infinite tower battle results.
    /// Contains information about the battle outcome and player performance.
    /// </summary>
    public class InfiniteTowerLog
    {
        /// <summary>
        /// Gets or sets the battle result (Win/Lose).
        /// </summary>
        public InfiniteTowerResult Result { get; set; }

        /// <summary>
        /// Gets or sets whether the floor was cleared.
        /// </summary>
        public bool IsClear { get; set; }

        /// <summary>
        /// Gets or sets the number of waves cleared.
        /// </summary>
        public int ClearedWaveNumber { get; set; }

        /// <summary>
        /// Gets or sets the player's remaining HP after battle.
        /// </summary>
        public int PlayerHpRemaining { get; set; }

        /// <summary>
        /// Gets or sets the number of enemies defeated.
        /// </summary>
        public int EnemiesDefeated { get; set; }

        /// <summary>
        /// Represents the possible results of an infinite tower battle.
        /// </summary>
        public enum InfiniteTowerResult
        {
            /// <summary>
            /// The player won the battle.
            /// </summary>
            Win,

            /// <summary>
            /// The player lost the battle.
            /// </summary>
            Lose
        }
    }

    /// <summary>
    /// Result of a single wave battle.
    /// Contains information about the wave completion status and battle outcome.
    /// </summary>
    public class WaveResult
    {
        /// <summary>
        /// Gets or sets whether the wave was cleared.
        /// </summary>
        public bool IsClear { get; set; }

        /// <summary>
        /// Gets or sets the player's remaining HP after the wave.
        /// </summary>
        public int PlayerHpRemaining { get; set; }

        /// <summary>
        /// Gets or sets the number of enemies defeated in this wave.
        /// </summary>
        public int EnemiesDefeated { get; set; }

        /// <summary>
        /// Gets or sets the battle result for this wave.
        /// </summary>
        public BattleLog.Result Result { get; set; }
    }

    /// <summary>
    /// Represents a wave in the infinite tower.
    /// Contains information about the enemies that will spawn in this wave.
    /// </summary>
    public class InfiniteTowerWave
    {
        /// <summary>
        /// Gets the wave number.
        /// </summary>
        public int WaveNumber { get; }

        /// <summary>
        /// Gets the enemy character ID for this wave.
        /// </summary>
        public int EnemyId { get; }

        /// <summary>
        /// Gets the number of enemies to spawn in this wave.
        /// </summary>
        public int EnemyCount { get; }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerWave class.
        /// </summary>
        /// <param name="waveNumber">The wave number.</param>
        /// <param name="enemyId">The enemy character ID.</param>
        /// <param name="enemyCount">The number of enemies to spawn.</param>
        public InfiniteTowerWave(int waveNumber, int enemyId, int enemyCount)
        {
            WaveNumber = waveNumber;
            EnemyId = enemyId;
            EnemyCount = enemyCount;
        }
    }

}
