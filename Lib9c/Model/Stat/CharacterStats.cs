using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// Stat is built with _baseStats based on level,
    /// _equipmentStats based on equipments,
    /// _consumableStats based on consumables,
    /// _buffStats based on buffs
    /// and _optionalStats for runes, etc...
    /// Stat of character is built with total of these stats.
    /// </summary>
    [Serializable]
    public class CharacterStats : Stats, IBaseAndAdditionalStats, ICloneable
    {
        private readonly CharacterSheet.Row _row;

        private readonly Stats _baseStats = new Stats();
        private readonly Stats _equipmentStats = new Stats();
        private readonly Stats _consumableStats = new Stats();
        private readonly Stats _runeStats = new Stats();
        private readonly Stats _costumeStats = new Stats();
        private readonly Stats _collectionStats = new Stats();
        private readonly Stats _buffStats = new Stats();

        private readonly List<StatModifier> _initialStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _equipmentStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _consumableStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _runeStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _costumeStatModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _collectionStatModifiers = new List<StatModifier>();
        private readonly Dictionary<int, StatModifier> _buffStatModifiers = new Dictionary<int, StatModifier>();

        public readonly StatMap StatWithoutBuffs = new StatMap();

        public int Level { get; private set; }

        public IStats BaseStats => _baseStats;
        public IStats EquipmentStats => _equipmentStats;
        public IStats ConsumableStats => _consumableStats;
        public IStats RuneStats => _runeStats;
        public IStats BuffStats => _buffStats;
        public IStats CostumeStats => _costumeStats;
        public IStats CollectionStats => _collectionStats;

        public long BaseHP => BaseStats.HP;
        public long BaseATK => BaseStats.ATK;
        public long BaseDEF => BaseStats.DEF;
        public long BaseCRI => BaseStats.CRI;
        public long BaseHIT => BaseStats.HIT;
        public long BaseSPD => BaseStats.SPD;
        public long BaseDRV => BaseStats.DRV;
        public long BaseDRR => BaseStats.DRR;
        public long BaseCDMG => BaseStats.CDMG;
        public long BaseArmorPenetration => BaseStats.ArmorPenetration;
        public long BaseThorn => BaseStats.Thorn;

        public long AdditionalHP => HP - _baseStats.HP;
        public long AdditionalATK => ATK - _baseStats.ATK;
        public long AdditionalDEF => DEF - _baseStats.DEF;
        public long AdditionalCRI => CRI - _baseStats.CRI;
        public long AdditionalHIT => HIT - _baseStats.HIT;
        public long AdditionalSPD => SPD - _baseStats.SPD;
        public long AdditionalDRV => DRV - _baseStats.DRV;
        public long AdditionalDRR => DRR - _baseStats.DRR;
        public long AdditionalCDMG => CDMG - _baseStats.CDMG;
        public long AdditionalArmorPenetration => ArmorPenetration - _baseStats.ArmorPenetration;
        public long AdditionalThorn => Thorn - _baseStats.Thorn;

        public bool IsArenaCharacter { private get; set; } = false;
        public long HpIncreasingModifier { private get; set; } = 2;

        private readonly Dictionary<StatType, decimal> MinimumStatValues =
            new Dictionary<StatType, decimal>()
            {
                { StatType.HP, 0m },
                { StatType.ATK, 0m },
                { StatType.DEF, 0m },
                { StatType.CRI, 0m },
                { StatType.HIT, 1m },
                { StatType.SPD, 1m },
                { StatType.DRV, 0m },
                { StatType.DRR, 0m },
                { StatType.CDMG, 0m },
                { StatType.ArmorPenetration, 0m },
                { StatType.Thorn, 0m },
            };

        public CharacterStats(
            CharacterSheet.Row row,
            int level,
            IReadOnlyList<StatModifier> initialStatModifiers = null
        )
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            if (initialStatModifiers != null)
            {
                _initialStatModifiers.AddRange(initialStatModifiers);
            }

            SetStats(level);
        }

        public CharacterStats(WorldBossCharacterSheet.WaveStatData stat)
        {
            var stats = stat.ToStats();
            _baseStats.Set(stats);
            SetStats(stat.Level);
        }

        public CharacterStats(CharacterStats value) : base(value)
        {
            _row = value._row;

            _baseStats = new Stats(value._baseStats);
            _equipmentStats = new Stats(value._equipmentStats);
            _consumableStats = new Stats(value._consumableStats);
            _runeStats = new Stats(value._runeStats);
            _buffStats = new Stats(value._buffStats);
            _costumeStats = new Stats(value._costumeStats);

            _equipmentStatModifiers = value._equipmentStatModifiers;
            _consumableStatModifiers = value._consumableStatModifiers;
            _runeStatModifiers = value._runeStatModifiers;
            _buffStatModifiers = value._buffStatModifiers;
            _costumeStatModifiers = value._costumeStatModifiers;
            IsArenaCharacter = value.IsArenaCharacter;

            Level = value.Level;
        }

        /// <summary>
        /// Set base stats based on character level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetStats(int level, bool updateImmediate = true)
        {
            if (level == Level)
                return this;

            Level = level;

            if (updateImmediate)
            {
                UpdateBaseStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on equipments. Also recalculates stats from consumables and buffs.
        /// </summary>
        /// <param name="equipments"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetEquipments(
            IEnumerable<Equipment> equipments,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate = true
        )
        {
            _equipmentStatModifiers.Clear();
            if (!(equipments is null))
            {
                foreach (var equipment in equipments)
                {
                    var statMap = equipment.StatsMap;
                    foreach (var (statType, value) in statMap.GetStats(true))
                    {
                        var statModifier = new StatModifier(
                            statType,
                            StatModifier.OperationType.Add,
                            value);
                        _equipmentStatModifiers.Add(statModifier);
                    }
                }

                // set effects.
                var setEffectRows = sheet.GetSetEffectRows(equipments);
                foreach (var statModifier in setEffectRows.SelectMany(row =>
                             row.StatModifiers.Values))
                {
                    _equipmentStatModifiers.Add(statModifier);
                }
            }

            if (updateImmediate)
            {
                UpdateEquipmentStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on consumables. Also recalculates stats from buffs.
        /// </summary>
        /// <param name="consumables"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetConsumables(
            IEnumerable<Consumable> consumables,
            bool updateImmediate = true)
        {
            _consumableStatModifiers.Clear();
            if (!(consumables is null))
            {
                foreach (var consumable in consumables)
                {
                    var statMap = consumable.StatsMap;
                    foreach (var (statType, value) in statMap.GetStats(true))
                    {
                        var statModifier = new StatModifier(
                            statType,
                            StatModifier.OperationType.Add,
                            value);
                        _consumableStatModifiers.Add(statModifier);
                    }
                }
            }

            if (updateImmediate)
            {
                UpdateConsumableStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on runes.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetRunes(IEnumerable<StatModifier> value, bool updateImmediate = true)
        {
            _runeStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var modifier in value)
                {
                    _runeStatModifiers.Add(modifier);
                }
            }

            if (updateImmediate)
            {
                UpdateRuneStats();
            }

            return this;
        }

        /// <summary>
        /// Set stats based on buffs.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="buffLimitSheet"></param>
        /// <param name="updateImmediate"></param>
        /// <returns></returns>
        public CharacterStats SetBuffs(IEnumerable<Buff.StatBuff> value,
            BuffLimitSheet buffLimitSheet, bool updateImmediate = true)
        {
            _buffStatModifiers.Clear();
            if (!(value is null))
            {
                foreach (var buff in value)
                {
                    AddBuff(buff,  buffLimitSheet, false);
                }
            }

            if (updateImmediate)
            {
                UpdateBuffStats();
            }

            return this;
        }

        public CharacterStats SetCollections(IEnumerable<StatModifier> statModifiers,
            bool updateImmediate = true)
        {
            _collectionStatModifiers.Clear();
            var perModifiers = new List<StatModifier>();
            foreach (var modifier in statModifiers)
            {
                switch (modifier.Operation)
                {
                    case StatModifier.OperationType.Add:
                        _collectionStatModifiers.Add(modifier);
                        break;
                    case StatModifier.OperationType.Percentage:
                        perModifiers.Add(modifier);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var groupBy = perModifiers.GroupBy(m => m.StatType).ToList();
            foreach (var group in groupBy)
            {
                var statType = group.Key;
                var sum = group.Sum(g => g.Value);
                _collectionStatModifiers.Add(new StatModifier(statType,
                    StatModifier.OperationType.Percentage, sum));
            }

            if (updateImmediate)
            {
                UpdateCollectionStats();
            }

            return this;
        }

        public void AddBuff(Buff.StatBuff buff, BuffLimitSheet buffLimitSheet, bool updateImmediate = true)
        {
            var modifier = GetBuffModifier(buff, buffLimitSheet);
            _buffStatModifiers[buff.RowData.GroupId] = modifier;

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void RemoveBuff(Buff.StatBuff buff, bool updateImmediate = true)
        {
            if (!_buffStatModifiers.ContainsKey(buff.RowData.GroupId))
                return;

            _buffStatModifiers.Remove(buff.RowData.GroupId);

            if (updateImmediate)
            {
                UpdateBuffStats();
            }
        }

        public void AddRune(IEnumerable<StatModifier> statModifiers)
        {
            _runeStatModifiers.AddRange(statModifiers);
            UpdateRuneStats();
        }

        public void AddCostume(IEnumerable<StatModifier> statModifiers)
        {
            _costumeStatModifiers.AddRange(statModifiers);
            UpdateCostumeStats();
        }

        private void SetCostumes(IEnumerable<Costume> costumes, CostumeStatSheet costumeStatSheet)
        {
            var statModifiers = new List<StatModifier>();
            foreach (var costume in costumes)
            {
                var stat = costumeStatSheet.OrderedList
                    .Where(r => r.CostumeId == costume.Id)
                    .Select(row => new StatModifier(row.StatType, StatModifier.OperationType.Add,
                        (int) row.Stat));
                statModifiers.AddRange(stat);
            }

            SetCostume(statModifiers);
        }

        public void SetCostume(IEnumerable<StatModifier> statModifiers)
        {
            _costumeStatModifiers.Clear();
            AddCostume(statModifiers);
        }

        /// <summary>
        /// Increases the HP of the character for the arena.
        /// </summary>
        public void IncreaseHpForArena()
        {
            var originalHp = _statMap[StatType.HP];
            var modifiedHp = CalculateModifiedBaseHp(originalHp);

            _statMap[StatType.HP].SetBaseValue(modifiedHp);
            var modifiedStatHp = CalculateModifiedHpWithoutBuffs(modifiedHp);

            StatWithoutBuffs[StatType.HP].SetBaseValue(modifiedStatHp);
        }

        private long CalculateModifiedBaseHp(DecimalStat originalHp)
        {
            return Math.Max(0, originalHp.TotalValueAsLong * HpIncreasingModifier);
        }

        private long CalculateModifiedHpWithoutBuffs(long modifiedHp)
        {
            return Math.Max(0, modifiedHp - _buffStats.HP * HpIncreasingModifier);
        }

        private void UpdateBaseStats()
        {
            if (_row != null)
            {
                var statsData = _row.ToStats(Level);
                _baseStats.Set(statsData);
            }

            if (_initialStatModifiers != null)
            {
                _baseStats.Modify(_initialStatModifiers);
            }

            UpdateEquipmentStats();
        }

        private void UpdateEquipmentStats()
        {
            _equipmentStats.Set(_equipmentStatModifiers, _baseStats);
            UpdateConsumableStats();
        }

        private void UpdateConsumableStats()
        {
            _consumableStats.Set(_consumableStatModifiers, _baseStats, _equipmentStats);
            UpdateRuneStats();
        }

        private void UpdateRuneStats()
        {
            _runeStats.Set(_runeStatModifiers, _baseStats, _equipmentStats, _consumableStats);
            UpdateCostumeStats();
        }

        private void UpdateBuffStats()
        {
            var buffModifiers = new List<StatModifier>();
            var perModifiers = new List<StatModifier>();
            foreach (var modifier in _buffStatModifiers.Values)
            {
                switch (modifier.Operation)
                {
                    case StatModifier.OperationType.Add:
                        buffModifiers.Add(modifier);
                        break;
                    case StatModifier.OperationType.Percentage:
                        perModifiers.Add(modifier);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var groupBy = perModifiers.GroupBy(m => m.StatType).ToList();
            foreach (var group in groupBy)
            {
                var statType = group.Key;
                var sum = group.Sum(g => g.Value);
                buffModifiers.Add(new StatModifier(statType, StatModifier.OperationType.Percentage,
                    sum));
            }

            _buffStats.Set(buffModifiers, _baseStats, _equipmentStats, _consumableStats, _runeStats,
                _costumeStats, _collectionStats);
            UpdateTotalStats();
        }

        private void UpdateCostumeStats()
        {
            _costumeStats.Set(_costumeStatModifiers, _baseStats, _equipmentStats, _consumableStats,
                _runeStats);
            UpdateCollectionStats();
        }

        private void UpdateCollectionStats()
        {
            _collectionStats.Set(_collectionStatModifiers, _baseStats, _equipmentStats,
                _consumableStats, _runeStats, _costumeStats);
            Set(StatWithoutBuffs, _baseStats, _equipmentStats, _consumableStats, _runeStats,
                _costumeStats, _collectionStats);
            foreach (var stat in StatWithoutBuffs.GetDecimalStats(false))
            {
                if (!LegacyDecimalStatTypes.Contains(stat.StatType))
                {
                    var value = Math.Max(0m, stat.BaseValueAsLong);
                    stat.SetBaseValue(value);
                }
                else
                {
                    var value = Math.Max(0m, stat.BaseValue);
                    stat.SetBaseValue(value);
                }
            }

            UpdateBuffStats();
        }

        private void UpdateTotalStats()
        {
            Set(_statMap, _baseStats, _equipmentStats, _consumableStats, _runeStats, _costumeStats,
                _collectionStats, _buffStats);

            foreach (var stat in _statMap.GetDecimalStats(false))
            {
                var minimumValue = MinimumStatValues[stat.StatType];
                if (!LegacyDecimalStatTypes.Contains(stat.StatType))
                {
                    var value = Math.Max(minimumValue, stat.BaseValueAsLong);
                    stat.SetBaseValue(value);
                }
                else
                {
                    var value = Math.Max(minimumValue, stat.BaseValue);
                    stat.SetBaseValue(value);
                }
            }

            if (IsArenaCharacter)
            {
                IncreaseHpForArena();
            }
        }

        public override object Clone()
        {
            return new CharacterStats(this);
        }

        public IEnumerable<(StatType statType, long baseValue)> GetBaseStats(
            bool ignoreZero = false)
        {
            return _baseStats.GetStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, long additionalValue)> GetAdditionalStats(
            bool ignoreZero = false)
        {
            var baseStats = _baseStats.GetStats();
            foreach (var (statType, stat) in baseStats)
            {
                var value = _statMap[statType].BaseValueAsLong - stat;
                if (!ignoreZero || value != default)
                {
                    yield return (statType, value);
                }
            }
        }

        public IEnumerable<(StatType statType, long baseValue, long additionalValue)>
            GetBaseAndAdditionalStats(
                bool ignoreZero = false)
        {
            var additionalStats = GetAdditionalStats();
            foreach (var (statType, additionalStat) in additionalStats)
            {
                var baseStat = _baseStats.GetStatAsLong(statType);
                if (!ignoreZero ||
                    (baseStat != default) || (additionalStat != default))
                {
                    yield return (statType, baseStat, additionalStat);
                }
            }
        }

        public void SetCostumeStat(IReadOnlyCollection<Costume> costumes,
            CostumeStatSheet costumeStatSheet)
        {
            var statModifiers = new List<StatModifier>();
            foreach (var itemId in costumes.Select(costume => costume.Id))
            {
                statModifiers.AddRange(
                    costumeStatSheet.OrderedList
                        .Where(r => r.CostumeId == itemId)
                        .Select(row => new StatModifier(row.StatType,
                            StatModifier.OperationType.Add, (int) row.Stat))
                );
            }

            SetCostume(statModifiers);
        }

        public void AddRuneStat(RuneOptionSheet.Row.RuneOptionInfo optionInfo, int runeLevelBonus)
        {
            var statModifiers = new List<StatModifier>();
            statModifiers.AddRange(
                optionInfo.Stats.Select(x => new StatModifier(
                    x.stat.StatType,
                    x.operationType,
                    (long)(x.stat.BaseValue * (100000 + runeLevelBonus) / 100000m)
                ))
            );
            AddRune(statModifiers);
        }

        public void ConfigureStats(
            IReadOnlyCollection<Equipment> equipments,
            IReadOnlyCollection<Costume> costumes,
            IReadOnlyCollection<RuneOptionSheet.Row.RuneOptionInfo> runeOptions,
            CostumeStatSheet costumeStatSheet,
            List<StatModifier> collectionStatModifiers,
            int runeLevelBonus
        )
        {
            SetEquipments(equipments, new EquipmentItemSetEffectSheet());
            SetCostumeStat(costumes, costumeStatSheet);
            foreach (var runeOption in runeOptions)
            {
                AddRuneStat(runeOption, runeLevelBonus);
            }

            SetCollections(collectionStatModifiers);
        }

        /// <summary>
        /// Returns a <see cref="StatModifier"/> based on the upper limit from <see cref="BuffLimitSheet"/>.
        /// </summary>
        /// <param name="buff"><see cref="Buff.StatBuff"/> for modify stats.</param>
        /// <param name="buffLimitSheet">Upper limit sheet data.</param>
        /// <returns>if buff modify stats 100% but limit 50% set in <see cref="BuffLimitSheet"/>,
        /// it will return 50%, else return 100% <see cref="StatModifier"/>,
        /// if de-buff modify stats -100% but limit -50% set in <see cref="BuffLimitSheet"/>,
        /// it will return -50%, else return -100% <see cref="StatModifier"/>
        /// </returns>
        private StatModifier GetBuffModifier(Buff.StatBuff buff, BuffLimitSheet buffLimitSheet)
        {
            var modifier = buff.GetModifier();
            try
            {
                var statType = modifier.StatType;
                var limitModifier = buffLimitSheet[buff.RowData.GroupId].GetModifier(statType);
                var stat = _statMap.GetStatAsLong(statType);
                var buffModified = modifier.GetModifiedValue(stat);
                var maxModified = (long)limitModifier.GetModifiedValue(stat);
                if (buff.IsDebuff() && maxModified > buffModified || buff.IsBuff() && maxModified < buffModified)
                {
                    return limitModifier;
                }
            }
            catch (KeyNotFoundException)
            {
                // pass
            }
            catch (NullReferenceException)
            {
                // pass
            }

            return modifier;
        }
    }
}
