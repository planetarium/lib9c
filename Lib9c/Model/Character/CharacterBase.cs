using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if TEST_LOG
using System.Text;
using UnityEngine;
#endif
using BTAI;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class CharacterBase : ICloneable
    {
        public const decimal CriticalMultiplier = 1.5m;

        public readonly Guid Id = Guid.NewGuid();

        [NonSerialized]
        public Simulator Simulator;

        public ElementalType atkElementType;
        public float attackRange;
        public ElementalType defElementType;

        public readonly Skills Skills = new Skills();
        public readonly Skills BuffSkills = new Skills();
        public readonly Dictionary<int, Buff.Buff> Buffs = new Dictionary<int, Buff.Buff>();
        public IEnumerable<StatBuff> StatBuffs => Buffs.Values.OfType<StatBuff>();
        public IEnumerable<ActionBuff> ActionBuffs => Buffs.Values.OfType<ActionBuff>();
        public readonly List<CharacterBase> Targets = new List<CharacterBase>();

        public CharacterSheet.Row RowData { get; }
        public int CharacterId { get; }
        public SizeType SizeType { get; }
        public float RunSpeed { get; }
        public CharacterStats Stats { get; }

        public int Level
        {
            get => Stats.Level;
            set => Stats.SetStats(value);
        }

        public long HP => Stats.HP;
        public long AdditionalHP => Stats.BuffStats.HP;
        public long ATK => Stats.ATK;
        public long DEF => Stats.DEF;
        public long CRI => Stats.CRI;
        public long HIT => Stats.HIT;
        public long SPD => Stats.SPD;
        public long DRV => Stats.DRV;
        public long DRR => Stats.DRR;
        public long CDMG => Stats.CDMG;
        public long ArmorPenetration => Stats.ArmorPenetration;
        public long Thorn => Stats.Thorn;

        private long _currentHP;

        public long CurrentHP
        {
            get => _currentHP;
            set => _currentHP = Math.Min(Math.Max(0, value), HP);
        }

        public bool IsDead => CurrentHP <= 0;

        public int AttackCount { get; set; }
        public int AttackCountMax { get; protected set; }
        public BattleStatus.Skill usedSkill { get; set; }

        protected CharacterBase(Simulator simulator, CharacterSheet characterSheet, int characterId, int level,
            IEnumerable<StatModifier> optionalStatModifiers = null)
        {
            Simulator = simulator;

            if (!characterSheet.TryGetValue(characterId, out var row))
                throw new SheetRowNotFoundException("CharacterSheet", characterId);

            RowData = row;
            CharacterId = characterId;
            Stats = new CharacterStats(RowData, level);
            if (!(optionalStatModifiers is null))
            {
                Stats.AddCostume(optionalStatModifiers);
            }
            ResetCurrentHP();

            Skills.Clear();

            SizeType = RowData.SizeType;
            atkElementType = RowData.ElementalType;
            defElementType = RowData.ElementalType;
            RunSpeed = RowData.RunSpeed;
            attackRange = RowData.AttackRange;
            AttackCountMax = 0;
        }

        protected CharacterBase(
            Simulator simulator,
            CharacterStats stat,
            int characterId,
            ElementalType elementalType,
            CharacterSheet.Row rowData = null,
            SizeType sizeType = SizeType.XL,
            float attackRange = 4,
            float runSpeed = 0.3f)
        {
            Simulator = simulator;
            Stats = stat;
            RowData = rowData;
            CharacterId = characterId;

            SizeType = sizeType;
            atkElementType = elementalType;
            defElementType = elementalType;
            this.attackRange = attackRange;
            RunSpeed = runSpeed;

            Skills.Clear();
            ResetCurrentHP();
            AttackCountMax = 0;
        }

        protected CharacterBase(CharacterBase value)
        {
            _root = value._root;
            Id = value.Id;
            Simulator = value.Simulator;

            CharacterId = value.CharacterId;
            SizeType = value.SizeType;
            atkElementType = value.atkElementType;
            defElementType = value.defElementType;
            attackRange = value.attackRange;
            RunSpeed = value.RunSpeed;

            // 스킬은 변하지 않는다는 가정 하에 얕은 복사.
            Skills = value.Skills;
            // 버프는 컨테이너도 옮기고,
            Buffs = new Dictionary<int, Buff.Buff>();
#pragma warning disable LAA1002
            foreach (var pair in value.Buffs)
#pragma warning restore LAA1002
            {
                // 깊은 복사까지 꼭.
                Buffs.Add(pair.Key, (Buff.Buff) pair.Value.Clone());
            }

            // 타갯은 컨테이너만 옮기기.
            Targets = new List<CharacterBase>(value.Targets);
            // 캐릭터 테이블 데이타는 변하지 않는다는 가정 하에 얕은 복사.
            RowData = value.RowData;
            Stats = new CharacterStats(value.Stats);
            AttackCountMax = value.AttackCountMax;
            CurrentHP = value.CurrentHP;
        }

        public abstract object Clone();

        #region Behaviour Tree

        [NonSerialized]
        private Root _root;

        protected CharacterBase(CharacterSheet.Row row)
        {
            RowData = row;
        }

        public virtual void InitAI()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(Act)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAIV1()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV1)
            );
        }

        [Obsolete("Use InitAI")]
        public void InitAIV2()
        {
            SetSkill();

            _root = new Root();
            _root.OpenBranch(
                BT.Call(ActV2)
            );
        }

        public void Tick()
        {
            _root.Tick();
        }

        private bool IsAlive()
        {
            return !IsDead;
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

        protected virtual void ReduceSkillCooldown()
        {
            Skills.ReduceCooldown();
        }

        [Obsolete("ReduceSkillCooldown")]
        private void ReduceSkillCooldownV1()
        {
            Skills.ReduceCooldownV1();
        }

        protected virtual BattleStatus.Skill UseSkill()
        {
            var selectedSkill = Skills.Select(Simulator.Random);
            bool log = Simulator.LogEvent;
            usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    Stats,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                ),
                log
            );

            if (!Simulator.SkillSheet.TryGetValue(selectedSkill.SkillRow.Id, out var sheetSkill))
            {
                throw new KeyNotFoundException(selectedSkill.SkillRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            Skills.SetCooldown(selectedSkill.SkillRow.Id, sheetSkill.Cooldown);
            if (log)
            {
                Simulator.Log.Add(usedSkill);
            }
            return usedSkill;
        }

        [Obsolete("Use UseSkill")]
        private BattleStatus.Skill UseSkillV1()
        {
            var selectedSkill = Skills.SelectV1(Simulator.Random);

            usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    Stats,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                ),
                Simulator.LogEvent
            );

            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);
            return usedSkill;
        }

        [Obsolete("Use UseSkill")]
        private BattleStatus.Skill UseSkillV2()
        {
            var selectedSkill = Skills.SelectV2(Simulator.Random);

            usedSkill = selectedSkill.Use(
                this,
                Simulator.WaveTurn,
                BuffFactory.GetBuffs(
                    Stats,
                    selectedSkill,
                    Simulator.SkillBuffSheet,
                    Simulator.StatBuffSheet,
                    Simulator.SkillActionBuffSheet,
                    Simulator.ActionBuffSheet
                ),
                Simulator.LogEvent
            );

            Skills.SetCooldown(selectedSkill.SkillRow.Id, selectedSkill.SkillRow.Cooldown);
            Simulator.Log.Add(usedSkill);
            return usedSkill;
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

            Stats.SetBuffs(StatBuffs, Simulator.BuffLimitSheet);
            if (Simulator.LogEvent)
            {
                Simulator.Log.Add(new RemoveBuffs((CharacterBase) Clone()));
            }
        }

        protected virtual void EndTurn()
        {
#if TEST_LOG
            UnityEngine.Debug.LogWarning($"{nameof(RowData.Id)} : {RowData.Id} / Turn Ended.");
#endif
        }

        #endregion

        #region Buff

        /// <summary>
        /// Add buff/debuff to target; it means buff/debuff is used by caster.
        /// When `Dispel` is used, it can remove prev. debuffs on target.
        /// All the removed debuffs will be returned and saved in battle log.
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="updateImmediate"></param>
        /// <returns>An enumerable of removed debuffs from target. `null` will be returned if nothing eliminated.</returns>
        public IEnumerable<Buff.Buff> AddBuff(Buff.Buff buff, bool updateImmediate = true)
        {
            if (Buffs.TryGetValue(buff.BuffInfo.GroupId, out var outBuff) &&
                outBuff.BuffInfo.Id > buff.BuffInfo.Id)
                return null;

            var dispelList = new List<Buff.Buff>();
            switch (buff)
            {
                // StatBuff Modifies stats
                case StatBuff stat:
                {
                    var clone = (StatBuff)stat.Clone();
                    if (Buffs.TryGetValue(stat.RowData.GroupId, out var current))
                    {
                        var stack = ((StatBuff) current).Stack + 1;
                        clone.SetStack(stack);
                    }
                    Buffs[stat.RowData.GroupId] = clone;
                    Stats.AddBuff(clone, Simulator.BuffLimitSheet, updateImmediate);
                    break;
                }
                case ActionBuff action:
                {
                    var clone = (ActionBuff)action.Clone();

                    switch (action)
                    {
                        // Stun freezes target
                        case Stun stun:
                        {
                            Buffs[stun.BuffInfo.GroupId] = clone;
                            break;
                        }
                        // Dispel removes debuffs
                        case Dispel dispel:
                        {
                            Buffs[dispel.BuffInfo.GroupId] = clone;

                            dispelList = Buffs.Values.Where(
                                            bff => bff.IsDebuff() &&
                                                Simulator.Random.Next(0, 100) <
                                                dispel.BuffInfo.Chance).ToList();

                            foreach (var bff in dispelList)
                            {
                                switch (bff)
                                {
                                    case StatBuff statBuff:
                                        RemoveStatBuff(statBuff);
                                        break;
                                    case ActionBuff actionBuff:
                                        RemoveActionBuff(actionBuff);
                                        break;
                                }
                            }

                            break;
                        }
                        default:
                            Buffs[action.RowData.GroupId] = clone;
                            break;
                    }

                    break;
                }
            }

            return dispelList;
        }

        public void RemoveActionBuff(ActionBuff removedBuff)
        {
            Buffs.Remove(removedBuff.RowData.GroupId);
        }

        public void RemoveStatBuff(StatBuff removedBuff)
        {
            Stats.RemoveBuff(removedBuff);
            Buffs.Remove(removedBuff.RowData.GroupId);
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
                RemoveStatBuff(removedBuff);
            }
        }

        #endregion

        public void ResetCurrentHP()
        {
            CurrentHP = Math.Max(0, Stats.HP);
        }

        public bool IsCritical(bool considerAttackCount = true)
        {
            var chance = Simulator.Random.Next(0, 100);
            if (!considerAttackCount)
                return CRI >= chance;

            var additionalCriticalChance =
                AttackCountHelper.GetAdditionalCriticalChance(AttackCount, AttackCountMax);
            return CRI + additionalCriticalChance >= chance;
        }

        public bool IsHit(ElementalResult result)
        {
            var correction = result == ElementalResult.Lose ? 50 : 0;
            var chance = Simulator.Random.Next(0, 100);
            return chance >= Stats.HIT + correction;
        }

        public virtual bool IsHit(CharacterBase caster)
        {
            if (caster.ActionBuffs.Any(buff => buff is Focus))
            {
                return true;
            }

            var isHit = HitHelper.IsHit(caster.Level, caster.HIT, Level, HIT, Simulator.Random.Next(0, 100));
            if (!isHit)
            {
                caster.AttackCount = 0;
            }

            return isHit;
        }

        public long GetDamage(long damage, bool considerAttackCount = true)
        {
            if (!considerAttackCount)
                return damage;

            AttackCount++;
            if (AttackCount > AttackCountMax)
            {
                AttackCount = 1;
            }

            var damageMultiplier = AttackCountHelper.GetDamageMultiplier(AttackCount, AttackCountMax);
            damage *= damageMultiplier;

#if TEST_LOG
            var sb = new StringBuilder(RowData.Id.ToString());
            sb.Append($" / {nameof(AttackCount)}: {AttackCount}");
            sb.Append($" / {nameof(AttackCountMax)}: {AttackCountMax}");
            sb.Append($" / {nameof(damageMultiplier)}: {damageMultiplier}");
            sb.Append($" / {nameof(damage)}: {damage}");
            Debug.LogWarning(sb.ToString());
#endif

            return damage;
        }

        public void Die()
        {
            OnDead();
        }

        protected virtual void OnDead()
        {
            if (Simulator.LogEvent)
            {
                var dead = new Dead((CharacterBase) Clone());
                Simulator.Log.Add(dead);
            }
        }

        public void Heal(long heal)
        {
            CurrentHP += heal;
        }

        protected virtual void SetSkill()
        {
            usedSkill = null;
            if (!Simulator.SkillSheet.TryGetValue(GameConfig.DefaultAttackId, out var skillRow))
            {
                throw new KeyNotFoundException(GameConfig.DefaultAttackId.ToString(CultureInfo.InvariantCulture));
            }

            var attack = SkillFactory.GetV1(skillRow, 0, 100);
            Skills.Add(attack);
        }

        public bool GetChance(int chance)
        {
            return chance > Simulator.Random.Next(0, 100);
        }

        private void Act()
        {
            usedSkill = null;
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldown();
                if (OnPreSkill())
                {
                    usedSkill = new Tick((CharacterBase)Clone());
                    Simulator.Log.Add(usedSkill);
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
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void ActV1()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldownV1();
                OnPreSkill();
                usedSkill = UseSkillV1();
                if (usedSkill != null)
                {
                    OnPostSkill(usedSkill);
                }
                RemoveBuffs();
            }
            EndTurn();
        }

        [Obsolete("Use Act")]
        private void ActV2()
        {
            if (IsAlive())
            {
                ReduceDurationOfBuffs();
                ReduceSkillCooldownV1();
                OnPreSkill();
                usedSkill = UseSkillV2();
                if (usedSkill != null)
                {
                    OnPostSkill(usedSkill);
                }
                RemoveBuffs();
            }
            EndTurn();
        }

        protected virtual bool OnPreSkill()
        {
            return Buffs.Values.Any(buff => buff is Stun);
        }

        protected virtual void OnPostSkill(BattleStatus.Skill usedSkill)
        {
            var log = Simulator.LogEvent;
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
                                     vampiric.GiveEffect(this, skillInfo, Simulator.WaveTurn, log))
                                 .Where(_ => log))
                    {
                        Simulator.Log.Add(effect);
                    }
                }
            }

            var bleeds = Buffs.Values.OfType<Bleed>().OrderBy(x => x.BuffInfo.Id);
            foreach (var bleed in bleeds)
            {
                var effect = bleed.GiveEffect(this, Simulator.WaveTurn, log);
                if (log)
                {
                    Simulator.Log.Add(effect);
                }
            }

            // Apply thorn damage if target has thorn
            foreach (var skillInfo in attackSkills)
            {
                if (skillInfo.Thorn > 0)
                {
                    var effect = GiveThornDamage(skillInfo.Thorn);
                    if (log)
                    {
                        Simulator.Log.Add(effect);
                    }
                }

                if (skillInfo.IceShield is not null)
                {
                    var frostBite = skillInfo.IceShield.FrostBite(Simulator.StatBuffSheet, Simulator.BuffLinkSheet);
                    AddBuff(frostBite);
                    Simulator.Log.Add(new Tick(frostBite.RowData.Id, (CharacterBase)Clone(), usedSkill.SkillInfos, usedSkill.BuffInfos));
                }
            }

            if (IsDead)
            {
                Die();
            }

            FinishTargetIfKilledForBeforeV100310(usedSkill);
            FinishTargetIfKilled(usedSkill);
        }

        internal BattleStatus.Skill GiveThornDamage(long targetThorn)
        {
            bool log = Simulator.LogEvent;
            // Copy not damaged character
            var clone = log ? (CharacterBase)Clone() : null;
            // minimum 1 damage
            var thornDamage = Math.Max(1, targetThorn - DEF);
            CurrentHP -= thornDamage;
            if (log)
            {
                // Copy new damaged character
                var damageInfos = new List<BattleStatus.Skill.SkillInfo>()
                {
                    new BattleStatus.Skill.SkillInfo(
                        Id,
                        IsDead,
                        thornDamage,
                        thornDamage,
                        false,
                        SkillCategory.TickDamage,
                        Simulator.WaveTurn,
                        target: (CharacterBase)Clone())
                };
                var tickDamage = new TickDamage(
                    default,
                    clone,
                    damageInfos,
                    null);
                return tickDamage;
            }

            return null;
        }

        private void FinishTargetIfKilledForBeforeV100310(BattleStatus.Skill usedSkill)
        {
            var isFirst = true;
            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.IsDead)
                {
                    continue;
                }

                if (isFirst)
                {
                    isFirst = false;
                    continue;
                }

                var target = Targets.FirstOrDefault(i =>
                    i.Id == info.CharacterId);
                switch (target)
                {
                    case Player player:
                    {
                        var quest = new KeyValuePair<int, int>((int)QuestEventType.Die, 1);
                        player.eventMapForBeforeV100310.Add(quest);

                        break;
                    }
                    case Enemy enemy:
                    {
                        if (enemy.Targets[0] is Player targetPlayer)
                        {
                            var quest = new KeyValuePair<int, int>(enemy.CharacterId, 1);
                            targetPlayer.monsterMapForBeforeV100310.Add(quest);
                        }

                        break;
                    }
                }
            }
        }

        private void FinishTargetIfKilled(BattleStatus.Skill usedSkill)
        {
            var killedTargets = new List<CharacterBase>();
            foreach (var info in usedSkill.SkillInfos)
            {
                if (!info.IsDead)
                {
                    continue;
                }

                var target = Targets.FirstOrDefault(i => i.Id == info.CharacterId);
                if (!killedTargets.Contains(target))
                {
                    killedTargets.Add(target);
                }
            }

            foreach (var target in killedTargets)
            {
                target?.Die();
            }
        }
    }
}
