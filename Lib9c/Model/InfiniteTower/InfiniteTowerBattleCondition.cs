using System;
using System.Collections.Generic;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.InfiniteTower
{
    /// <summary>
    /// Represents different types of battle conditions that can be applied to infinite tower floors.
    /// </summary>
    public enum BattleConditionType
    {
        /// <summary>
        /// Combat Power restrictions (RequiredCp, MaxCp)
        /// </summary>
        CP,

        /// <summary>
        /// Item Grade restrictions (MinItemGrade, MaxItemGrade)
        /// </summary>
        ItemGrade,

        /// <summary>
        /// Item Level restrictions (MinItemLevel, MaxItemLevel)
        /// </summary>
        ItemLevel,

        /// <summary>
        /// Forbidden Rune Types restrictions
        /// </summary>
        ForbiddenRuneTypes,

        /// <summary>
        /// Required Elemental Type restrictions
        /// </summary>
        RequiredElementalType,

        /// <summary>
        /// Forbidden Item Sub Types restrictions
        /// </summary>
        ForbiddenItemSubTypes
    }

    /// <summary>
    /// Infinite tower battle condition for managing floor-specific restrictions and requirements.
    /// Represents various types of battle conditions that can be applied to infinite tower floors.
    /// </summary>
    [Serializable]
    public class InfiniteTowerBattleCondition
    {
        /// <summary>
        /// Gets the type of this battle condition.
        /// </summary>
        public BattleConditionType Type { get; }

        /// <summary>
        /// Gets the minimum required combat power (for CP type).
        /// </summary>
        public long? RequiredCp { get; }

        /// <summary>
        /// Gets the maximum allowed combat power (for CP type).
        /// </summary>
        public long? MaxCp { get; }

        /// <summary>
        /// Gets the minimum item grade (for ItemGrade type).
        /// </summary>
        public int? MinItemGrade { get; }

        /// <summary>
        /// Gets the maximum item grade (for ItemGrade type).
        /// </summary>
        public int? MaxItemGrade { get; }

        /// <summary>
        /// Gets the minimum item level (for ItemLevel type).
        /// </summary>
        public int? MinItemLevel { get; }

        /// <summary>
        /// Gets the maximum item level (for ItemLevel type).
        /// </summary>
        public int? MaxItemLevel { get; }

        /// <summary>
        /// Gets the list of forbidden rune types (for ForbiddenRuneTypes type).
        /// </summary>
        public List<RuneType> ForbiddenRuneTypes { get; }

        /// <summary>
        /// Gets the list of required elemental types (for RequiredElementalType type).
        /// </summary>
        public List<ElementalType> RequiredElementalTypes { get; }

        /// <summary>
        /// Gets the list of forbidden item sub types (for ForbiddenItemSubTypes type).
        /// </summary>
        public List<ItemSubType> ForbiddenItemSubTypes { get; }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for CP restrictions.
        /// </summary>
        /// <param name="requiredCp">Minimum required combat power</param>
        /// <param name="maxCp">Maximum allowed combat power</param>
        public InfiniteTowerBattleCondition(long? requiredCp, long? maxCp)
        {
            Type = BattleConditionType.CP;
            RequiredCp = requiredCp;
            MaxCp = maxCp;
            MinItemGrade = null;
            MaxItemGrade = null;
            MinItemLevel = null;
            MaxItemLevel = null;
            ForbiddenRuneTypes = null;
            RequiredElementalTypes = null;
            ForbiddenItemSubTypes = null;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for Item Grade restrictions.
        /// </summary>
        /// <param name="minItemGrade">Minimum item grade</param>
        /// <param name="maxItemGrade">Maximum item grade</param>
        /// <param name="isItemGrade">Must be true to indicate this is for item grade</param>
        public InfiniteTowerBattleCondition(int? minItemGrade, int? maxItemGrade, bool isItemGrade)
        {
            if (!isItemGrade)
            {
                throw new ArgumentException("isItemGrade must be true for this constructor");
            }

            Type = BattleConditionType.ItemGrade;
            RequiredCp = null;
            MaxCp = null;
            MinItemGrade = minItemGrade;
            MaxItemGrade = maxItemGrade;
            MinItemLevel = null;
            MaxItemLevel = null;
            ForbiddenRuneTypes = null;
            RequiredElementalTypes = null;
            ForbiddenItemSubTypes = null;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for Item Level restrictions.
        /// </summary>
        /// <param name="minItemLevel">Minimum item level</param>
        /// <param name="maxItemLevel">Maximum item level</param>
        public InfiniteTowerBattleCondition(int? minItemLevel, int? maxItemLevel)
        {
            Type = BattleConditionType.ItemLevel;
            RequiredCp = null;
            MaxCp = null;
            MinItemGrade = null;
            MaxItemGrade = null;
            MinItemLevel = minItemLevel;
            MaxItemLevel = maxItemLevel;
            ForbiddenRuneTypes = null;
            RequiredElementalTypes = null;
            ForbiddenItemSubTypes = null;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for Forbidden Rune Types.
        /// </summary>
        /// <param name="forbiddenRuneTypes">List of forbidden rune types</param>
        public InfiniteTowerBattleCondition(List<RuneType> forbiddenRuneTypes)
        {
            Type = BattleConditionType.ForbiddenRuneTypes;
            RequiredCp = null;
            MaxCp = null;
            MinItemGrade = null;
            MaxItemGrade = null;
            MinItemLevel = null;
            MaxItemLevel = null;
            ForbiddenRuneTypes = forbiddenRuneTypes ?? new List<RuneType>();
            RequiredElementalTypes = null;
            ForbiddenItemSubTypes = null;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for Required Elemental Type.
        /// </summary>
        /// <param name="requiredElementalTypes">List of required elemental types</param>
        /// <param name="isElementalType">Must be true to indicate this is for elemental type</param>
        public InfiniteTowerBattleCondition(List<ElementalType> requiredElementalTypes, bool isElementalType)
        {
            if (!isElementalType)
            {
                throw new ArgumentException("isElementalType must be true for this constructor");
            }

            Type = BattleConditionType.RequiredElementalType;
            RequiredCp = null;
            MaxCp = null;
            MinItemGrade = null;
            MaxItemGrade = null;
            MinItemLevel = null;
            MaxItemLevel = null;
            ForbiddenRuneTypes = null;
            RequiredElementalTypes = requiredElementalTypes ?? new List<ElementalType>();
            ForbiddenItemSubTypes = null;
        }

        /// <summary>
        /// Initializes a new instance of the InfiniteTowerBattleCondition class for Forbidden Item Sub Types.
        /// </summary>
        /// <param name="forbiddenItemSubTypes">List of forbidden item sub types</param>
        /// <param name="isItemSubTypes">Must be true to indicate this is for item sub types</param>
        public InfiniteTowerBattleCondition(List<ItemSubType> forbiddenItemSubTypes, bool isItemSubTypes)
        {
            if (!isItemSubTypes)
            {
                throw new ArgumentException("isItemSubTypes must be true for this constructor");
            }

            Type = BattleConditionType.ForbiddenItemSubTypes;
            RequiredCp = null;
            MaxCp = null;
            MinItemGrade = null;
            MaxItemGrade = null;
            MinItemLevel = null;
            MaxItemLevel = null;
            ForbiddenRuneTypes = null;
            RequiredElementalTypes = null;
            ForbiddenItemSubTypes = forbiddenItemSubTypes ?? new List<ItemSubType>();
        }

        /// <summary>
        /// Checks if this battle condition has any restrictions set.
        /// </summary>
        /// <returns>True if the condition has restrictions, false otherwise</returns>
        public bool HasRestrictions()
        {
            return Type switch
            {
                BattleConditionType.CP => RequiredCp.HasValue || MaxCp.HasValue,
                BattleConditionType.ItemGrade => MinItemGrade.HasValue || MaxItemGrade.HasValue,
                BattleConditionType.ItemLevel => MinItemLevel.HasValue || MaxItemLevel.HasValue,
                BattleConditionType.ForbiddenRuneTypes => ForbiddenRuneTypes?.Count > 0,
                BattleConditionType.RequiredElementalType => RequiredElementalTypes?.Count > 0,
                BattleConditionType.ForbiddenItemSubTypes => ForbiddenItemSubTypes?.Count > 0,
                _ => false
            };
        }

        /// <summary>
        /// Gets a description of this battle condition.
        /// </summary>
        /// <returns>String description of the condition</returns>
        public override string ToString()
        {
            return Type switch
            {
                BattleConditionType.CP => $"CP: Required={RequiredCp}, Max={MaxCp}",
                BattleConditionType.ItemGrade => $"ItemGrade: Min={MinItemGrade}, Max={MaxItemGrade}",
                BattleConditionType.ItemLevel => $"ItemLevel: Min={MinItemLevel}, Max={MaxItemLevel}",
                BattleConditionType.ForbiddenRuneTypes => $"ForbiddenRuneTypes: {string.Join(", ", ForbiddenRuneTypes ?? new List<RuneType>())}",
                BattleConditionType.RequiredElementalType => $"RequiredElementalTypes: [{string.Join(", ", RequiredElementalTypes ?? new List<ElementalType>())}]",
                BattleConditionType.ForbiddenItemSubTypes => $"ForbiddenItemSubTypes: {string.Join(", ", ForbiddenItemSubTypes ?? new List<ItemSubType>())}",
                _ => "Unknown"
            };
        }
    }
}
