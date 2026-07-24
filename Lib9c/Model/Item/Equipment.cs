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
using Nekoyume.Helper;

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
    /// Potential field (23): potential — optional trailing field, absent in equipment serialized before the potential layer
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
        // Field count constants for serialization.
        // This is the MINIMUM required length (indices 0~22). The potential layer (index 23) is an
        // optional trailing field, so this constant is intentionally NOT bumped when it is present.
        private const int EQUIPMENT_FIELD_COUNT = ITEM_USABLE_FIELD_COUNT + 12; // base + equipped, level, stat, setId, spineResourcePath, iconId, byCustomCraft, craftWithRandom, hasRandomOnlyIcon, optionCountFromCombination, madeWithMimisbrunnrRecipe, exp

        /// <summary>Whether the equipment is currently equipped.</summary>
        // FIXME: Whether the equipment is equipped or not has no asset value and must be removed from the state.
        public bool equipped;

        /// <summary>The enhancement level of the equipment.</summary>
        public int level;

        /// <summary>The accumulated experience points used for enhancement.</summary>
        public long Exp;

        /// <summary>The number of options granted from combination (crafting).</summary>
        public int optionCountFromCombination;

        /// <summary>The icon identifier used for UI display.</summary>
        public int IconId;

        /// <summary>Whether the equipment was crafted through custom crafting.</summary>
        public bool ByCustomCraft;

        /// <summary>Whether random options were applied during crafting.</summary>
        public bool CraftWithRandom;

        /// <summary>Whether the equipment has a random-only icon.</summary>
        public bool HasRandomOnlyIcon;

        /// <summary>The primary stat of the equipment.</summary>
        public DecimalStat Stat { get; private set; }

        /// <summary>The identifier used for equipment set bonuses.</summary>
        public int SetId { get; private set; }

        /// <summary>The path to the spine animation resource.</summary>
        public string SpineResourcePath { get; private set; }

        /// <summary>Whether the equipment was made with a Mimisbrunnr recipe.</summary>
        public bool MadeWithMimisbrunnrRecipe { get; set; }

        /// <summary>
        /// The latent ("potential") option layer attached to this equipment.
        /// Independent of <see cref="StatsMap"/> and <see cref="Skills"/>; defaults to
        /// <see cref="EquipmentPotential.Empty"/> for equipment that has never been granted options.
        /// </summary>
        public EquipmentPotential Potential { get; private set; } = EquipmentPotential.Empty;

        /// <summary>The stat type of the equipment's primary stat.</summary>
        public StatType UniqueStatType => Stat.StatType;

        /// <summary>Whether the equipment is currently equipped.</summary>
        public bool Equipped => equipped;

        /// <summary>
        /// Gets the stat increment amount applied per enhancement level.
        /// </summary>
        /// <returns>The increment amount, at least 1.0.</returns>
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

            // Legacy Dictionary-format equipment predates the potential layer.
            Potential = EquipmentPotential.Empty;
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

            // potential (index 23) — optional trailing field.
            // Equipment serialized before the potential layer was introduced stops at index 22,
            // so read it only when present to stay backward compatible.
            Potential = list.Count > 23
                ? new EquipmentPotential(list[23])
                : EquipmentPotential.Empty;
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
                .Add(Exp)
                .Add(Potential.Serialize());

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
                "exp",
                "potential"
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

        /// <summary>
        /// Replaces the latent ("potential") option layer of this equipment.
        /// Passing <c>null</c> resets it to <see cref="EquipmentPotential.Empty"/>.
        /// </summary>
        /// <param name="potential">The new potential layer.</param>
        public void SetPotential(EquipmentPotential potential)
        {
            Potential = potential ?? EquipmentPotential.Empty;
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
                var chance = NumberConversionHelper.SafeDecimalToInt32(skill.Chance * 1.3m);
                var power = NumberConversionHelper.SafeDecimalToInt32(skill.Power * 1.3m);
                var statPowerRatio = NumberConversionHelper.SafeDecimalToInt32(skill.StatPowerRatio * 1.3m);
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

                var chance = skill.Chance + NumberConversionHelper.SafeDecimalToInt32(addChance);
                var power = skill.Power + NumberConversionHelper.SafeDecimalToInt32(addPower);
                var statPowerRatio = skill.StatPowerRatio + NumberConversionHelper.SafeDecimalToInt32(addStatPowerRatio);

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

                var chance = skill.Chance + NumberConversionHelper.SafeDecimalToInt32(addChance);
                var power = skill.Power + NumberConversionHelper.SafeDecimalToInt32(addPower);
                var statPowerRatio = skill.StatPowerRatio + NumberConversionHelper.SafeDecimalToInt32(addStatPowerRatio);

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
