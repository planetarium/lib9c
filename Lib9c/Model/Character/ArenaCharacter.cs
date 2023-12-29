using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTAI;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Skill.Arena;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using ArenaSkill = Nekoyume.Model.BattleStatus.Arena.ArenaSkill;

namespace Nekoyume.Model
{
    public class ArenaCharacter : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        private readonly SkillSheet _skillSheet;
        private readonly SkillBuffSheet _skillBuffSheet;
        private readonly StatBuffSheet _statBuffSheet;
        private readonly SkillActionBuffSheet _skillActionBuffSheet;
        private readonly ActionBuffSheet _actionBuffSheet;
        private readonly IArenaSimulator _simulator;
        private readonly ArenaSkills _skills;

        public readonly ArenaSkills _runeSkills = new ArenaSkills();
        public readonly Dictionary<int, int> RuneSkillCooldownMap = new Dictionary<int, int>();

        private readonly int _attackCountMax;

        private ArenaCharacter _target;
        private int _attackCount;

        public Guid Id { get; } = Guid.NewGuid();
        public BattleStatus.Arena.ArenaSkill SkillLog { get; private set; }
        public CharacterStats Stats { get; }
        public ElementalType OffensiveElementalType { get; }
        public ElementalType DefenseElementalType { get; }
        public SizeType SizeType { get; }
        public float RunSpeed { get; }
        public float AttackRange { get; }
        public int CharacterId { get; }
        public bool IsEnemy { get; }

        private bool _setExtraValueBuffBeforeGetBuffs = false;

        private int _currentHP;

        public int CurrentHP
        {
            get => _currentHP;
            set => _currentHP = Math.Min(Math.Max(0, value), HP);
        }

        public int Level
        {
            get => Stats.Level;
            set => Stats.SetStats(value);
        }

        public int HP => Stats.HP;
        public int AdditionalHP => Stats.BuffStats.HP;
        public int ATK => Stats.ATK;
        public int DEF => Stats.DEF;
        public int CRI => Stats.CRI;
        public int HIT => Stats.HIT;
        public int SPD => Stats.SPD;
        public int DRV => Stats.DRV;
        public int DRR => Stats.DRR;
        public int CDMG => Stats.CDMG;
        public int ArmorPenetration => Stats.ArmorPenetration;
        public int Thorn => Stats.Thorn;

        public bool IsDead => CurrentHP <= 0;
        public Dictionary<int, Buff.Buff> Buffs { get; } = new Dictionary<int, Buff.Buff>();
        public IEnumerable<StatBuff> StatBuffs => Buffs.Values.OfType<StatBuff>();
        public IEnumerable<ActionBuff> ActionBuffs => Buffs.Values.OfType<ActionBuff>();

        public object Clone() => new ArenaCharacter(this);

        [Obsolete("It using at ArenaSimulatorV1.")]
        public ArenaCharacter(
            ArenaSimulatorV1 simulator,
            ArenaPlayerDigest digest,
            ArenaSimulatorSheetsV1 sheets,
            bool isEnemy = false)
        {
            OffensiveElementalType = GetElementalType(digest.Equipments, ItemSubType.Weapon);
            DefenseElementalType = GetElementalType(digest.Equipments, ItemSubType.Armor);
            var row = CharacterRow(digest.CharacterId, sheets);
            SizeType = row?.SizeType ?? SizeType.S;
            RunSpeed = row?.RunSpeed ?? 1f;
            AttackRange = row?.AttackRange ?? 1f;
            CharacterId = digest.CharacterId;
            IsEnemy = isEnemy;

            _skillSheet = sheets.SkillSheet;
            _skillBuffSheet = sheets.SkillBuffSheet;
            _statBuffSheet = sheets.StatBuffSheet;
            _skillActionBuffSheet = sheets.SkillActionBuffSheet;
            _actionBuffSheet = sheets.ActionBuffSheet;

            _simulator = simulator;
            Stats = GetStatV1(
                digest,
                row,
                sheets.EquipmentItemSetEffectSheet,
                sheets.CostumeStatSheet);
            _skills = GetSkills(digest.Equipments, sheets.SkillSheet);
            _attackCountMax = AttackCountHelper.GetCountMax(digest.Level);
            ResetCurrentHP();
        }

        public ArenaCharacter(
            IArenaSimulator simulator,
            ArenaPlayerDigest digest,
            ArenaSimulatorSheets sheets,
            bool isEnemy = false)
        {
            OffensiveElementalType = GetElementalType(digest.Equipments, ItemSubType.Weapon);
            DefenseElementalType = GetElementalType(digest.Equipments, ItemSubType.Armor);
            var row = CharacterRow(digest.CharacterId, sheets);
            SizeType = row?.SizeType ?? SizeType.S;
            RunSpeed = row?.RunSpeed ?? 1f;
            AttackRange = row?.AttackRange ?? 1f;
            CharacterId = digest.CharacterId;
            IsEnemy = isEnemy;

            _skillSheet = sheets.SkillSheet;
            _skillBuffSheet = sheets.SkillBuffSheet;
            _statBuffSheet = sheets.StatBuffSheet;
            _skillActionBuffSheet = sheets.SkillActionBuffSheet;
            _actionBuffSheet = sheets.ActionBuffSheet;

            _simulator = simulator;
            Stats = GetStatV1(
                digest,
                row,
                sheets.EquipmentItemSetEffectSheet,
                sheets.CostumeStatSheet);
            _skills = GetSkills(digest.Equipments, sheets.SkillSheet);
            _attackCountMax = AttackCountHelper.GetCountMax(digest.Level);
            ResetCurrentHP();
        }

        public ArenaCharacter(
            IArenaSimulator simulator,
            ArenaPlayerDigest digest,
            ArenaSimulatorSheets sheets,
            int hpModifier,
            bool isEnemy = false,
            bool setExtraValueBuffBeforeGetBuffs = false)
        {
            OffensiveElementalType = GetElementalType(digest.Equipments, ItemSubType.Weapon);
            DefenseElementalType = GetElementalType(digest.Equipments, ItemSubType.Armor);
            var row = CharacterRow(digest.CharacterId, sheets);
            SizeType = row?.SizeType ?? SizeType.S;
            RunSpeed = row?.RunSpeed ?? 1f;
            AttackRange = row?.AttackRange ?? 1f;
            CharacterId = digest.CharacterId;
            IsEnemy = isEnemy;
            _setExtraValueBuffBeforeGetBuffs = setExtraValueBuffBeforeGetBuffs;

            _skillSheet = sheets.SkillSheet;
            _skillBuffSheet = sheets.SkillBuffSheet;
            _statBuffSheet = sheets.StatBuffSheet;
            _skillActionBuffSheet = sheets.SkillActionBuffSheet;
            _actionBuffSheet = sheets.ActionBuffSheet;

            _simulator = simulator;
            Stats = GetStat(
                digest,
                row,
                sheets.EquipmentItemSetEffectSheet,
                sheets.CostumeStatSheet,
                hpModifier);
            _skills = GetSkills(digest.Equipments, sheets.SkillSheet);
            _attackCountMax = AttackCountHelper.GetCountMax(digest.Level);
            ResetCurrentHP();
        }

        private ArenaCharacter(ArenaCharacter value)
        {
            Id = value.Id;
            SkillLog = value.SkillLog;
            OffensiveElementalType = value.OffensiveElementalType;
            DefenseElementalType = value.OffensiveElementalType;
            SizeType = value.SizeType;
            RunSpeed = value.RunSpeed;
            AttackRange = value.AttackRange;
            CharacterId = value.CharacterId;
            IsEnemy = value.IsEnemy;

            _skillSheet = value._skillSheet;
            _skillBuffSheet = value._skillBuffSheet;
            _statBuffSheet = value._statBuffSheet;
            _skillActionBuffSheet = value._skillActionBuffSheet;
            _actionBuffSheet = value._actionBuffSheet;

            _simulator = value._simulator;
            Stats = new CharacterStats(value.Stats);
            _skills = value._skills;
            Buffs = new Dictionary<int, Buff.Buff>();
#pragma warning disable LAA1002
            foreach (var pair in value.Buffs)
#pragma warning restore LAA1002
            {
                Buffs.Add(pair.Key, (Buff.Buff) pair.Value.Clone());
            }

            _attackCountMax = value._attackCount;
            _attackCount = value._attackCount;
            _target = value._target;
            CurrentHP = value.CurrentHP;
        }

        private ElementalType GetElementalType(IEnumerable<Equipment> equipments, ItemSubType itemSubType)
        {
            var equipment = equipments.FirstOrDefault(x => x.ItemSubType.Equals(itemSubType));
            return equipment?.ElementalType ?? ElementalType.Normal;
        }

        private static CharacterSheet.Row CharacterRow(int characterId, ArenaSimulatorSheetsV1 sheets)
        {
            if (!sheets.CharacterSheet.TryGetValue(characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", characterId);
            }

            return row;
        }

        private static CharacterSheet.Row CharacterRow(int characterId, ArenaSimulatorSheets sheets)
        {
            if (!sheets.CharacterSheet.TryGetValue(characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", characterId);
            }

            return row;
        }

        protected void ResetCurrentHP()
        {
            CurrentHP = Math.Max(0, Stats.HP);
        }

        private static CharacterStats GetStat(
            ArenaPlayerDigest digest,
            CharacterSheet.Row characterRow,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet,
            int hpModifier)
        {
            var stats = new CharacterStats(characterRow, digest.Level)
            {
                IsArenaCharacter = true,
                HpIncreasingModifier = hpModifier
            };
            stats.SetEquipments(digest.Equipments, equipmentItemSetEffectSheet);

            var options = new List<StatModifier>();
            foreach (var itemId in digest.Costumes.Select(costume => costume.Id))
            {
                if (TryGetStats(costumeStatSheet, itemId, out var option))
                {
                    options.AddRange(option);
                }
            }

            stats.SetOption(options);
            return stats;
        }

        private static CharacterStats GetStatV1(
            ArenaPlayerDigest digest,
            CharacterSheet.Row characterRow,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet,
            CostumeStatSheet costumeStatSheet)
        {
            var stats = new CharacterStats(characterRow, digest.Level)
            {
                IsArenaCharacter = true
            };
            stats.SetEquipments(digest.Equipments, equipmentItemSetEffectSheet);

            var options = new List<StatModifier>();
            foreach (var itemId in digest.Costumes.Select(costume => costume.Id))
            {
                if (TryGetStats(costumeStatSheet, itemId, out var option))
                {
                    options.AddRange(option);
                }
            }

            stats.SetOption(options);
            return stats;
        }

        private static bool TryGetStats(
            CostumeStatSheet statSheet,
            int itemId,
            out IEnumerable<StatModifier> statModifiers)
        {
            statModifiers = statSheet.OrderedList
                .Where(r => r.CostumeId == itemId)
                .Select(row =>
                    new StatModifier(row.StatType, StatModifier.OperationType.Add, (int)row.Stat));

            return statModifiers.Any();
        }

        [Obsolete("Use SetRune instead.")]
        public void SetRuneV1(
            List<RuneState> runes,
            RuneOptionSheet runeOptionSheet,
            SkillSheet skillSheet)
        {
            foreach (var rune in runes)
            {
                if (!runeOptionSheet.TryGetValue(rune.RuneId, out var optionRow) ||
                    !optionRow.LevelOptionMap.TryGetValue(rune.Level, out var optionInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    optionInfo.Stats.Select(x =>
                        new StatModifier(
                            x.stat.StatType,
                            x.operationType,
                            x.stat.TotalValueAsInt)));
                Stats.AddOptional(statModifiers);
                ResetCurrentHP();

                if (optionInfo.SkillId == default ||
                    !skillSheet.TryGetValue(optionInfo.SkillId, out var skillRow))
                {
                    continue;
                }

                var power = 0;
                if (optionInfo.StatReferenceType == EnumType.StatReferenceType.Caster)
                {
                    if (optionInfo.SkillValueType == StatModifier.OperationType.Add)
                    {
                        power = (int)optionInfo.SkillValue;
                    }
                    else
                    {
                        switch (optionInfo.SkillStatType)
                        {
                            case StatType.HP:
                                power = HP;
                                break;
                            case StatType.ATK:
                                power = ATK;
                                break;
                            case StatType.DEF:
                                power = DEF;
                                break;
                        }

                        power = (int)Math.Round(power * optionInfo.SkillValue);
                    }
                }
                var skill = SkillFactory.GetForArena(skillRow, power, optionInfo.SkillChance, default, StatType.NONE);
                _runeSkills.Add(skill);
                RuneSkillCooldownMap[optionInfo.SkillId] = optionInfo.SkillCooldown;
            }
        }

        [Obsolete("Use SetRune instead.")]
        public void SetRuneV2(
            List<RuneState> runes,
            RuneOptionSheet runeOptionSheet,
            SkillSheet skillSheet)
        {
            foreach (var rune in runes)
            {
                if (!runeOptionSheet.TryGetValue(rune.RuneId, out var optionRow) ||
                    !optionRow.LevelOptionMap.TryGetValue(rune.Level, out var optionInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    optionInfo.Stats.Select(x =>
                        new StatModifier(
                            x.stat.StatType,
                            x.operationType,
                            x.stat.TotalValueAsInt)));
                Stats.AddOptional(statModifiers);
                ResetCurrentHP();

                if (optionInfo.SkillId == default ||
                    !skillSheet.TryGetValue(optionInfo.SkillId, out var skillRow))
                {
                    continue;
                }

                var power = 0;

                if (optionInfo.SkillValueType == StatModifier.OperationType.Add)
                {
                    power = (int)optionInfo.SkillValue;
                }
                else if (optionInfo.StatReferenceType == EnumType.StatReferenceType.Caster)
                {
                    var value = Stats.GetStatAsInt(optionInfo.SkillStatType);
                    power = (int)Math.Round(value * optionInfo.SkillValue);
                }
                var skill = SkillFactory.GetForArena(skillRow, power, optionInfo.SkillChance, default, StatType.NONE);
                var customField = new SkillCustomField
                {
                    BuffDuration = optionInfo.BuffDuration,
                    BuffValue = power,
                };
                skill.CustomField = customField;
                _runeSkills.Add(skill);
                RuneSkillCooldownMap[optionInfo.SkillId] = optionInfo.SkillCooldown;
            }
        }

        public void SetRune(
            List<RuneState> runes,
            RuneOptionSheet runeOptionSheet,
            SkillSheet skillSheet)
        {
            foreach (var rune in runes)
            {
                if (!runeOptionSheet.TryGetValue(rune.RuneId, out var optionRow) ||
                    !optionRow.LevelOptionMap.TryGetValue(rune.Level, out var optionInfo))
                {
                    continue;
                }

                var statModifiers = new List<StatModifier>();
                statModifiers.AddRange(
                    optionInfo.Stats.Select(x =>
                        new StatModifier(
                            x.stat.StatType,
                            x.operationType,
                            x.stat.TotalValueAsInt)));
                Stats.AddRune(statModifiers);
                ResetCurrentHP();

                if (optionInfo.SkillId == default ||
                    !skillSheet.TryGetValue(optionInfo.SkillId, out var skillRow))
                {
                    continue;
                }

                var power = 0;

                if (optionInfo.SkillValueType == StatModifier.OperationType.Add)
                {
                    power = (int)optionInfo.SkillValue;
                }
                else if (optionInfo.StatReferenceType == EnumType.StatReferenceType.Caster)
                {
                    var value = Stats.GetStatAsInt(optionInfo.SkillStatType);
                    power = (int)Math.Round(value * optionInfo.SkillValue);
                }
                var skill = SkillFactory.GetForArena(skillRow, power, optionInfo.SkillChance, default, StatType.NONE);
                var customField = new SkillCustomField
                {
                    BuffDuration = optionInfo.BuffDuration,
                    BuffValue = power,
                };
                skill.CustomField = customField;

                _runeSkills.Add(skill);
                RuneSkillCooldownMap[optionInfo.SkillId] = optionInfo.SkillCooldown;
            }
        }

        private static ArenaSkills GetSkills(IEnumerable<Equipment> equipments, SkillSheet skillSheet)
        {
            var skills = new ArenaSkills();

            // normal attack
            if (!skillSheet.TryGetValue(GameConfig.DefaultAttackId, out var skillRow))
            {
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
            }

            var attack = SkillFactory.GetForArena(skillRow, 0, 100, default, StatType.NONE);
            skills.Add(attack);

            foreach (var skill in equipments.SelectMany(equipment => equipment.Skills))
            {
                var arenaSkill = SkillFactory.GetForArena(
                    skill.SkillRow,
                    skill.Power,
                    skill.Chance,
                    skill.StatPowerRatio,
                    skill.ReferencedStatType);
                skills.Add(arenaSkill);
            }

            foreach (var buff in equipments.SelectMany(equipment => equipment.BuffSkills))
            {
                var buffSkill = SkillFactory.GetForArena(
                    buff.SkillRow,
                    buff.Power,
                    buff.Chance,
                    buff.StatPowerRatio,
                    buff.ReferencedStatType);
                skills.Add(buffSkill);
            }

            return skills;
        }

        #region Behaviour Tree

        [NonSerialized]
        private Root _root;

        private void InitAI()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act)
            );
        }

        [Obsolete("Use InitAI")]
        private void InitAIV2()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV2)
            );
        }

        [Obsolete("Use InitAI")]
        private void InitAIV1()
        {
            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV1)
            );
        }

        private void Act()
        {
            if (IsDead)
            {
                return;
            }

            ReduceDurationOfBuffs();
            ReduceSkillCooldown();
            ArenaSkill usedSkill;
            if (OnPreSkill())
            {
                usedSkill = new ArenaTick((ArenaCharacter)Clone());
                _simulator.Log.Add(usedSkill);
            }
            else
            {
                usedSkill = UseSkill();
            }

            if (usedSkill != null)
            {
                OnPostSkill(usedSkill);
            }
            RemoveBuffs();
        }

        [Obsolete("Use Act")]
        private void ActV1()
        {
            if (IsDead)
            {
                return;
            }

            ReduceDurationOfBuffs();
            ReduceSkillCooldown();
            UseSkillV1();
            RemoveBuffsV1();
        }

        [Obsolete("Use Act")]
        private void ActV2()
        {
            if (IsDead)
            {
                return;
            }

            ReduceDurationOfBuffs();
            ReduceSkillCooldown();
            UseSkill();
            RemoveBuffs();
        }

        protected virtual bool OnPreSkill()
        {
            return Buffs.Values.Any(buff => buff is Stun);
        }

        protected virtual void OnPostSkill(BattleStatus.Arena.ArenaSkill usedSkill)
        {
            var attackSkills = usedSkill.SkillInfos
                .Where(skillInfo => skillInfo.SkillCategory
                    is SkillCategory.NormalAttack
                    or SkillCategory.BlowAttack
                    or SkillCategory.DoubleAttack
                    or SkillCategory.AreaAttack
                    or SkillCategory.BuffRemovalAttack)
                .ToList();
            if (Buffs.Values.OfType<Vampiric>().OrderBy(x => x.BuffInfo.Id) is
                { } vampirics)
            {
                foreach (var vampiric in vampirics)
                {
                    foreach (var effect in attackSkills
                                 .Select(skillInfo =>
                                     vampiric.GiveEffectForArena(this, skillInfo, _simulator.Turn)))
                    {
                        _simulator.Log.Add(effect);
                    }
                }
            }

            var bleeds = Buffs.Values.OfType<Bleed>().OrderBy(x => x.BuffInfo.Id);
            foreach (var bleed in bleeds)
            {
                var effect = bleed.GiveEffectForArena(this, _simulator.Turn);
                _simulator.Log.Add(effect);
            }

            // Apply thorn damage if target has thorn
            foreach (var skillInfo in attackSkills)
            {
                if (skillInfo.Target.Thorn > 0)
                {
                    var effect = GiveThornDamage(skillInfo.Target.Thorn);
                    _simulator.Log.Add(effect);
                }
            }
        }

        private ArenaSkill GiveThornDamage(int targetThorn)
        {
            var clone = (ArenaCharacter)Clone();
            // minimum 1 damage
            var thornDamage = Math.Max(1, targetThorn - DEF);
            CurrentHP -= thornDamage;
            var damageInfos = new List<ArenaSkill.ArenaSkillInfo>()
            {
                new ArenaSkill.ArenaSkillInfo(
                    (ArenaCharacter)Clone(),
                    thornDamage,
                    false,
                    SkillCategory.TickDamage,
                    _simulator.Turn,
                    ElementalType.Normal,
                    SkillTargetType.Enemy)
            };

            var tickDamage = new ArenaTickDamage(
                clone,
                damageInfos,
                null);

            return tickDamage;
        }

        private void ReduceDurationOfBuffs()
        {
#pragma warning disable LAA1002
            foreach (var pair in Buffs)
#pragma warning restore LAA1002
            {
                pair.Value.RemainedDuration--;
            }
        }

        private void ReduceSkillCooldown()
        {
            _skills.ReduceCooldown();
            _runeSkills.ReduceCooldown();
        }

        private BattleStatus.Arena.ArenaSkill UseSkill()
        {
            var selectedRuneSkill = _runeSkills.SelectWithoutDefaultAttack(_simulator.Random);
            var selectedSkill = selectedRuneSkill ??
                _skills.Select(_simulator.Random);
            var usedSkill = selectedSkill.Use(
                this,
                _target,
                _simulator.Turn,
                BuffFactory.GetBuffs(
                    Stats,
                    selectedSkill,
                    _skillBuffSheet,
                    _statBuffSheet,
                    _skillActionBuffSheet,
                    _actionBuffSheet,
                    _setExtraValueBuffBeforeGetBuffs &&
                    selectedSkill is ArenaBuffSkill &&
                    (selectedSkill.Power > 0 ||
                     selectedSkill.ReferencedStatType !=
                     StatType.NONE))
            );

            if (!_skillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var row))
            {
                throw new KeyNotFoundException(
                    selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            if (selectedRuneSkill == null)
            {
                _skills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
            }
            else
            {
                _runeSkills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
            }

            _simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        [Obsolete("Use UseSkill")]
        private void UseSkillV1()
        {
            var selectedSkill = _skills.Select(_simulator.Random);
            SkillLog = selectedSkill.UseV1(
                this,
                _target,
                _simulator.Turn,
                BuffFactory.GetBuffs(
                    Stats,
                    selectedSkill,
                    _skillBuffSheet,
                    _statBuffSheet,
                    _skillActionBuffSheet,
                    _actionBuffSheet)
            );

            if (!_skillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var row))
            {
                throw new KeyNotFoundException(
                    selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            _skills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
        }

        private void RemoveBuffs()
        {
            var isBuffRemoved = false;

            var keyList = Buffs.Keys.ToList();
            foreach (var key in keyList)
            {
                var buff = Buffs[key];
                if (buff.RemainedDuration > 0)
                    continue;

                Buffs.Remove(key);
                isBuffRemoved = true;
            }

            if (!isBuffRemoved)
                return;

            Stats.SetBuffs(StatBuffs);
        }

        [Obsolete("Use RemoveBuffs")]
        private void RemoveBuffsV1()
        {
            var isApply = false;

            foreach (var key in Buffs.Keys.ToList())
            {
                var buff = Buffs[key];
                if (buff.RemainedDuration > 0)
                {
                    continue;
                }

                Buffs.Remove(key);
                isApply = true;
            }

            if (isApply)
            {
                Stats.SetBuffs(StatBuffs);
            }
        }

        public void Tick()
        {
            _root.Tick();
        }
        #endregion

        public void Spawn(ArenaCharacter target)
        {
            _target = target;
            InitAI();
        }

        [Obsolete("Use Spawn")]
        public void SpawnV1(ArenaCharacter target)
        {
            _target = target;
            InitAIV1();
        }

        [Obsolete("Use Spawn")]
        public void SpawnV2(ArenaCharacter target)
        {
            _target = target;
            InitAIV2();
        }

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.BuffInfo.GroupId, out var outBuff) &&
                outBuff.BuffInfo.Id > buff.BuffInfo.Id)
                return;

            if (buff is StatBuff stat)
            {
                var clone = (StatBuff)stat.Clone();
                Buffs[stat.RowData.GroupId] = clone;
                Stats.AddBuff(clone, updateImmediate);
            }
            else if (buff is ActionBuff action)
            {
                var clone = (ActionBuff)action.Clone();
                Buffs[action.RowData.GroupId] = clone;
            }
        }

        [Obsolete("Use AddBuff")]
        public void AddBuffV1(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.BuffInfo.GroupId, out var outBuff) &&
                outBuff.BuffInfo.Id > buff.BuffInfo.Id)
                return;

            var clone = (Buff.StatBuff) buff.Clone();
            Buffs[buff.BuffInfo.GroupId] = clone;
            Stats.AddBuff(clone, updateImmediate);
        }

        public void RemoveRecentStatBuff()
        {
            StatBuff removedBuff = null;
            var minDuration = int.MaxValue;
            foreach (var buff in StatBuffs)
            {
                if (buff.RowData.Value < 0)
                {
                    continue;
                }

                var elapsedTurn = buff.OriginalDuration - buff.RemainedDuration;
                if (removedBuff is null)
                {
                    minDuration = elapsedTurn;
                    removedBuff = buff;
                }

                if (elapsedTurn > minDuration ||
                    buff.RowData.Id >= removedBuff.RowData.Id)
                {
                    continue;
                }

                minDuration = elapsedTurn;
                removedBuff = buff;
            }

            if (removedBuff != null)
            {
                Stats.RemoveBuff(removedBuff);
                Buffs.Remove(removedBuff.RowData.GroupId);
            }
        }

        public void Heal(int heal)
        {
            CurrentHP += heal;
        }

        public bool IsCritical(bool considerAttackCount = true)
        {
            var chance = _simulator.Random.Next(0, 100);
            if (!considerAttackCount)
                return CRI >= chance;

            var additionalCriticalChance =
                AttackCountHelper.GetAdditionalCriticalChance(_attackCount, _attackCountMax);
            return CRI + additionalCriticalChance >= chance;
        }

        public virtual bool IsHit(ArenaCharacter caster)
        {
            var isHit = HitHelper.IsHitWithoutLevelCorrection(
                caster.Level,
                caster.HIT,
                Level,
                HIT,
                _simulator.Random.Next(0, 100));
            if (!isHit)
            {
                caster._attackCount = 0;
            }

            return isHit;
        }

        public int GetDamage(int damage, bool considerAttackCount = true)
        {
            if (!considerAttackCount)
                return damage;

            _attackCount++;
            if (_attackCount > _attackCountMax)
            {
                _attackCount = 1;
            }

            var damageMultiplier = AttackCountHelper.GetDamageMultiplier(_attackCount, _attackCountMax);
            damage *= damageMultiplier;
            return damage;
        }
    }
}
