using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Battle
{
    public static class CPHelper
    {
        public static long TotalCP(
            IReadOnlyCollection<Equipment> equipments,
            IReadOnlyCollection<Costume> costumes,
            IReadOnlyCollection<RuneOptionSheet.Row.RuneOptionInfo> runeOptions,
            int level,
            CharacterSheet.Row row,
            CostumeStatSheet costumeStatSheet,
            List<StatModifier> collectionStatModifiers,
            int runeLevelBonus
        )
        {
            decimal levelStatsCp = GetStatsCP(row.ToStats(level), level);
            var collectionCp = 0m;
            // CharacterStats.BaseStats CP equals row.ToStats
            if (collectionStatModifiers.Any())
            {
                // Prepare CharacterStats for calculate collection Stats
                var characterStats = new CharacterStats(row, level);
                characterStats.ConfigureStats(equipments, costumes, runeOptions, costumeStatSheet, collectionStatModifiers, runeLevelBonus);
                foreach (var (statType, value) in characterStats.CollectionStats.GetStats())
                {
                    collectionCp += GetStatCP(statType, value);
                }
            }

            var equipmentsCp = 0L;
            var costumeCp = 0L;
            var runeCp = 0L;
            var runeLevelBonusCp = 0m;

            foreach (var equipment in equipments)
            {
                equipmentsCp += GetCP(equipment);
            }

            foreach (var costume in costumes)
            {
                costumeCp += GetCP(costume, costumeStatSheet);
            }

            foreach (var runeOption in runeOptions)
            {
                runeCp += runeOption.Cp;
                runeLevelBonusCp += runeOption.Stats.Sum(optionInfo =>
                    GetStatCP(
                        optionInfo.stat.StatType,
                        optionInfo.stat.BaseValue * runeLevelBonus / 100_000m
                    )
                );
            }

            var totalCp = DecimalToLong(
                levelStatsCp + equipmentsCp + costumeCp + runeCp + runeLevelBonusCp + collectionCp
            );
            return totalCp;
        }

        [Obsolete("Use TotalCP")]
        public static long GetCP(AvatarState avatarState, CharacterSheet characterSheet)
        {
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var levelStats = row.ToStats(avatarState.level);
            var levelStatsCP = GetStatsCP(levelStats, avatarState.level);
            var equipmentsCP = avatarState.inventory.Items
                .Select(item => item.item)
                .OfType<Equipment>()
                .Where(equipment => equipment.equipped)
                .Sum(GetCP);

            return DecimalToLong(levelStatsCP + equipmentsCP);
        }

        [Obsolete("Use TotalCP")]
        public static long GetCPV2(
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CostumeStatSheet costumeStatSheet)
        {
            var current = GetCP(avatarState, characterSheet);
            var costumeCP = avatarState.inventory.Costumes
                .Where(c => c.equipped)
                .Sum(c => GetCP(c, costumeStatSheet));

            return DecimalToLong(current + costumeCP);
        }

        public static long GetCP(ItemUsable itemUsable)
        {
            var statsCP = GetStatsCP(itemUsable.StatsMap);
            var skills = itemUsable.Skills.Concat(itemUsable.BuffSkills).ToArray();
            return DecimalToLong(statsCP * GetSkillsMultiplier(skills.Length));
        }

        public static long GetCP(Costume costume, CostumeStatSheet sheet)
        {
            var statsMap = new StatsMap();
            foreach (var r in sheet.OrderedList.Where(r => r.CostumeId == costume.Id))
            {
                statsMap.AddStatValue(r.StatType, r.Stat);
            }

            return DecimalToLong(GetStatsCP(statsMap));
        }

        [Obsolete("Use GetCp")]
        public static long GetCP(INonFungibleItem tradableItem, CostumeStatSheet sheet)
        {
            if (tradableItem is ItemUsable itemUsable)
            {
                return GetCP(itemUsable);
            }

            if (tradableItem is Costume costume)
            {
                return GetCP(costume, sheet);
            }

            return 0;
        }

        public static decimal GetStatsCP(IStats stats, int characterLevel = 1)
        {
            var statTuples = stats.GetStats(true);
            decimal cp = 0m;
            foreach (var tuple in statTuples)
            {
                cp += GetStatCP(tuple.statType, tuple.value, characterLevel);
            }

            return cp;
        }

        public static decimal GetStatCP(StatType statType, decimal statValue, int characterLevel = 1)
        {
            switch (statType)
            {
                case StatType.NONE:
                    return 0m;
                case StatType.HP:
                    return GetCPOfHP(statValue);
                case StatType.ATK:
                    return GetCPOfATK(statValue);
                case StatType.DEF:
                    return GetCPOfDEF(statValue);
                case StatType.CRI:
                    return GetCPOfCRI(statValue, characterLevel);
                case StatType.HIT:
                    return GetCPOfHIT(statValue);
                case StatType.SPD:
                    return GetCPOfSPD(statValue);
                case StatType.DRV:
                    return GetCPOfDRV(statValue);
                case StatType.DRR:
                    return GetCPOfDRR(statValue, characterLevel);
                case StatType.CDMG:
                    return GetCPOfCDMG(statValue, characterLevel);
                case StatType.ArmorPenetration:
                    return GetCPOfArmorPenetration(statValue);
                case StatType.Thorn:
                    return GetCPOfThorn(statValue);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static decimal GetCPOfHP(decimal value) => value * 0.7m;

        public static decimal GetCPOfATK(decimal value) => value * 10.5m;

        public static decimal GetCPOfDEF(decimal value) => value * 10.5m;

        public static decimal GetCPOfSPD(decimal value) => value * 3m;

        public static decimal GetCPOfHIT(decimal value) => value * 2.3m;

        // NOTE : Temp formula
        public static decimal GetCPOfDRV(decimal value) => value * 10.5m;

        // NOTE : Temp formula
        public static decimal GetCPOfDRR(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        public static decimal GetCPOfCRI(decimal value, int characterLevel) =>
            value * characterLevel * 20m;

        // NOTE : Temp formula
        public static decimal GetCPOfCDMG(decimal value, int characterLevel) =>
            value * characterLevel * 3m;

        public static decimal GetCPOfArmorPenetration(decimal value) =>
            value * 5m;

        public static decimal GetCPOfThorn(decimal value) =>
            value * 1m;

        // NOTE: If a stat type is added or Cp calculation logic is changed, You must change this function too.
        public static decimal ConvertCpToStat(StatType statType, decimal cp, int characterLevel)
        {
            switch (statType)
            {
                case StatType.HP:
                    return cp / 0.7m;
                case StatType.ATK:
                    return cp / 10.5m;
                case StatType.DEF:
                    return cp / 10.5m;
                case StatType.CRI:
                    return cp / characterLevel / 20m;
                case StatType.HIT:
                    return cp / 2.3m;
                case StatType.SPD:
                    return cp / 3m;
                case StatType.DRV:
                    return cp / 10.5m;
                case StatType.DRR:
                    return cp / characterLevel / 20m;
                case StatType.CDMG:
                    return cp / characterLevel / 3m;
                case StatType.ArmorPenetration:
                    return cp / 5m;
                case StatType.Thorn:
                    return cp / 1m;
                case StatType.NONE:
                default:
                    // throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
                    return 0m;
            }
        }

        public static decimal GetSkillsMultiplier(int skillsCount)
        {
            switch (skillsCount)
            {
                case 0:
                    return 1m;
                case 1:
                    return 1.15m;
                default:
                    return 1.35m;
            }
        }

        public static long DecimalToLong(decimal value)
        {
            if (value > long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long) value;
        }
    }
}
