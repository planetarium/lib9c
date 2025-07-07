using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    /// <summary>
    /// Represents equipment items that can be equipped by characters.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    ///
    /// <para>
    /// Field Order (List Format):
    /// Base fields (0~10): 11 fields from ItemUsable
    /// Equipment fields (11~22): equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp
    /// </para>
    ///
    /// <para>
    /// Equipment Properties:
    /// - Equipped: Whether the equipment is currently equipped
    /// - Level: Equipment enhancement level
    /// - Stat: Primary stat of the equipment
    /// - SetId: ID for equipment set bonuses
    /// - SpineResourcePath: Path to spine animation resource
    /// - IconId: Icon identifier for UI display
    /// - ByCustomCraft: Whether crafted through custom crafting
    /// - CraftWithRandom: Whether random options were applied during crafting
    /// - HasRandomOnlyIcon: Whether has random-only icon
    /// - OptionCountFromCombination: Number of options from combination
    /// - MadeWithMimisbrunnrRecipe: Whether made with Mimisbrunnr recipe
    /// - Exp: Experience points for leveling
    /// </para>
    /// </summary>
    /// <remarks>
    /// Equipment items can be enhanced, equipped, and provide various stats and bonuses.
    /// The equipment system supports both regular crafting and custom crafting with random options.
    ///
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create equipment
    /// var equipment = new Equipment(equipmentRow, Guid.NewGuid(), 1000L);
    ///
    /// // Enhance equipment
    /// equipment.LevelUp();
    ///
    /// // Equip/unequip
    /// equipment.Equipped = true;
    ///
    /// // Check stats
    /// var statValue = equipment.Stat.StatType;
    /// var setBonus = equipment.SetId;
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class Equipment : ItemUsable, IEquippableItem
    {
        // Field count constants for serialization
        private const int EQUIPMENT_FIELD_COUNT = ITEM_USABLE_FIELD_COUNT + 12; // base + equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp

        // FIXME: Whether the equipment is equipped or not has no asset value and must be removed from the state.
        public bool equipped;
        public int level;
        public long Exp;
        public int optionCountFromCombination;
        public int IconId;
        public bool ByCustomCraft;
        public bool CraftWithRandom;
        public bool HasRandomOnlyIcon;

        public DecimalStat Stat { get; private set; }
        public int SetId { get; private set; }
        public string SpineResourcePath { get; private set; }
        public bool MadeWithMimisbrunnrRecipe { get; set; }
        public StatType UniqueStatType => Stat.StatType;
        public bool Equipped => equipped;

        public decimal GetIncrementAmountOfEnhancement()
        {
            var stat = StatsMap.GetBaseStat(UniqueStatType);
            return Math.Max(1.0m, stat * 0.1m);
        }

        public Equipment(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex,
            bool madeWithMimisbrunnrRecipe = false, int iconId = 0)
            : base(data, id, requiredBlockIndex)
        {
            Stat = data.Stat;
            SetId = data.SetId;
            SpineResourcePath = data.SpineResourcePath;
            MadeWithMimisbrunnrRecipe = madeWithMimisbrunnrRecipe;
            Exp = data.Exp ?? 0L;
            IconId = iconId != 0 ? iconId : data.Id;
            ByCustomCraft = false;
            CraftWithRandom = false;
            HasRandomOnlyIcon = false;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        public Equipment(IValue serialized) : base(serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
            }
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
            if (dict.TryGetValue((Text) LegacyEquippedKey, out var value))
            {
                equipped = value.ToBoolean();
            }

            if (dict.TryGetValue((Text) LegacyLevelKey, out value))
            {
                try
                {
                    level = value.ToInteger();
                }
                catch (InvalidCastException)
                {
                    level = (Integer) value;
                }
            }

            if (dict.TryGetValue((Text)EquipmentExpKey, out value))
            {
                try
                {
                    Exp = value.ToLong();
                }
                catch (InvalidCastException)
                {
                    Exp = (Integer) value;
                }
            }
            else
            {
                Exp = 0L;
            }

            IconId = dict.TryGetValue((Text)EquipmentIconIdKey, out value) ? (Integer)value : Id;
            ByCustomCraft = dict.TryGetValue((Text)ByCustomCraftKey, out value) && value.ToBoolean();
            CraftWithRandom = dict.TryGetValue((Text)CraftWithRandomKey, out value) && value.ToBoolean();
            HasRandomOnlyIcon = dict.TryGetValue((Text)HasRandomOnlyIconKey, out value) && value.ToBoolean();

            if (dict.TryGetValue((Text)LegacyStatKey, out value))
            {
                Stat = value.ToDecimalStat();
            }

            if (dict.TryGetValue((Text) LegacySetIdKey, out value))
            {
                SetId = value.ToInteger();
            }

            if (dict.TryGetValue((Text) LegacySpineResourcePathKey, out value))
            {
                SpineResourcePath = (Text) value;
            }

            if (dict.TryGetValue((Text) OptionCountFromCombinationKey, out value))
            {
                optionCountFromCombination = value.ToInteger();
            }

            if (dict.TryGetValue((Text) MadeWithMimisbrunnrRecipeKey, out value))
            {
                MadeWithMimisbrunnrRecipe = value.ToBoolean();
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [baseData..., equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for Equipment
            if (list.Count < EQUIPMENT_FIELD_COUNT)
            {
                var fieldNames = string.Join(", ", GetFieldNames());
                throw new ArgumentException(
                    $"Invalid list length for {GetType().Name}: expected at least {EQUIPMENT_FIELD_COUNT}, got {list.Count}. " +
                    $"Required fields: {fieldNames}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // Always read EQUIPMENT_FIELD_COUNT fields
            // base fields (0~10): 11 fields from ItemUsable
            // Equipment fields (11~22): equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp

            // equipped (index 11)
            equipped = list[11].ToBoolean();

            // level (index 12)
            level = (Integer)list[12];

            // stat (index 13)
            Stat = list[13].ToDecimalStat();

            // setId (index 14)
            SetId = (Integer)list[14];

            // spineResourcePath (index 15)
            SpineResourcePath = list[15].ToDotnetString();

            // iconId (index 16)
            IconId = (Integer)list[16];

            // byCustomCraft (index 17)
            ByCustomCraft = list[17].ToBoolean();

            // craftWithRandom (index 18)
            CraftWithRandom = list[18].ToBoolean();

            // hasRandomOnlyIcon (index 19)
            HasRandomOnlyIcon = list[19].ToBoolean();

            // optionCountFromCombination (index 20)
            optionCountFromCombination = (Integer)list[20];

            // madeWithMimisbrunnrRecipe (index 21)
            MadeWithMimisbrunnrRecipe = list[21].ToBoolean();

            // exp (index 22)
            Exp = (Integer)list[22];
        }

        protected Equipment(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        /// <summary>
        /// Serializes the equipment to List format (new format).
        /// Order: [baseData..., equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize()
        {
            var list = ((List)base.Serialize())
                .Add(equipped.Serialize())
                .Add(level)
                .Add(Stat.SerializeForLegacyEquipmentStat())
                .Add(SetId)
                .Add(SpineResourcePath)
                .Add(IconId)
                .Add(ByCustomCraft)
                .Add(CraftWithRandom)
                .Add(HasRandomOnlyIcon)
                .Add(optionCountFromCombination)
                .Add(MadeWithMimisbrunnrRecipe)
                .Add(Exp);

            return list;
        }

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        protected override string[] GetFieldNames()
        {
            return base.GetFieldNames().Concat(new[]
            {
                "equipped",
                "level",
                "stat",
                "setId",
                "spineResourcePath",
                "iconId",
                "byCustomCraft",
                "craftWithRandom",
                "hasRandomOnlyIcon",
                "optionCountFromCombination",
                "madeWithMimisbrunnrRecipe",
                "exp"
            }).ToArray();
        }

        public void Equip()
        {
            equipped = true;
        }

        public void Unequip()
        {
            equipped = false;
        }

        [Obsolete("Use LevelUp")]
        public void LevelUpV1()
        {
            level++;
            var increment = GetIncrementAmountOfEnhancement();
            StatsMap.AddStatValue(UniqueStatType, increment);
            if (new[] {4, 7, 10}.Contains(level) &&
                GetOptionCount() > 0)
            {
                UpdateOptions();
            }
        }

        [Obsolete("Since ItemEnhancement12, Use `SetLevel` instead.")]
        public void LevelUp(IRandom random, EnhancementCostSheetV2.Row row, bool isGreatSuccess)
        {
            level++;
            var rand = isGreatSuccess ? row.BaseStatGrowthMax
                : random.Next(row.BaseStatGrowthMin, row.BaseStatGrowthMax + 1);
            var ratio = rand.NormalizeFromTenThousandths();
            var baseStat = StatsMap.GetBaseStat(UniqueStatType) * ratio;
            if (baseStat > 0)
            {
                baseStat = Math.Max(1.0m, baseStat);
            }

            StatsMap.AddStatValue(UniqueStatType, baseStat);

            if (GetOptionCount() > 0)
            {
                UpdateOptionsV2(random, row, isGreatSuccess);
            }
        }

        public void SetLevel(IRandom random, int targetLevel, EnhancementCostSheetV3 sheet)
        {
            var startLevel = level;
            level = targetLevel;
            for (var i = startLevel + 1; i <= targetLevel; i++)
            {
                var row = sheet.OrderedList.First(
                    r => r.Level == i && r.Grade == Grade && r.ItemSubType == ItemSubType
                );
                var rand = random.Next(row.BaseStatGrowthMin, row.BaseStatGrowthMax + 1);
                var ratio = rand.NormalizeFromTenThousandths();
                var baseStat = StatsMap.GetBaseStat(UniqueStatType) * ratio;
                if (baseStat > 0)
                {
                    baseStat = Math.Max(1.0m, baseStat);
                }

                StatsMap.AddStatValue(UniqueStatType, baseStat);

                if (GetOptionCount() > 0)
                {
                    UpdateOptionsV3(random, row);
                }
            }
        }

        public long GetRealExp(EquipmentItemSheet itemSheet, EnhancementCostSheetV3 costSheet)
        {
            if (Exp != 0) return Exp;
            if (level == 0)
            {
                return (long)itemSheet.OrderedList.First(r => r.Id == Id).Exp!;
            }

            return costSheet.OrderedList.First(r =>
                r.ItemSubType == ItemSubType && r.Grade == Grade &&
                r.Level == level).Exp;
        }

        private void UpdateOptions()
        {
            foreach (var stat in StatsMap.GetAdditionalStats())
            {
                StatsMap.SetStatAdditionalValue(
                    stat.StatType,
                    stat.AdditionalValue * 1.3m);
            }

            var skills = new List<Skill.Skill>();
            skills.AddRange(Skills);
            skills.AddRange(BuffSkills);
            foreach (var skill in skills)
            {
                var chance = decimal.ToInt32(skill.Chance * 1.3m);
                var power = decimal.ToInt32(skill.Power * 1.3m);
                var statPowerRatio = decimal.ToInt32(skill.StatPowerRatio * 1.3m);
                skill.Update(chance, power, statPowerRatio);
            }
        }

        [Obsolete("Since ItemEnhancement12, Use UpdateOptionV3 instead.")]
        private void UpdateOptionsV2(IRandom random, EnhancementCostSheetV2.Row row, bool isGreatSuccess)
        {
            foreach (var stat in StatsMap.GetAdditionalStats())
            {
                var rand = isGreatSuccess
                    ? row.ExtraStatGrowthMax
                    : random.Next(row.ExtraStatGrowthMin, row.ExtraStatGrowthMax + 1);
                var ratio = rand.NormalizeFromTenThousandths();
                var addValue = stat.AdditionalValue * ratio;
                if (addValue > 0)
                {
                    addValue = Math.Max(1.0m, addValue);
                }

                StatsMap.SetStatAdditionalValue(stat.StatType, stat.AdditionalValue + addValue);
            }

            var skills = new List<Skill.Skill>();
            skills.AddRange(Skills);
            skills.AddRange(BuffSkills);
            foreach (var skill in skills)
            {
                var chanceRand = isGreatSuccess ? row.ExtraSkillChanceGrowthMax
                    : random.Next(row.ExtraSkillChanceGrowthMin, row.ExtraSkillChanceGrowthMax + 1);
                var chanceRatio = chanceRand.NormalizeFromTenThousandths();
                var addChance = skill.Chance * chanceRatio;
                if (addChance > 0)
                {
                    addChance = Math.Max(1.0m, addChance);
                }

                var damageRand = isGreatSuccess ? row.ExtraSkillDamageGrowthMax
                    : random.Next(row.ExtraSkillDamageGrowthMin, row.ExtraSkillDamageGrowthMax + 1);
                var damageRatio = damageRand.NormalizeFromTenThousandths();
                var addPower = skill.Power * damageRatio;
                if (addPower > 0)
                {
                    addPower = Math.Max(1.0m, addPower);
                }

                var addStatPowerRatio = skill.StatPowerRatio * damageRatio;
                if (addStatPowerRatio > 0)
                {
                    addStatPowerRatio = Math.Max(1.0m, addStatPowerRatio);
                }

                var chance = skill.Chance + (int)addChance;
                var power = skill.Power + (int)addPower;
                var statPowerRatio = skill.StatPowerRatio + (int)addStatPowerRatio;

                skill.Update(chance, power, statPowerRatio);
            }
        }

        private void UpdateOptionsV3(IRandom random, EnhancementCostSheetV3.Row row)
        {
            foreach (var stat in StatsMap.GetAdditionalStats())
            {
                var rand = random.Next(row.ExtraStatGrowthMin, row.ExtraStatGrowthMax + 1);
                var ratio = rand.NormalizeFromTenThousandths();
                var addValue = stat.AdditionalValue * ratio;
                if (addValue > 0)
                {
                    addValue = Math.Max(1.0m, addValue);
                }

                StatsMap.SetStatAdditionalValue(stat.StatType, stat.AdditionalValue + addValue);
            }

            var skills = new List<Skill.Skill>();
            skills.AddRange(Skills);
            skills.AddRange(BuffSkills);
            foreach (var skill in skills)
            {
                var chanceRand = random.Next(row.ExtraSkillChanceGrowthMin,
                    row.ExtraSkillChanceGrowthMax + 1);
                var chanceRatio = chanceRand.NormalizeFromTenThousandths();
                var addChance = skill.Chance * chanceRatio;
                if (addChance > 0)
                {
                    addChance = Math.Max(1.0m, addChance);
                }

                var damageRand = random.Next(row.ExtraSkillDamageGrowthMin,
                    row.ExtraSkillDamageGrowthMax + 1);
                var damageRatio = damageRand.NormalizeFromTenThousandths();
                var addPower = skill.Power * damageRatio;
                if (addPower > 0)
                {
                    addPower = Math.Max(1.0m, addPower);
                }

                var addStatPowerRatio = skill.StatPowerRatio * damageRatio;
                if (addStatPowerRatio > 0)
                {
                    addStatPowerRatio = Math.Max(1.0m, addStatPowerRatio);
                }

                var chance = skill.Chance + (int)addChance;
                var power = skill.Power + (int)addPower;
                var statPowerRatio = skill.StatPowerRatio + (int)addStatPowerRatio;

                skill.Update(chance, power, statPowerRatio);
            }
        }

        protected bool Equals(Equipment other)
        {
            return base.Equals(other) && equipped == other.equipped && level == other.level &&
                Exp == other.Exp && Equals(Stat, other.Stat) && SetId == other.SetId &&
                SpineResourcePath == other.SpineResourcePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Equipment) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ equipped.GetHashCode();
                hashCode = (hashCode * 397) ^ level;
                hashCode = (hashCode * 397) ^ (Stat != null ? Stat.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SetId;
                hashCode = (hashCode * 397) ^ (SpineResourcePath != null ? SpineResourcePath.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
