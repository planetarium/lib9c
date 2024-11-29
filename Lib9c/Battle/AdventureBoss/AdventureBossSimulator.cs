// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.BattleStatus.AdventureBoss;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Priority_Queue;
using NormalAttack = Nekoyume.Model.BattleStatus.NormalAttack;

namespace Nekoyume.Battle.AdventureBoss
{
    public class AdventureBossSimulator : Simulator, IStageSimulator
    {
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;

        public CollectionMap ItemMap { get; private set; } = new ();
        public EnemySkillSheet EnemySkillSheet { get; }

        public int BossId { get; }
        public int StageId => FloorId;
        public int FloorId { get; }
        private int TurnLimit { get; }
        private int StageBuffSkillId { get; }
        public override IEnumerable<ItemBase> Reward => _waveRewards;

        public AdventureBossSimulator(
            int bossId,
            int floorId,
            IRandom random,
            AvatarState avatarState,
            SimulatorSheets simulatorSheets,
            bool logEvent = true,
            long shatterStrikeMaxDamage = 400_000
        ) : base(
            random, avatarState, new List<Guid>(), simulatorSheets, logEvent, shatterStrikeMaxDamage
        )
        {
            BossId = bossId;
            FloorId = floorId;
        }

        public AdventureBossSimulator(
            int bossId,
            int floorId,
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            AllRuneState runeStates,
            RuneSlotState runeSlotState,
            AdventureBossFloorSheet.Row floorRow,
            AdventureBossFloorWaveSheet.Row floorWaveRow,
            SimulatorSheets simulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            List<ItemBase> waveRewards,
            List<StatModifier> collectionModifiers,
            BuffLimitSheet buffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            bool logEvent = true,
            long shatterStrikeMaxDamage = 400_000
        )
            : base(
                random,
                avatarState,
                foods,
                simulatorSheets,
                logEvent,
                shatterStrikeMaxDamage
            )
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

            Player.ConfigureStats(costumeStatSheet, equippedRune, runeOptionSheet, runeLevelBonus,
                skillSheet, collectionModifiers);

            // call SetRuneSkills last. because rune skills affect from total calculated stats
            Player.SetRuneSkills(equippedRune, runeOptionSheet, skillSheet);

            _waves = new List<Wave>();
            _waveRewards = waveRewards;
            BossId = bossId;
            FloorId = floorId;
            EnemySkillSheet = enemySkillSheet;
            TurnLimit = floorRow.TurnLimit;
            StageBuffSkillId = floorRow.StageBuffSkillId;

            SetWave(floorRow, floorWaveRow);
        }

        public static List<ItemBase> GetWaveRewards(
            IRandom random,
            AdventureBossFloorSheet.Row floorRow,
            MaterialItemSheet materialItemSheet,
            int playCount = 1)
        {
            var maxCountForItemDrop = random.Next(
                floorRow.MinDropItem,
                floorRow.MaxDropItem + 1);
            var waveRewards = new List<ItemBase>();
            for (var i = 0; i < playCount; i++)
            {
                var itemSelector = SetItemSelector(floorRow, random);
                var rewards = SetReward(
                    itemSelector,
                    maxCountForItemDrop,
                    random,
                    materialItemSheet
                );

                waveRewards.AddRange(rewards);
            }

            return waveRewards;
        }

        public Player Simulate()
        {
            Log.stageId = FloorId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;
            for (var wv = 0; wv < _waves.Count; wv++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = wv + 1;
                WaveTurn = 1;
                _waves[wv].Spawn(this);

                if (StageBuffSkillId != 0)
                {
                    var skillRow = SkillSheet.OrderedList.First(row => row.Id == StageBuffSkillId);
                    var skill = SkillFactory.Get(skillRow, default, 100, default, StatType.NONE);
                    var buffs = BuffFactory.GetBuffs(
                        Player.Stats,
                        skill,
                        SkillBuffSheet,
                        StatBuffSheet,
                        SkillActionBuffSheet,
                        ActionBuffSheet
                    );
                    var stageBuff = skill.Use(Player, 0, buffs, LogEvent);
                    if (LogEvent)
                    {
                        Log.Add(new StageBuff(stageBuff.SkillId, stageBuff.Character,
                            stageBuff.SkillInfos, stageBuff.BuffInfos));
                    }
                }

                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        Result = wv == 0 ? BattleLog.Result.Lose : BattleLog.Result.TimeOver;
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                    {
                        break;
                    }

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        Result = wv == 0 ? BattleLog.Result.Lose : BattleLog.Result.Win;
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;
                        Log.newlyCleared = true;
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var spdMultiplier = 0.6m;
                        var current = Characters.GetPriority(other);
                        if (other == Player && other.usedSkill is not null &&
                            other.usedSkill is not NormalAttack)
                        {
                            spdMultiplier = 0.9m;
                        }

                        var speed = current * spdMultiplier;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit || Player.IsDead)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        public void AddBreakthrough(List<int> floorIdList,
            AdventureBossFloorWaveSheet adventureBossFloorWaveSheet)
        {
            if (Log.events.Count == 0)
            {
                Log.events.Add(new SpawnPlayer((CharacterBase)Player.Clone()));
            }

            // Add event in reversed order to keep insert position
            for (var i = 0; i < floorIdList.Count; i++)
            {
                var floorId = floorIdList[i];
                var floorWave = adventureBossFloorWaveSheet[floorId].Waves[0];
                Log.events.Insert(i + 1, new Breakthrough(Player, floorId, floorWave.Monsters));
            }
        }

        private void SetWave(AdventureBossFloorSheet.Row floorRow,
            AdventureBossFloorWaveSheet.Row floorWaveRow)
        {
            var enemyStatModifiers = floorRow.EnemyInitialStatModifiers;
            var waves = floorWaveRow.Waves;
            foreach (var wave in
                     waves.Select(e => SpawnWave(e, enemyStatModifiers))
                    )
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(
            AdventureBossFloorWaveSheet.WaveData waveData,
            IReadOnlyList<StatModifier> initialStatModifiers)
        {
            var wave = new Wave();
            foreach (var monsterData in waveData.Monsters)
            {
                for (var i = 0; i < monsterData.Count; i++)
                {
                    CharacterSheet.TryGetValue(
                        monsterData.CharacterId,
                        out var row,
                        true);

                    var stat = new CharacterStats(row, monsterData.Level, initialStatModifiers);
                    var enemyModel = new Enemy(Player, stat, row, row.ElementalType);
                    wave.Add(enemyModel);
                    wave.HasBoss = waveData.HasBoss;
                }
            }

            return wave;
        }

        public static WeightedSelector<AdventureBossFloorSheet.RewardData> SetItemSelector(
            AdventureBossFloorSheet.Row floorRow, IRandom random
        )
        {
            var itemSelector = new WeightedSelector<AdventureBossFloorSheet.RewardData>(random);
            foreach (var r in floorRow.Rewards)
            {
                itemSelector.Add(r, r.Ratio);
            }

            return itemSelector;
        }

        public static List<ItemBase> SetReward(
            WeightedSelector<AdventureBossFloorSheet.RewardData> itemSelector,
            int maxCount,
            IRandom random,
            MaterialItemSheet materialItemSheet
        )
        {
            var reward = new List<ItemBase>();

            while (reward.Count < maxCount)
            {
                try
                {
                    var data = itemSelector.Select(1).First();
                    if (materialItemSheet.TryGetValue(data.ItemId, out var itemData))
                    {
                        var count = random.Next(data.Min, data.Max + 1);
                        for (var i = 0; i < count; i++)
                        {
                            var item = ItemFactory.CreateMaterial(itemData);
                            if (reward.Count < maxCount)
                            {
                                reward.Add(item);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (ListEmptyException)
                {
                    break;
                }
            }

            reward = reward.OrderBy(r => r.Id).ToList();
            return reward;
        }
    }
}
