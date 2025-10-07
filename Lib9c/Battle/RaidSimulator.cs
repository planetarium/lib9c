using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Helper;
using Lib9c.Model.BattleStatus;
using Lib9c.Model.Character;
using Lib9c.Model.Item;
using Lib9c.Model.Stat;
using Lib9c.Model.State;
using Lib9c.TableData;
using Lib9c.TableData.Character;
using Lib9c.TableData.Item;
using Lib9c.TableData.Skill;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Priority_Queue;
using NormalAttack = Lib9c.Model.BattleStatus.NormalAttack;

namespace Lib9c.Battle
{
    public class RaidSimulator : Simulator
    {
        public int BossId { get; private set; }
        public long DamageDealt { get; private set; }
        public List<FungibleAssetValue> AssetReward { get; private set; } = new List<FungibleAssetValue>();
        public override IEnumerable<ItemBase> Reward => _reward;
        private readonly List<RaidBoss> _waves;
        private List<ItemBase> _reward;

        private WorldBossBattleRewardSheet _worldBossBattleRewardSheet;
        private RuneWeightSheet _runeWeightSheet;
        private RuneSheet _runeSheet;
        private MaterialItemSheet _materialItemSheet;
        private WorldBossCharacterSheet.Row _currentBossRow;

        public RaidSimulator(int bossId,
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            AllRuneState runeStates,
            RuneSlotState runeSlotState,
            RaidSimulatorSheets simulatorSheets,
            CostumeStatSheet costumeStatSheet,
            List<StatModifier> collectionModifiers,
            BuffLimitSheet buffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            long shatterStrikeMaxDamage = 400_000) : base(random, avatarState, foods, simulatorSheets,
            shatterStrikeMaxDamage: shatterStrikeMaxDamage)
        {
            BuffLimitSheet = buffLimitSheet;
            BuffLinkSheet = buffLinkSheet;
            var runeOptionSheet = simulatorSheets.RuneOptionSheet;
            var skillSheet = simulatorSheets.SkillSheet;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates, simulatorSheets.RuneListSheet, simulatorSheets.RuneLevelBonusSheet
            );
            var equippedRune = new List<RuneState>();
            foreach (var runeInfo in runeSlotState.GetEquippedRuneSlotInfos())
            {
                if (runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    equippedRune.Add(runeState);
                }
            }

            Player.ConfigureStats(costumeStatSheet,
                equippedRune, runeOptionSheet, runeLevelBonus,
                skillSheet, collectionModifiers
            );

            // call SetRuneSkills last. because rune skills affect from total calculated stats
            Player.SetRuneSkills(equippedRune, runeOptionSheet, skillSheet);

            BossId = bossId;
            _waves = new List<RaidBoss>();

            if (!simulatorSheets.WorldBossCharacterSheet.TryGetValue(bossId, out _currentBossRow))
                throw new SheetRowNotFoundException(nameof(WorldBossCharacterSheet), bossId);

            if (!simulatorSheets.WorldBossActionPatternSheet.TryGetValue(bossId, out var patternRow))
                throw new SheetRowNotFoundException(nameof(WorldBossActionPatternSheet), bossId);

            _worldBossBattleRewardSheet = simulatorSheets.WorldBossBattleRewardSheet;
            _runeWeightSheet = simulatorSheets.RuneWeightSheet;
            _runeSheet = simulatorSheets.RuneSheet;
            _materialItemSheet = simulatorSheets.MaterialItemSheet;

            SetEnemies(_currentBossRow, patternRow);
        }

        private void SetEnemies(
            WorldBossCharacterSheet.Row characterRow,
            WorldBossActionPatternSheet.Row patternRow)
        {
            for (var i = 0; i < characterRow.WaveStats.Count; ++i)
            {
                var enemyModel = new RaidBoss(
                    Player,
                    characterRow,
                    patternRow,
                    characterRow.WaveStats[i],
                    true);
                _waves.Add(enemyModel);
            }
        }

        public void SpawnBoss(RaidBoss raidBoss)
        {
            Player.Targets.Add(raidBoss);
            Characters.Enqueue(raidBoss, TurnPriority / raidBoss.SPD);
            raidBoss.InitAI();

            var enemies = new List<Enemy>() { new RaidBoss(raidBoss) };
            var spawnWave = new SpawnWave(null, WaveNumber, WaveTurn, enemies, true);
            Log.Add(spawnWave);
        }


        public BattleLog Simulate()
        {
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;

            var turnLimitExceeded = false;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;

                var currentWaveBoss = _waves[i];
                SpawnBoss(currentWaveBoss);

                var waveStatData = currentWaveBoss.RowData.WaveStats
                    .FirstOrDefault(x => x.Wave == WaveNumber);
                while (true)
                {
                    // On turn limit exceeded, player loses.
                    if (TurnNumber > waveStatData.TurnLimit)
                    {
                        turnLimitExceeded = true;
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    if (!Characters.TryDequeue(out var character))
                        break;

                    // Boss enrages on EnrageTurn. (EnrageTurn is counted in individual waves.)
                    if (WaveTurn >= waveStatData.EnrageTurn &&
                        !currentWaveBoss.Enraged)
                    {
                        currentWaveBoss.Enrage();
                    }

                    character.Tick();

                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // If targets are all gone
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var spdMultiplier = 0.6m;
                        var current = Characters.GetPriority(other);
                        if (other == Player && other.usedSkill is not null && other.usedSkill is not NormalAttack)
                        {
                            spdMultiplier = 0.9m;
                        }

                        var speed = current * spdMultiplier;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // If turn limit exceeded or player died
                if (turnLimitExceeded || Player.IsDead)
                    break;
            }

            foreach (var wave in _waves)
            {
                var leftHp = wave.CurrentHP > 0 ? wave.CurrentHP : 0;
                DamageDealt += wave.HP - leftHp;
            }

            var rank =  WorldBossHelper.CalculateRank(_currentBossRow, DamageDealt);
            var rewards = WorldBossHelper.CalculateReward(
                rank,
                BossId,
                _runeWeightSheet,
                _worldBossBattleRewardSheet,
                _runeSheet,
                _materialItemSheet,
                Random);
            AssetReward = rewards.assets;

            var materialReward = new List<ItemBase>();
#pragma warning disable LAA1002
            foreach (var reward in rewards.materials)
#pragma warning restore LAA1002
            {
                for (var i = 0; i < reward.Value; i++)
                {
                    materialReward.Add(reward.Key);
                }
            }

            _reward = materialReward;

            Log.result = Result;
            return Log;
        }
    }
}
