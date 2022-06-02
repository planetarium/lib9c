using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTAI;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Skill.Arena;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Character
{
    public class ArenaPlayer : ICharacter, ICloneable
    {
        public object Clone()
        {
            return new ArenaPlayer(this);
        }

        public ArenaPlayer(ArenaPlayer value)
        {
        }

        private readonly SkillSheet _skillSheet;
        private readonly SkillBuffSheet _skillBuffSheet;
        private readonly BuffSheet _buffSheet;
        private readonly ArenaSimulator _simulator;
        private readonly CharacterStats _stats;
        private readonly Skills _skills;
        private readonly Dictionary<int, Buff.Buff> _buffs = new Dictionary<int, Buff.Buff>();
        private readonly int _attackCountMax;

        private ArenaPlayer _target;
        private int _attackCount;

        // stat
        public int Level
        {
            get => _stats.Level;
            set => _stats.SetLevel(value);
        }

        public int CurrentHP
        {
            get => _stats.CurrentHP;
            set => _stats.CurrentHP = value;
        }

        public int HP => _stats.HP;
        public int ATK => _stats.ATK;
        public int DEF => _stats.DEF;
        public int CRI => _stats.CRI;
        public int HIT => _stats.HIT;
        public int SPD => _stats.SPD;
        public ElementalType OffensiveElementalType { get; }
        public ElementalType DefenseElementalType { get; }
        public bool IsDead => CurrentHP <= 0;
        public bool IsEnemy { get; }
        public BattleStatus.Skill SkillLog { get; private set; }

        public ArenaPlayer(
            ArenaSimulator simulator,
            ArenaPlayerDigest digest,
            ArenaSimulatorSheets sheets,
            bool isEnemy = false)
        {
            OffensiveElementalType = GetElementalType(digest.Equipments, ItemSubType.Weapon);
            DefenseElementalType = GetElementalType(digest.Equipments, ItemSubType.Armor);
            IsEnemy = isEnemy;

            _skillSheet = sheets.SkillSheet;
            _skillBuffSheet = sheets.SkillBuffSheet;
            _buffSheet = sheets.BuffSheet;
            _attackCountMax = AttackCountHelper.GetCountMax(digest.Level);

            _simulator = simulator;
            _stats = GetStat(digest, sheets);
            _skills = GetSkills(digest.Equipments, sheets.SkillSheet);

        }

        private ElementalType GetElementalType(IEnumerable<Equipment> equipments, ItemSubType itemSubType)
        {
            var equipment = equipments.FirstOrDefault(x => x.ItemSubType.Equals(itemSubType));
            return equipment?.ElementalType ?? ElementalType.Normal;
        }

        private static CharacterStats GetStat(ArenaPlayerDigest digest, ArenaSimulatorSheets sheets)
        {
            if (!sheets.CharacterSheet.TryGetValue(digest.CharacterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", digest.CharacterId);
            }

            var stats = new CharacterStats(row, digest.Level);

            stats.SetEquipments(digest.Equipments, sheets.EquipmentItemSetEffectSheet);

            var options = new List<StatModifier>();
            foreach (var itemId in digest.Costumes.Select(costume => costume.Id))
            {
                if (TryGetStats(sheets.CostumeStatSheet, itemId, out var option))
                {
                    options.AddRange(option);
                }
            }

            stats.SetOption(options);
            stats.EqualizeCurrentHPWithHP();
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

        private static Skills GetSkills(IEnumerable<Equipment> equipments, SkillSheet skillSheet)
        {
            var skills = new Skills();

            // normal attack
            if (!skillSheet.TryGetValue(GameConfig.DefaultAttackId, out var skillRow))
            {
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
            }

            var attack = SkillFactory.GetForArena(skillRow, 0, 100);
            skills.Add(attack);

            foreach (var skill in equipments.SelectMany(equipment => equipment.Skills))
            {
                skills.Add(skill);
            }

            foreach (var buffSkill in equipments.SelectMany(equipment => equipment.BuffSkills))
            {
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

        private void Act()
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

        private void ReduceDurationOfBuffs()
        {
#pragma warning disable LAA1002
            foreach (var pair in _buffs)
#pragma warning restore LAA1002
            {
                pair.Value.remainedDuration--;
            }
        }

        private void ReduceSkillCooldown()
        {
            _skills.ReduceCooldown();
        }

        private void UseSkill()
        {
            var selectedSkill = _skills.Select(_simulator.Random);
            if (selectedSkill is IArenaSkill arenaSkill)
            {
                SkillLog = arenaSkill.Use(
                    this,
                    _target,
                    _simulator.Turn,
                    BuffFactory.GetBuffs(selectedSkill, _skillBuffSheet, _buffSheet)
                );
            }

            if (!_skillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var row))
            {
                throw new KeyNotFoundException(
                    selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            _skills.SetCooldown(selectedSkill.SkillRow.Id, row.Cooldown);
        }

        private void RemoveBuffs()
        {
            var isApply = false;

            foreach (var key in _buffs.Keys.ToList())
            {
                var buff = _buffs[key];
                if (buff.remainedDuration > 0)
                {
                    continue;
                }

                _buffs.Remove(key);
                isApply = true;
            }

            if (isApply)
            {
                _stats.SetBuffs(_buffs.Values);
            }
        }

        public void Tick()
        {
            _root.Tick();
        }
        #endregion

        public void Spawn(ArenaPlayer target)
        {
            _target = target;
            InitAI();
        }

        public void AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (_buffs.TryGetValue(buff.RowData.GroupId, out var outBuff) &&
                outBuff.RowData.Id > buff.RowData.Id)
                return;

            var clone = (Buff.Buff) buff.Clone();
            _buffs[buff.RowData.GroupId] = clone;
            _stats.AddBuff(clone, updateImmediate);
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
                (int) AttackCountHelper.GetAdditionalCriticalChance(_attackCount, _attackCountMax);
            return CRI + additionalCriticalChance >= chance;
        }

        public virtual bool IsHit(ArenaPlayer caster)
        {
            var isHit = HitHelper.IsHit(
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

            var damageMultiplier = (int) AttackCountHelper.GetDamageMultiplier(_attackCount, _attackCountMax);
            damage *= damageMultiplier;
            return damage;
        }
    }
}
