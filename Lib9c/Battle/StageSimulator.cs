// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Model.Buff;
using Nekoyume.TableData;
using Priority_Queue;
using NormalAttack = Nekoyume.Model.BattleStatus.NormalAttack;
using Skill = Nekoyume.Model.Skill.Skill;

namespace Nekoyume.Battle
{
    /// <summary>
    /// Simulator for stage battles that handles wave-based combat.
    /// </summary>
    public class StageSimulator : Simulator, IStageSimulator
    {
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;
        private readonly List<Model.Skill.Skill> _skillsOnWaveStart;
        private readonly List<StageSheet.FavRewardData> _favRewards;
        private readonly int _favDropMin;
        private readonly int _favDropMax;
        private readonly BoostScheduleSheet.Row _favRewardBoost;

        /// <summary>
        /// Upper bound of the boosted count of each distinct item in one clear, as a multiple of
        /// how many times it was drawn in that clear: a <see cref="BoostScheduleSheet"/> row
        /// grants at most this many times the drawn items per clear, whatever its value. It
        /// bounds a mistyped value (e.g. <c>MUL 200</c> for <c>MUL 2.00</c>) to a tenfold
        /// over-reward, and the objects <see cref="ApplyItemRewardBoost"/> creates per clear to
        /// this many times <c>DropItemMax</c>. It is not a per-transaction bound;
        /// <c>HackAndSlashSweep</c> adds its extra units with one count per item id instead of one
        /// object per unit, so its cost does not grow with the value.
        /// </summary>
        public const int MaxItemRewardBoostFactor = 10;

        /// <summary>
        /// Gets the collection map for items.
        /// </summary>
        public CollectionMap ItemMap { get; private set; } = new CollectionMap();

        /// <summary>
        /// Gets the fungible asset rewards (e.g., Crystal) obtained from wave 2 clear.
        /// </summary>
        public Dictionary<string, int> FungibleAssetRewards { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets the enemy skill sheet.
        /// </summary>
        public EnemySkillSheet EnemySkillSheet { get; }

        /// <summary>
        /// Gets the world ID.
        /// </summary>
        private int WorldId { get; }

        /// <summary>
        /// Gets the stage ID.
        /// </summary>
        public int StageId { get; }

        /// <summary>
        /// Gets whether the stage is cleared.
        /// </summary>
        private bool IsCleared { get; }

        /// <summary>
        /// Gets the experience points.
        /// </summary>
        private int Exp { get; }

        /// <summary>
        /// Gets the turn limit for the battle.
        /// </summary>
        private int TurnLimit { get; }

        /// <summary>
        /// Gets the rewards from the battle.
        /// </summary>
        public override IEnumerable<ItemBase> Reward => _waveRewards;

        /// <summary>
        /// Initializes a new instance of the StageSimulator class.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="avatarState">The avatar state.</param>
        /// <param name="foods">The food items to use.</param>
        /// <param name="runeStates">The rune states.</param>
        /// <param name="runeSlotState">The rune slot state.</param>
        /// <param name="skillsOnWaveStart">The skills to use at wave start.</param>
        /// <param name="worldId">The world ID.</param>
        /// <param name="stageId">The stage ID.</param>
        /// <param name="stageRow">The stage row data.</param>
        /// <param name="stageWaveRow">The stage wave row data.</param>
        /// <param name="isCleared">Whether the stage is cleared.</param>
        /// <param name="exp">The experience points.</param>
        /// <param name="simulatorSheets">The simulator sheets.</param>
        /// <param name="enemySkillSheet">The enemy skill sheet.</param>
        /// <param name="costumeStatSheet">The costume stat sheet.</param>
        /// <param name="waveRewards">The item rewards already drawn for this clear.</param>
        /// <param name="collectionModifiers">The stat modifiers from activated collections.</param>
        /// <param name="buffLimitSheet">The buff limit sheet.</param>
        /// <param name="buffLinkSheet">The buff link sheet.</param>
        /// <param name="logEvent">Whether to record the battle log.</param>
        /// <param name="shatterStrikeMaxDamage">Maximum damage for shatter strike.</param>
        /// <param name="favRewardBoost">
        /// The <see cref="BoostScheduleSheet"/> row adjusting this stage's fungible asset rewards,
        /// or <c>null</c> for none. See <see cref="ApplyFavRewardBoost"/>. A caller replaying a
        /// battle must pass what the action resolved.
        /// </param>
        public StageSimulator(IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            AllRuneState runeStates,
            RuneSlotState runeSlotState,
            List<Skill> skillsOnWaveStart,
            int worldId,
            int stageId,
            StageSheet.Row stageRow,
            StageWaveSheet.Row stageWaveRow,
            bool isCleared,
            int exp,
            SimulatorSheets simulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            List<ItemBase> waveRewards,
            List<StatModifier> collectionModifiers,
            BuffLimitSheet buffLimitSheet,
            BuffLinkSheet buffLinkSheet,
            bool logEvent = true,
            long shatterStrikeMaxDamage = 400_000,
            BoostScheduleSheet.Row favRewardBoost = null
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
            _favRewards = stageRow.FavRewards;
            _favDropMin = stageRow.FavDropMin;
            _favDropMax = stageRow.FavDropMax;
            _favRewardBoost = favRewardBoost;
            WorldId = worldId;
            StageId = stageId;
            IsCleared = isCleared;
            Exp = exp;
            EnemySkillSheet = enemySkillSheet;
            TurnLimit = stageRow.TurnLimit;
            _skillsOnWaveStart = skillsOnWaveStart;

            SetWave(stageRow, stageWaveRow);
        }

        /// <summary>
        /// Draws the fungible asset rewards of one clear of <paramref name="stageRow"/>.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="stageRow">The stage being cleared.</param>
        /// <param name="favRewardBoost">
        /// The <see cref="BoostScheduleSheet"/> row adjusting the amounts, or <c>null</c> for none.
        /// See <see cref="ApplyFavRewardBoost"/>. It never changes the random draws.
        /// </param>
        /// <returns>The amount drawn for each ticker.</returns>
        public static List<(string ticker, int amount)> GetFavWaveRewards(
            IRandom random,
            StageSheet.Row stageRow,
            BoostScheduleSheet.Row favRewardBoost = null)
        {
            return GetFavWaveRewards(
                random,
                stageRow.FavRewards,
                stageRow.FavDropMin,
                stageRow.FavDropMax,
                favRewardBoost);
        }

        /// <summary>
        /// Applies a <see cref="BoostScheduleSheet"/> row to the fungible asset rewards of one
        /// clear.
        /// </summary>
        /// <param name="rewards">The amount drawn for each ticker.</param>
        /// <param name="favRewardBoost">The row to apply, or <c>null</c> for none.</param>
        /// <returns>
        /// <paramref name="rewards"/> itself when <paramref name="favRewardBoost"/> is
        /// <c>null</c>; otherwise a new list with each positive amount replaced by
        /// <see cref="BoostScheduleSheet.Row.Apply"/> of it, in the same order. A ticker whose
        /// adjusted amount is 0 is left out so that nothing mints a zero amount.
        /// </returns>
        /// <remarks>
        /// Draws nothing, so the random sequence is the same as without a row. A boost adjusts
        /// what was drawn and never creates a reward: a ticker that was not drawn, or drawn as 0,
        /// stays as it is.
        /// </remarks>
        public static List<(string ticker, int amount)> ApplyFavRewardBoost(
            List<(string ticker, int amount)> rewards,
            BoostScheduleSheet.Row favRewardBoost)
        {
            if (favRewardBoost is null)
            {
                return rewards;
            }

            var result = new List<(string ticker, int amount)>(rewards.Count);
            foreach (var (ticker, amount) in rewards)
            {
                if (amount <= 0)
                {
                    result.Add((ticker, amount));
                    continue;
                }

                var boosted = favRewardBoost.Apply(amount);
                if (boosted > 0)
                {
                    result.Add((ticker, boosted));
                }
            }

            return result;
        }

        /// <summary>
        /// Applies a <see cref="BoostScheduleSheet"/> row to the item rewards of one clear, one
        /// object per unit.
        /// </summary>
        /// <param name="rewards">The items drawn in one clear, one object per unit.</param>
        /// <param name="itemRewardBoost">The row to apply, or <c>null</c> for none.</param>
        /// <param name="materialItemSheet">The sheet the rewards were created from.</param>
        /// <returns>
        /// <paramref name="rewards"/> itself when <paramref name="itemRewardBoost"/> is
        /// <c>null</c>; otherwise a new list, ordered by item id, in which each distinct item
        /// drawn <c>n</c> times appears <see cref="BoostScheduleSheet.Row.Apply"/>(<c>n</c>)
        /// times, at most <see cref="MaxItemRewardBoostFactor"/> × <c>n</c>.
        /// </returns>
        /// <remarks>
        /// Draws nothing, so the random sequence is the same as without a row. <c>MUL 2</c>
        /// grants exactly twice each drawn item; <c>ADD 1</c> one more of each distinct item; a
        /// multiplier below 1 takes items away. The count can exceed the stage's
        /// <c>DropItemMax</c>, which only limits the draws. Added items are created the way
        /// <see cref="Simulator.SetRewardV2"/> creates them, so a circle is tradable as usual.
        /// Equivalent to <see cref="SplitItemRewardBoost"/> followed by
        /// <see cref="CreateItemRewardBoostExtras"/>.
        /// </remarks>
        public static List<ItemBase> ApplyItemRewardBoost(
            List<ItemBase> rewards,
            BoostScheduleSheet.Row itemRewardBoost,
            MaterialItemSheet materialItemSheet)
        {
            if (itemRewardBoost is null)
            {
                return rewards;
            }

            var extraCounts = new Dictionary<int, int>();
            var kept = SplitItemRewardBoost(rewards, itemRewardBoost, extraCounts);
            if (extraCounts.Count == 0)
            {
                return kept;
            }

            var extras = CreateItemRewardBoostExtras(extraCounts, materialItemSheet);
            var result = new List<ItemBase>(kept.Count + extras.Count);
            result.AddRange(kept);
            result.AddRange(extras);
            return result.OrderBy(item => item.Id).ToList();
        }

        /// <summary>
        /// Applies a <see cref="BoostScheduleSheet"/> row to the item rewards of one clear
        /// without creating an object per added unit: units taken away are dropped from the
        /// returned list, and units added are only counted into
        /// <paramref name="extraCounts"/>.
        /// </summary>
        /// <param name="rewards">The items drawn in one clear, one object per unit.</param>
        /// <param name="itemRewardBoost">The row to apply, or <c>null</c> for none.</param>
        /// <param name="extraCounts">
        /// Accumulates, per item id, how many units the row adds on top of the returned list.
        /// Left untouched when nothing is added, so one dictionary can collect several clears.
        /// </param>
        /// <returns>
        /// <paramref name="rewards"/> itself when <paramref name="itemRewardBoost"/> is
        /// <c>null</c>; otherwise the drawn items that remain, ordered by item id. Together with
        /// <paramref name="extraCounts"/> they add up to exactly what
        /// <see cref="ApplyItemRewardBoost"/> grants for the clear.
        /// </returns>
        public static List<ItemBase> SplitItemRewardBoost(
            List<ItemBase> rewards,
            BoostScheduleSheet.Row itemRewardBoost,
            IDictionary<int, int> extraCounts)
        {
            if (itemRewardBoost is null)
            {
                return rewards;
            }

            var kept = new List<ItemBase>(rewards.Count);
            foreach (var group in rewards.GroupBy(item => item.Id).OrderBy(group => group.Key))
            {
                var drawn = group.ToList();
                var count = drawn.Count;
                var boosted = (int)Math.Min(
                    itemRewardBoost.Apply(count),
                    (long)count * MaxItemRewardBoostFactor);
                if (boosted <= count)
                {
                    kept.AddRange(drawn.Take(boosted));
                    continue;
                }

                kept.AddRange(drawn);
                extraCounts.TryGetValue(group.Key, out var extra);
                extraCounts[group.Key] = checked(extra + boosted - count);
            }

            return kept;
        }

        /// <summary>
        /// Creates the items <see cref="SplitItemRewardBoost"/> counted, one object per unit,
        /// for a caller that needs objects (a battle's drop log, a result view).
        /// </summary>
        /// <param name="extraCounts">Units to create per item id.</param>
        /// <param name="materialItemSheet">The sheet the rewards were created from.</param>
        /// <returns>The items, ordered by item id.</returns>
        public static List<ItemBase> CreateItemRewardBoostExtras(
            IReadOnlyDictionary<int, int> extraCounts,
            MaterialItemSheet materialItemSheet)
        {
            var result = new List<ItemBase>();
            foreach (var (itemId, count) in extraCounts.OrderBy(pair => pair.Key))
            {
                // Unreachable: every counted id came from an item created from this sheet.
                if (!materialItemSheet.TryGetValue(itemId, out var materialRow))
                {
                    continue;
                }

                for (var i = 0; i < count; i++)
                {
                    result.Add(CreateItemRewardBoostItem(materialRow));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates one unit of a boosted item reward the way <see cref="Simulator.SetRewardV2"/>
        /// creates a drawn one, so a circle is tradable as usual. A fungible item, so one object
        /// may stand for any number of units when added with a count.
        /// </summary>
        /// <param name="materialRow">The item's row.</param>
        /// <returns>The item.</returns>
        public static ItemBase CreateItemRewardBoostItem(MaterialItemSheet.Row materialRow) =>
            materialRow.ItemSubType is ItemSubType.Circle
                ? ItemFactory.CreateTradableMaterial(materialRow)
                : ItemFactory.CreateMaterial(materialRow);

        private static List<(string ticker, int amount)> GetFavWaveRewards(
            IRandom random,
            List<StageSheet.FavRewardData> favRewards,
            int favDropMin,
            int favDropMax,
            BoostScheduleSheet.Row favRewardBoost)
        {
            if (favRewards.Count == 0)
                return new List<(string, int)>();

            var dropCount = random.Next(favDropMin, favDropMax + 1);
            if (dropCount <= 0)
                return new List<(string, int)>();

            var selector = new WeightedSelector<StageSheet.FavRewardData>(random);
            foreach (var fav in favRewards)
                selector.Add(fav, fav.Ratio);

            var result = new Dictionary<string, int>();
            for (var i = 0; i < dropCount; i++)
            {
                var selected = selector.Select(1).First();
                var amount = random.Next(selected.Min, selected.Max + 1);
                if (result.ContainsKey(selected.Ticker))
                    result[selected.Ticker] += amount;
                else
                    result[selected.Ticker] = amount;
            }

            return ApplyFavRewardBoost(
                result.Select(kv => (kv.Key, kv.Value)).ToList(),
                favRewardBoost);
        }

        /// <summary>
        /// Draws the item rewards of <paramref name="playCount"/> clears of
        /// <paramref name="stageRow"/>.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="stageRow">The stage being cleared.</param>
        /// <param name="materialItemSheet">The sheet the rewards are created from.</param>
        /// <param name="playCount">The number of clears.</param>
        /// <param name="itemRewardBoost">
        /// The <see cref="BoostScheduleSheet"/> row adjusting each clear's items, or <c>null</c>
        /// for none. See <see cref="ApplyItemRewardBoost"/>. It never changes the random draws.
        /// A caller replaying a battle must pass what the action resolved.
        /// </param>
        /// <returns>The items, one object per unit, each clear's ordered by item id.</returns>
        public static List<ItemBase> GetWaveRewards(
            IRandom random,
            StageSheet.Row stageRow,
            MaterialItemSheet materialItemSheet,
            int playCount = 1,
            BoostScheduleSheet.Row itemRewardBoost = null)
        {
            var maxCountForItemDrop = random.Next(
                stageRow.DropItemMin,
                stageRow.DropItemMax + 1);
            var waveRewards = new List<ItemBase>();
            for (var i = 0; i < playCount; i++)
            {
                var itemSelector = StageSimulatorV1.SetItemSelector(stageRow, random);
                var rewards = SetRewardV2(
                    itemSelector,
                    maxCountForItemDrop,
                    random,
                    materialItemSheet
                );

                waveRewards.AddRange(
                    ApplyItemRewardBoost(rewards, itemRewardBoost, materialItemSheet));
            }

            return waveRewards;
        }

        public Player Simulate()
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].Spawn(this);

                foreach (var skill in _skillsOnWaveStart)
                {
                    var buffs = BuffFactory.GetBuffs(
                        Player.Stats,
                        skill,
                        SkillBuffSheet,
                        StatBuffSheet,
                        SkillActionBuffSheet,
                        ActionBuffSheet
                    );

                    var usedSkill = skill.Use(Player, 0, buffs, LogEvent);
                    if (LogEvent)
                    {
                        Log.Add(usedSkill);
                    }
                }

                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (Exp > 0)
                            {
                                Player.GetExp((int)(Exp * 0.3m), LogEvent);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }

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
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (Exp > 0)
                            {
                                Player.GetExp((int)(Exp * 0.3m), LogEvent);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }

                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                            {
                                if (Exp > 0)
                                {
                                    Player.GetExp(Exp, LogEvent);
                                }

                                break;
                            }
                            case 2:
                            {
                                ItemMap = Player.GetRewards(_waveRewards);
                                foreach (var (ticker, amount) in GetFavWaveRewards(Random, _favRewards, _favDropMin, _favDropMax, _favRewardBoost))
                                {
                                    FungibleAssetRewards[ticker] = amount;
                                }

                                if (LogEvent)
                                {
                                    var dropBox = new DropBox(null, _waveRewards);
                                    Log.Add(dropBox);
                                    var getReward = new GetReward(null, _waveRewards, FungibleAssetRewards);
                                    Log.Add(getReward);
                                }

                                break;
                            }
                            default:
                            {
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                            }
                        }

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

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        private void SetWave(StageSheet.Row stageRow, StageWaveSheet.Row stageWaveRow)
        {
            var enemyStatModifiers = stageRow.EnemyInitialStatModifiers;
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves
                         .Select(e => SpawnWave(e, enemyStatModifiers)))
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(
            StageWaveSheet.WaveData waveData,
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
    }
}
