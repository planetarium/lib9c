#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.Model.Rune;
using Nekoyume.TableData.Rune;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Infinite tower floor sheet for managing tower floors and their properties.
    /// </summary>
    [Serializable]
    public class InfiniteTowerFloorSheet : Sheet<int, InfiniteTowerFloorSheet.Row>
    {
        public InfiniteTowerFloorSheet() : base(nameof(InfiniteTowerFloorSheet))
        {
        }
        /// <summary>
        /// Represents a row in the InfiniteTowerFloorSheet containing floor data.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the floor ID as the key for this row.
            /// </summary>
            public override int Key => Id;

            /// <summary>
            /// Gets the unique identifier for this floor.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// Gets the floor number.
            /// </summary>
            public int Floor { get; private set; }

            /// <summary>
            /// Gets the minimum required combat power for this floor.
            /// </summary>
            public long? RequiredCp { get; private set; }

            /// <summary>
            /// Gets the maximum allowed combat power for this floor.
            /// </summary>
            public long? MaxCp { get; private set; }

            /// <summary>
            /// Gets the list of forbidden item sub-types for this floor.
            /// </summary>
            public List<ItemSubType> ForbiddenItemSubTypes { get; private set; } = new();

            /// <summary>
            /// Gets the minimum item grade allowed for this floor.
            /// </summary>
            public int? MinItemGrade { get; private set; }

            /// <summary>
            /// Gets the maximum item grade allowed for this floor.
            /// </summary>
            public int? MaxItemGrade { get; private set; }

            /// <summary>
            /// Gets the minimum item level allowed for this floor.
            /// </summary>
            public int? MinItemLevel { get; private set; }

            /// <summary>
            /// Gets the maximum item level allowed for this floor.
            /// </summary>
            public int? MaxItemLevel { get; private set; }

            /// <summary>
            /// Gets the guaranteed condition ID that will always be applied to this floor.
            /// </summary>
            public int GuaranteedConditionId { get; private set; }

            /// <summary>
            /// Gets the minimum number of random conditions to apply.
            /// </summary>
            public int MinRandomConditions { get; private set; }

            /// <summary>
            /// Gets the maximum number of random conditions to apply.
            /// </summary>
            public int MaxRandomConditions { get; private set; }

            /// <summary>
            /// Gets the first random condition ID for weighted selection.
            /// </summary>
            public int? RandomConditionId1 { get; private set; }

            /// <summary>
            /// Gets the weight for the first random condition.
            /// </summary>
            public int? RandomConditionWeight1 { get; private set; }

            /// <summary>
            /// Gets the second random condition ID for weighted selection.
            /// </summary>
            public int? RandomConditionId2 { get; private set; }

            /// <summary>
            /// Gets the weight for the second random condition.
            /// </summary>
            public int? RandomConditionWeight2 { get; private set; }

            /// <summary>
            /// Gets the third random condition ID for weighted selection.
            /// </summary>
            public int? RandomConditionId3 { get; private set; }

            /// <summary>
            /// Gets the weight for the third random condition.
            /// </summary>
            public int? RandomConditionWeight3 { get; private set; }

            /// <summary>
            /// Gets the fourth random condition ID for weighted selection.
            /// </summary>
            public int? RandomConditionId4 { get; private set; }

            /// <summary>
            /// Gets the weight for the fourth random condition.
            /// </summary>
            public int? RandomConditionWeight4 { get; private set; }

            /// <summary>
            /// Gets the fifth random condition ID for weighted selection.
            /// </summary>
            public int? RandomConditionId5 { get; private set; }

            /// <summary>
            /// Gets the weight for the fifth random condition.
            /// </summary>
            public int? RandomConditionWeight5 { get; private set; }

            /// <summary>
            /// Gets the NCG cost for purchasing tickets for this floor.
            /// </summary>
            public int? NcgCost { get; private set; }

            /// <summary>
            /// Gets the material item ID required for purchasing tickets.
            /// </summary>
            public int? MaterialCostId { get; private set; }

            /// <summary>
            /// Gets the amount of material required for purchasing tickets.
            /// </summary>
            public int? MaterialCostCount { get; private set; }


            /// <summary>
            /// Gets the list of forbidden rune types for this floor.
            /// </summary>
            public List<RuneType> ForbiddenRuneTypes { get; private set; } = new();

            /// <summary>
            /// Gets the list of required elemental types for equipment on this floor.
            /// </summary>
            public List<ElementalType> RequiredElementalTypes { get; private set; } = new();

            /// <summary>
            /// Gets the first item reward ID for this floor.
            /// </summary>
            public int? ItemRewardId1 { get; private set; }

            /// <summary>
            /// Gets the count of the first item reward.
            /// </summary>
            public int? ItemRewardCount1 { get; private set; }

            /// <summary>
            /// Gets the second item reward ID for this floor.
            /// </summary>
            public int? ItemRewardId2 { get; private set; }

            /// <summary>
            /// Gets the count of the second item reward.
            /// </summary>
            public int? ItemRewardCount2 { get; private set; }

            /// <summary>
            /// Gets the third item reward ID for this floor.
            /// </summary>
            public int? ItemRewardId3 { get; private set; }

            /// <summary>
            /// Gets the count of the third item reward.
            /// </summary>
            public int? ItemRewardCount3 { get; private set; }

            /// <summary>
            /// Gets the fourth item reward ID for this floor.
            /// </summary>
            public int? ItemRewardId4 { get; private set; }

            /// <summary>
            /// Gets the count of the fourth item reward.
            /// </summary>
            public int? ItemRewardCount4 { get; private set; }

            /// <summary>
            /// Gets the fifth item reward ID for this floor.
            /// </summary>
            public int? ItemRewardId5 { get; private set; }

            /// <summary>
            /// Gets the count of the fifth item reward.
            /// </summary>
            public int? ItemRewardCount5 { get; private set; }

            /// <summary>
            /// Gets the first fungible asset reward ticker for this floor.
            /// </summary>
            public string? FungibleAssetRewardTicker1 { get; private set; }

            /// <summary>
            /// Gets the amount of the first fungible asset reward.
            /// </summary>
            public int? FungibleAssetRewardAmount1 { get; private set; }

            /// <summary>
            /// Gets the second fungible asset reward ticker for this floor.
            /// </summary>
            public string? FungibleAssetRewardTicker2 { get; private set; }

            /// <summary>
            /// Gets the amount of the second fungible asset reward.
            /// </summary>
            public int? FungibleAssetRewardAmount2 { get; private set; }

            /// <summary>
            /// Gets the third fungible asset reward ticker for this floor.
            /// </summary>
            public string? FungibleAssetRewardTicker3 { get; private set; }

            /// <summary>
            /// Gets the amount of the third fungible asset reward.
            /// </summary>
            public int? FungibleAssetRewardAmount3 { get; private set; }

            /// <summary>
            /// Gets the fourth fungible asset reward ticker for this floor.
            /// </summary>
            public string? FungibleAssetRewardTicker4 { get; private set; }

            /// <summary>
            /// Gets the amount of the fourth fungible asset reward.
            /// </summary>
            public int? FungibleAssetRewardAmount4 { get; private set; }

            /// <summary>
            /// Gets the fifth fungible asset reward ticker for this floor.
            /// </summary>
            public string? FungibleAssetRewardTicker5 { get; private set; }

            /// <summary>
            /// Gets the amount of the fifth fungible asset reward.
            /// </summary>
            public int? FungibleAssetRewardAmount5 { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Floor = ParseInt(fields[1]);
                RequiredCp = string.IsNullOrEmpty(fields[2]) ? null : ParseLong(fields[2]);
                MaxCp = string.IsNullOrEmpty(fields[3]) ? null : ParseLong(fields[3]);

                // Parse item sub-type restrictions
                ForbiddenItemSubTypes = string.IsNullOrEmpty(fields[4]) ? new List<ItemSubType>() : ParseItemSubTypes(fields[4]);

                // Parse item grade restrictions
                MinItemGrade = string.IsNullOrEmpty(fields[5]) ? null : ParseInt(fields[5]);
                MaxItemGrade = string.IsNullOrEmpty(fields[6]) ? null : ParseInt(fields[6]);

                // Parse item level restrictions
                MinItemLevel = string.IsNullOrEmpty(fields[7]) ? null : ParseInt(fields[7]);
                MaxItemLevel = string.IsNullOrEmpty(fields[8]) ? null : ParseInt(fields[8]);

                GuaranteedConditionId = ParseInt(fields[9]);
                MinRandomConditions = ParseInt(fields[10]);
                MaxRandomConditions = ParseInt(fields[11]);

                // Parse weighted random conditions (fields 12-21)
                RandomConditionId1 = ParseIntOrNull(fields[12]);
                RandomConditionWeight1 = ParseIntOrNull(fields[13]);
                RandomConditionId2 = ParseIntOrNull(fields[14]);
                RandomConditionWeight2 = ParseIntOrNull(fields[15]);
                RandomConditionId3 = ParseIntOrNull(fields[16]);
                RandomConditionWeight3 = ParseIntOrNull(fields[17]);
                RandomConditionId4 = ParseIntOrNull(fields[18]);
                RandomConditionWeight4 = ParseIntOrNull(fields[19]);
                RandomConditionId5 = ParseIntOrNull(fields[20]);
                RandomConditionWeight5 = ParseIntOrNull(fields[21]);

                // Parse reward fields - Item rewards (max 5 types)
                ItemRewardId1 = ParseIntOrNull(fields[22]);
                ItemRewardCount1 = ParseIntOrNull(fields[23]);
                ItemRewardId2 = ParseIntOrNull(fields[24]);
                ItemRewardCount2 = ParseIntOrNull(fields[25]);
                ItemRewardId3 = ParseIntOrNull(fields[26]);
                ItemRewardCount3 = ParseIntOrNull(fields[27]);
                ItemRewardId4 = ParseIntOrNull(fields[28]);
                ItemRewardCount4 = ParseIntOrNull(fields[29]);
                ItemRewardId5 = ParseIntOrNull(fields[30]);
                ItemRewardCount5 = ParseIntOrNull(fields[31]);

                // Parse reward fields - Fungible asset rewards (max 5 types)
                FungibleAssetRewardTicker1 = ParseStringOrNull(fields[32]);
                FungibleAssetRewardAmount1 = ParseIntOrNull(fields[33]);
                FungibleAssetRewardTicker2 = ParseStringOrNull(fields[34]);
                FungibleAssetRewardAmount2 = ParseIntOrNull(fields[35]);
                FungibleAssetRewardTicker3 = ParseStringOrNull(fields[36]);
                FungibleAssetRewardAmount3 = ParseIntOrNull(fields[37]);
                FungibleAssetRewardTicker4 = ParseStringOrNull(fields[38]);
                FungibleAssetRewardAmount4 = ParseIntOrNull(fields[39]);
                FungibleAssetRewardTicker5 = ParseStringOrNull(fields[40]);
                FungibleAssetRewardAmount5 = ParseIntOrNull(fields[41]);

                // Parse ticket purchase cost fields
                NcgCost = ParseIntOrNull(fields[42]);
                MaterialCostId = ParseIntOrNull(fields[43]);
                MaterialCostCount = ParseIntOrNull(fields[44]);

                // Parse rune restrictions and elemental type (optional trailing columns)
                if (fields.Count > 45)
                {
                    ForbiddenRuneTypes = string.IsNullOrEmpty(fields[45]) ? new List<RuneType>() : ParseRuneTypes(fields[45]);
                }

                if (fields.Count > 46)
                {
                    RequiredElementalTypes = string.IsNullOrEmpty(fields[46]) ? new List<ElementalType>() : ParseElementalTypes(fields[46]);
                }
            }


            /// <summary>
            /// Gets all item rewards as a list of tuples (itemId, count).
            /// </summary>
            public List<(int itemId, int count)> GetItemRewards()
            {
                var rewards = new List<(int, int)>();

                if (ItemRewardId1.HasValue && ItemRewardCount1.HasValue)
                    rewards.Add((ItemRewardId1.Value, ItemRewardCount1.Value));
                if (ItemRewardId2.HasValue && ItemRewardCount2.HasValue)
                    rewards.Add((ItemRewardId2.Value, ItemRewardCount2.Value));
                if (ItemRewardId3.HasValue && ItemRewardCount3.HasValue)
                    rewards.Add((ItemRewardId3.Value, ItemRewardCount3.Value));
                if (ItemRewardId4.HasValue && ItemRewardCount4.HasValue)
                    rewards.Add((ItemRewardId4.Value, ItemRewardCount4.Value));
                if (ItemRewardId5.HasValue && ItemRewardCount5.HasValue)
                    rewards.Add((ItemRewardId5.Value, ItemRewardCount5.Value));

                return rewards;
            }

            /// <summary>
            /// Gets all fungible asset rewards as a list of tuples (ticker, amount).
            /// </summary>
            public List<(string ticker, int amount)> GetFungibleAssetRewards()
            {
                var rewards = new List<(string, int)>();

                if (!string.IsNullOrEmpty(FungibleAssetRewardTicker1) && FungibleAssetRewardAmount1.HasValue)
                    rewards.Add((FungibleAssetRewardTicker1, FungibleAssetRewardAmount1.Value));
                if (!string.IsNullOrEmpty(FungibleAssetRewardTicker2) && FungibleAssetRewardAmount2.HasValue)
                    rewards.Add((FungibleAssetRewardTicker2, FungibleAssetRewardAmount2.Value));
                if (!string.IsNullOrEmpty(FungibleAssetRewardTicker3) && FungibleAssetRewardAmount3.HasValue)
                    rewards.Add((FungibleAssetRewardTicker3, FungibleAssetRewardAmount3.Value));
                if (!string.IsNullOrEmpty(FungibleAssetRewardTicker4) && FungibleAssetRewardAmount4.HasValue)
                    rewards.Add((FungibleAssetRewardTicker4, FungibleAssetRewardAmount4.Value));
                if (!string.IsNullOrEmpty(FungibleAssetRewardTicker5) && FungibleAssetRewardAmount5.HasValue)
                    rewards.Add((FungibleAssetRewardTicker5, FungibleAssetRewardAmount5.Value));

                return rewards;
            }

            /// <summary>
            /// Validates item sub-type restrictions for the floor (applies to both equipment and costumes).
            /// </summary>
            public void ValidateItemTypeRestrictions<T>(List<T> itemList) where T : ItemBase
            {
                // Skip validation if no sub-type restrictions are set
                if (ForbiddenItemSubTypes.Count == 0)
                    return;

                foreach (var item in itemList)
                {
                    // Check forbidden sub-types
                    if (ForbiddenItemSubTypes.Contains(item.ItemSubType))
                    {
                        throw new Exception($"Invalid item sub-type. Item sub-type '{item.ItemSubType}' is forbidden. Forbidden sub-types: {string.Join(", ", ForbiddenItemSubTypes)}");
                    }
                }
            }

            /// <summary>
            /// Validates item grade restrictions for the floor (applies to both equipment and costumes).
            /// </summary>
            public void ValidateItemGradeRestrictions<T>(List<T> itemList) where T : ItemBase
            {
                // Skip validation if no grade restrictions are set
                if (!MinItemGrade.HasValue && !MaxItemGrade.HasValue)
                    return;

                foreach (var item in itemList)
                {
                    // Check minimum grade
                    if (MinItemGrade.HasValue && item.Grade < MinItemGrade.Value)
                    {
                        throw new Exception($"Invalid item grade. Item grade '{item.Grade}' is below minimum requirement. Minimum grade required: {MinItemGrade.Value}");
                    }

                    // Check maximum grade
                    if (MaxItemGrade.HasValue && item.Grade > MaxItemGrade.Value)
                    {
                        throw new Exception($"Invalid item grade. Item grade '{item.Grade}' exceeds maximum limit. Maximum grade allowed: {MaxItemGrade.Value}");
                    }
                }
            }

            /// <summary>
            /// Validates item level restrictions for the floor (applies to both equipment and costumes).
            /// </summary>
            public void ValidateItemLevelRestrictions<T>(List<T> itemList) where T : ItemBase
            {
                // Skip validation if no level restrictions are set
                if (!MinItemLevel.HasValue && !MaxItemLevel.HasValue)
                    return;

                foreach (var item in itemList)
                {
                    int itemLevel = 0;

                    // Get level based on item type
                    if (item is Equipment equipment)
                    {
                        itemLevel = equipment.level;
                    }
                    else if (item is Costume)
                    {
                        // Costumes don't have level, skip level validation for costumes
                        continue;
                    }

                    // Check minimum level
                    if (MinItemLevel.HasValue && itemLevel < MinItemLevel.Value)
                    {
                        throw new Exception($"Invalid item level. Item level '{itemLevel}' is below minimum requirement. Minimum level required: {MinItemLevel.Value}");
                    }

                    // Check maximum level
                    if (MaxItemLevel.HasValue && itemLevel > MaxItemLevel.Value)
                    {
                        throw new Exception($"Invalid item level. Item level '{itemLevel}' exceeds maximum limit. Maximum level allowed: {MaxItemLevel.Value}");
                    }
                }
            }

            /// <summary>
            /// Validates CP requirements (minimum and maximum) for the floor.
            /// </summary>
            public void ValidateCpRequirements(long currentCp)
            {
                // Skip CP validation if no requirements are set
                if (RequiredCp == null && MaxCp == null)
                    return;

                // Check minimum CP requirement
                if (RequiredCp.HasValue && currentCp < RequiredCp.Value)
                {
                    throw new Exception($"Insufficient combat power. Current CP '{currentCp}' is below minimum requirement. Minimum CP required: {RequiredCp.Value}");
                }

                // Check maximum CP limit
                if (MaxCp.HasValue && currentCp > MaxCp.Value)
                {
                    throw new Exception($"Excessive combat power. Current CP '{currentCp}' exceeds maximum limit. Maximum CP allowed: {MaxCp.Value}");
                }
            }

            /// <summary>
            /// Gets random conditions with weights for weighted selection.
            /// </summary>
            public List<(int conditionId, int weight)> GetRandomConditionsWithWeights()
            {
                var conditions = new List<(int, int)>();

                if (RandomConditionId1.HasValue && RandomConditionWeight1.HasValue)
                    conditions.Add((RandomConditionId1.Value, RandomConditionWeight1.Value));
                if (RandomConditionId2.HasValue && RandomConditionWeight2.HasValue)
                    conditions.Add((RandomConditionId2.Value, RandomConditionWeight2.Value));
                if (RandomConditionId3.HasValue && RandomConditionWeight3.HasValue)
                    conditions.Add((RandomConditionId3.Value, RandomConditionWeight3.Value));
                if (RandomConditionId4.HasValue && RandomConditionWeight4.HasValue)
                    conditions.Add((RandomConditionId4.Value, RandomConditionWeight4.Value));
                if (RandomConditionId5.HasValue && RandomConditionWeight5.HasValue)
                    conditions.Add((RandomConditionId5.Value, RandomConditionWeight5.Value));

                return conditions;
            }

            /// <summary>
            /// Validates equipment elemental type restrictions for the floor.
            /// </summary>
            public void ValidateEquipmentElementalType<T>(List<T> equipmentList) where T : ItemBase
            {
                // Skip validation if no elemental type restriction is set
                if (RequiredElementalTypes == null || RequiredElementalTypes.Count == 0)
                    return;

                foreach (var equipment in equipmentList)
                {
                    if (!RequiredElementalTypes.Contains(equipment.ElementalType))
                    {
                        throw new Exception($"Invalid equipment elemental type. Equipment '{equipment.Id}' has elemental type '{equipment.ElementalType}' but required types are: {string.Join(", ", RequiredElementalTypes)}");
                    }
                }
            }

            /// <summary>
            /// Selects random conditions using weighted selection for this floor.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <param name="conditionSheet">The condition sheet containing all available conditions</param>
            /// <param name="random">Random number generator</param>
            /// <param name="guaranteedConditionId">Optional guaranteed condition ID to exclude from random selection</param>
            /// <returns>List of selected random conditions</returns>
            public List<InfiniteTowerCondition> GetRandomConditionsWithWeights(
                InfiniteTowerConditionSheet conditionSheet,
                Libplanet.Action.IRandom random,
                int? guaranteedConditionId = null)
            {
                var weightedConditions = GetRandomConditionsWithWeights();
                if (!weightedConditions.Any())
                {
                    return new List<InfiniteTowerCondition>();
                }

                // Create condition lookup for quick access
                var conditionLookup = conditionSheet.Values.ToDictionary(c => c.Id, c => c);

                // Filter out guaranteed condition and validate condition IDs
                var availableWeightedConditions = weightedConditions
                    .Where(wc => conditionLookup.ContainsKey(wc.conditionId))
                    .Where(wc => guaranteedConditionId == null || wc.conditionId != guaranteedConditionId)
                    .ToList();

                if (!availableWeightedConditions.Any())
                {
                    return new List<InfiniteTowerCondition>();
                }

                var count = random.Next(MinRandomConditions, MaxRandomConditions + 1);
                var selectedConditions = new List<InfiniteTowerCondition>();

                // Use WeightedSelector for weighted random selection
                var selector = new WeightedSelector<int>(random);
                foreach (var (conditionId, weight) in availableWeightedConditions)
                {
                    selector.Add(conditionId, weight);
                }

                var selectedConditionIds = selector.Select(count).ToList();
                foreach (var conditionId in selectedConditionIds)
                {
                    if (conditionLookup.TryGetValue(conditionId, out var conditionRow))
                    {
                        selectedConditions.Add(new InfiniteTowerCondition(conditionRow));
                    }
                }

                return selectedConditions;
            }

            /// <summary>
            /// Validates floor-specific restrictions for equipment and costumes.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <param name="equipmentList">List of equipment to validate</param>
            /// <param name="costumeList">List of costumes to validate</param>
            /// <exception cref="Exception">Thrown when equipment or costume restrictions are violated</exception>
            public void ValidateFloorRestrictions(
                List<Equipment> equipmentList,
                List<Costume> costumeList)
            {
                // Validate equipment restrictions
                ValidateItemTypeRestrictions(equipmentList);
                ValidateItemGradeRestrictions(equipmentList);
                ValidateItemLevelRestrictions(equipmentList);

                // Validate costume restrictions
                ValidateItemTypeRestrictions(costumeList);
                ValidateItemGradeRestrictions(costumeList);
                ValidateItemLevelRestrictions(costumeList);
            }

            /// <summary>
            /// Validates equipped rune types against the floor's forbidden list.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <param name="runeInfos">List of rune slot information</param>
            /// <param name="runeListSheet">Rune list sheet for type lookup</param>
            /// <exception cref="ForbiddenRuneTypeEquippedException">Thrown when forbidden rune types are equipped</exception>
            public void ValidateRuneTypes(
                List<RuneSlotInfo> runeInfos,
                RuneListSheet runeListSheet)
            {
                if (ForbiddenRuneTypes == null || ForbiddenRuneTypes.Count == 0)
                {
                    return;
                }

                var equippedRuneTypes = new List<RuneType>();
                foreach (var runeInfo in runeInfos ?? new List<RuneSlotInfo>())
                {
                    if (runeListSheet.TryGetValue(runeInfo.RuneId, out var runeRow))
                    {
                        equippedRuneTypes.Add((RuneType)runeRow.RuneType);
                    }
                }

                var blockedTypes = equippedRuneTypes.Intersect(ForbiddenRuneTypes).ToList();
                if (blockedTypes.Count > 0)
                {
                    throw new ForbiddenRuneTypeEquippedException(
                        ForbiddenRuneTypes,
                        blockedTypes);
                }
            }

            /// <summary>
            /// Validates equipment elemental types against the floor's requirements.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <param name="equipments">List of equipment to validate</param>
            /// <exception cref="InvalidElementalException">Thrown when equipment has invalid elemental type</exception>
            public void ValidateEquipmentElementalType(
                List<Equipment> equipments)
            {
                if (RequiredElementalTypes == null || RequiredElementalTypes.Count == 0)
                {
                    return;
                }

                var invalidEquipment = new List<(int equipmentId, ElementalType actualType)>();
                foreach (var equipment in equipments ?? new List<Equipment>())
                {
                    if (!RequiredElementalTypes.Contains(equipment.ElementalType))
                    {
                        invalidEquipment.Add((equipment.Id, equipment.ElementalType));
                    }
                }

                if (invalidEquipment.Count > 0)
                {
                    var invalidEquipmentInfo = string.Join(", ", invalidEquipment.Select(e => $"ID:{e.equipmentId}({e.actualType})"));
                    throw new InvalidElementalException(
                        $"Invalid equipment elemental type. Required types: [{string.Join(", ", RequiredElementalTypes)}], Invalid equipment: {invalidEquipmentInfo}");
                }
            }

            /// <summary>
            /// Creates a list of battle conditions from this floor's restrictions.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <returns>List of battle conditions representing this floor's restrictions</returns>
            public List<InfiniteTowerBattleCondition> GetBattleConditions()
            {
                var conditions = new List<InfiniteTowerBattleCondition>();

                // CP restrictions
                if (RequiredCp.HasValue || MaxCp.HasValue)
                {
                    conditions.Add(new InfiniteTowerBattleCondition(RequiredCp, MaxCp));
                }

            // Item Grade restrictions
            if (MinItemGrade.HasValue || MaxItemGrade.HasValue)
            {
                conditions.Add(new InfiniteTowerBattleCondition(MinItemGrade, MaxItemGrade, true));
            }

                // Item Level restrictions
                if (MinItemLevel.HasValue || MaxItemLevel.HasValue)
                {
                    conditions.Add(new InfiniteTowerBattleCondition(MinItemLevel, MaxItemLevel));
                }

                // Forbidden Rune Types restrictions
                if (ForbiddenRuneTypes?.Count > 0)
                {
                    conditions.Add(new InfiniteTowerBattleCondition(ForbiddenRuneTypes));
                }

                // Required Elemental Type restrictions
                if (RequiredElementalTypes?.Count > 0)
                {
                    conditions.Add(new InfiniteTowerBattleCondition(RequiredElementalTypes, true));
                }

                // Forbidden Item Sub Types restrictions
                if (ForbiddenItemSubTypes?.Count > 0)
                {
                    conditions.Add(new InfiniteTowerBattleCondition(ForbiddenItemSubTypes, true));
                }

                return conditions;
            }

            /// <summary>
            /// Gets battle conditions of a specific type from this floor's restrictions.
            /// This method can be used by clients for simulation and replay purposes.
            /// </summary>
            /// <param name="conditionType">The type of battle condition to retrieve</param>
            /// <returns>Battle condition of the specified type, or null if not found</returns>
            public InfiniteTowerBattleCondition? GetBattleCondition(BattleConditionType conditionType)
            {
                return conditionType switch
                {
                    BattleConditionType.CP when RequiredCp.HasValue || MaxCp.HasValue => new InfiniteTowerBattleCondition(RequiredCp, MaxCp),
                    BattleConditionType.ItemGrade when MinItemGrade.HasValue || MaxItemGrade.HasValue => new InfiniteTowerBattleCondition(MinItemGrade, MaxItemGrade, true),
                    BattleConditionType.ItemLevel when MinItemLevel.HasValue || MaxItemLevel.HasValue => new InfiniteTowerBattleCondition(MinItemLevel, MaxItemLevel),
                    BattleConditionType.ForbiddenRuneTypes when ForbiddenRuneTypes?.Count > 0 => new InfiniteTowerBattleCondition(ForbiddenRuneTypes),
                    BattleConditionType.RequiredElementalType when RequiredElementalTypes?.Count > 0 => new InfiniteTowerBattleCondition(RequiredElementalTypes, true),
                    BattleConditionType.ForbiddenItemSubTypes when ForbiddenItemSubTypes?.Count > 0 => new InfiniteTowerBattleCondition(ForbiddenItemSubTypes, true),
                    _ => null
                };
            }

        }
    }
}
