#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Model.EnumType;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    using Sheets = Dictionary<Type, (Address, ISheet)>;
    using GradeDict = Dictionary<int, Dictionary<ItemSubType, int>>;

    /// <summary>
    /// Synthesize action is a type of action that synthesizes items.
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Synthesize : GameAction
    {
        private const string TypeIdentifier = "synthesize";

        private const string MaterialsKey = "m";
        private const string ChargeApKey = "c";
        private const string AvatarAddressKey = "a";

        private static readonly ItemType[] ValidItemType =
        {
            ItemType.Costume,
            ItemType.Equipment,
        };

        private static readonly ItemSubType[] ValidItemSubType =
        {
            ItemSubType.FullCostume,
            ItemSubType.Title,
            ItemSubType.Grimoire,
            ItemSubType.Aura,
        };

#region Fields
        public List<Guid> MaterialIds = new();
        public bool ChargeAp;
        public Address AvatarAddress;

        private ItemSubType? _cachedItemSubType;
#endregion Fields

        /// <summary>
        /// Execute Synthesize action, Input <see cref="MaterialIds"/> and <see cref="AvatarAddress"/>.
        /// Depending on the probability, you can get different items.
        /// Success: Return new AvatarState with synthesized item in inventory.
        /// </summary>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            // Collect addresses
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            // Validate avatar
            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the agent state of the signer was failed to load."
                );
            }

            var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Use Sheets
            Sheets sheets = context.PreviousState.GetSheets(sheetTypes: new[]
            {
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(SynthesizeSheet),
                typeof(SynthesizeWeightSheet),
            });

            // Initialize variables
            var materialEquipments = new List<Equipment>();
            var materialCostumes = new List<Costume>();
            var gradeDict = new GradeDict();

            // Process materials
            foreach (var materialId in MaterialIds)
            {
                var materialEquipment = GetEquipmentFromId(materialId, avatarState, context, addressesHex);
                var materialCostume = GetCostumeFromId(materialId, avatarState, addressesHex);
                if (materialEquipment == null && materialCostume == null)
                {
                    throw new InvalidMaterialException(
                        $"{addressesHex} Aborted as the material item is not a valid item type."
                    );
                }

                if (materialEquipment != null)
                {
                    materialEquipments.Add(materialEquipment);
                    SetGradeDict(ref gradeDict, materialEquipment.Grade, materialEquipment.ItemSubType);
                }

                if (materialCostume != null)
                {
                    materialCostumes.Add(materialCostume);
                    SetGradeDict(ref gradeDict, materialCostume.Grade, materialCostume.ItemSubType);
                }
            }

            if (_cachedItemSubType == null)
            {
                throw new InvalidOperationException("ItemSubType is not set.");
            }

            // Unequip items (if necessary)
            foreach (var materialEquipment in materialEquipments)
            {
                materialEquipment.Unequip();
            }
            foreach (var materialCostume in materialCostumes)
            {
                materialCostume.Unequip();
            }

            // Remove materials from inventory
            foreach (var materialId in MaterialIds)
            {
                if (!avatarState.inventory.RemoveNonFungibleItem(materialId))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex} Aborted as the material item ({materialId}) does not exist in inventory."
                    );
                }
            }

            var synthesizeSheet = sheets.GetSheet<SynthesizeSheet>();
            var random = context.GetRandom();
            var synthesizedItems = new List<ItemBase>();

            // Calculate the number of items to be synthesized based on materials
            foreach (var gradeItem in gradeDict)
            {
                var gradeId = gradeItem.Key;
                var subTypeDict = gradeItem.Value;

                foreach (var subTypeItem in subTypeDict)
                {
                    var itemSubType = subTypeItem.Key;
                    var materialCount = subTypeItem.Value;

                    if (!synthesizeSheet.TryGetValue(gradeId, out var synthesizeRow))
                    {
                        throw new SheetRowNotFoundException(
                            $"Aborted as the synthesize row for grade ({gradeId}) was failed to load in {nameof(SynthesizeSheet)}", gradeId
                        );
                    }

                    // TODO: subType별로 필요한 아이템 개수가 다를 수 있음
                    var requiredCount = synthesizeRow.RequiredCount;
                    var succeedRate = Math.Clamp(synthesizeRow.SucceedRate, 0, 1);
                    var succeedRatePercentage = (int)(succeedRate * 100);

                    var synthesizeCount = materialCount / requiredCount;
                    var remainder = materialCount % requiredCount;

                    if (synthesizeCount <= 0 || remainder > 0)
                    {
                        throw new NotEnoughMaterialException(
                            $"{addressesHex} Aborted as the number of materials for grade {gradeId} and subtype {itemSubType} is not enough."
                        );
                    }

                    // Calculate success for each synthesis
                    for (var i = 0; i < synthesizeCount; i++)
                    {
                        var isSuccess = random.Next(100) < succeedRatePercentage;

                        var grade = (Grade)gradeId;
                        var outputGradeId = isSuccess ? GetTargetGrade(grade) : grade;

                        // Decide the item to add to inventory based on SynthesizeWeightSheet
                        var synthesizedItem = GetSynthesizedItem(outputGradeId, sheets, random, itemSubType);
                        synthesizedItems.Add(synthesizedItem);
                    }
                }
            }

            // Add synthesized items to inventory
            foreach (var item in synthesizedItems)
            {
                avatarState.inventory.AddNonFungibleItem(item);
            }

            return states.SetAvatarState(AvatarAddress, avatarState, true, true, false, false);
        }

        private ItemBase GetSynthesizedItem(Grade grade, Sheets sheets, IRandom random, ItemSubType itemSubTypeValue)
        {
            switch (itemSubTypeValue)
            {
                case ItemSubType.FullCostume:
                case ItemSubType.Title:
                    return GetRandomCostume(grade, itemSubTypeValue, sheets, random);
                case ItemSubType.Aura:
                case ItemSubType.Grimoire:
                    return GetRandomEquipment(grade, itemSubTypeValue, sheets, random);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#region GetRandomItem

        private ItemBase GetRandomCostume(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
        {
            var sheet = sheets.GetSheet<CostumeItemSheet>();
            var synthesizeWeightSheet = sheets.GetSheet<SynthesizeWeightSheet>();
            var synthesizeResultPool = GetSynthesizeResultPool(grade, itemSubType, sheet);

            if (synthesizeResultPool.Count == 0)
            {
                throw new InvalidOperationException($"No available items to synthesize for grade {grade} and subtype {itemSubType}");
            }

            var randomValue = GetRandomValueForItem(grade, synthesizeResultPool, synthesizeWeightSheet, random, out var itemWeights);
            float cumulativeWeight = 0;
            foreach (var (itemId, weight) in itemWeights)
            {
                cumulativeWeight += weight;
                if (randomValue >= cumulativeWeight)
                {
                    continue;
                }

                if (!sheet.TryGetValue(itemId, out var equipmentRow))
                {
                    throw new SheetRowNotFoundException(
                        $"Aborted as the equipment row ({itemId}) was failed to load in {nameof(EquipmentItemSheet)}", itemId
                    );
                }
                return ItemFactory.CreateItem(equipmentRow, random);
            }

            // Should not reach here
            throw new InvalidOperationException("Failed to select a synthesized item.");
        }

        private ItemBase GetRandomEquipment(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
        {
            var sheet = sheets.GetSheet<EquipmentItemSheet>();
            var synthesizeWeightSheet = sheets.GetSheet<SynthesizeWeightSheet>();
            var synthesizeResultPool = GetSynthesizeResultPool(grade, itemSubType, sheet);

            if (synthesizeResultPool.Count == 0)
            {
                throw new InvalidOperationException($"No available items to synthesize for grade {grade} and subtype {itemSubType}");
            }

            var randomValue = GetRandomValueForItem(grade, synthesizeResultPool, synthesizeWeightSheet, random, out var itemWeights);
            float cumulativeWeight = 0;
            foreach (var (itemId, weight) in itemWeights)
            {
                cumulativeWeight += weight;
                if (randomValue >= cumulativeWeight)
                {
                    continue;
                }

                if (!sheet.TryGetValue(itemId, out var equipmentRow))
                {
                    throw new SheetRowNotFoundException(
                        $"Aborted as the equipment row ({itemId}) was failed to load in {nameof(EquipmentItemSheet)}", itemId
                    );
                }
                return ItemFactory.CreateItem(equipmentRow, random);
            }

            // Should not reach here
            throw new InvalidOperationException("Failed to select a synthesized item.");
        }

        private float GetRandomValueForItem(Grade grade, HashSet<int> synthesizeResultPool, SynthesizeWeightSheet synthesizeWeightSheet,
            IRandom random, out List<(int ItemId, float Weight)> itemWeights)
        {
            float totalWeight = 0;
            itemWeights = new List<(int ItemId, float Weight)>();
            foreach (var itemId in synthesizeResultPool)
            {
                var weight = GetWeight(grade, itemId, synthesizeWeightSheet);
                itemWeights.Add((itemId, weight));
                totalWeight += weight;
            }

            // Random selection based on weight
            var randomValuePercentage = random.Next((int)(totalWeight * 100));
            return randomValuePercentage * 0.01f;
        }

#endregion GetRandomItem

        private Equipment? GetEquipmentFromId(Guid materialId, AvatarState avatarState, IActionContext context, string addressesHex)
        {
            if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out Equipment materialEquipment))
            {
                return null;
            }

            if (materialEquipment.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex} Aborted as the material ({materialId}) is not available yet;" +
                    $" it will be available at the block #{materialEquipment.RequiredBlockIndex}."
                );
            }

            // Validate item type
            if (!ValidItemType.Contains(materialEquipment.ItemType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item type: {materialEquipment.ItemType}."
                );
            }

            if (!ValidItemSubType.Contains(materialEquipment.ItemSubType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item sub type: {materialEquipment.ItemSubType}."
                );
            }

            _cachedItemSubType ??= materialEquipment.ItemSubType;
            if (materialEquipment.ItemSubType != _cachedItemSubType)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a {_cachedItemSubType}, but {materialEquipment.ItemSubType}."
                    );
            }

            return materialEquipment;
        }

        private Costume? GetCostumeFromId(Guid materialId, AvatarState avatarState, string addressesHex)
        {
            if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out Costume costumeItem))
            {
                return null;
            }

            // Validate item type
            if (!ValidItemType.Contains(costumeItem.ItemType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item type: {costumeItem.ItemType}."
                );
            }

            if (!ValidItemSubType.Contains(costumeItem.ItemSubType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item sub type: {costumeItem.ItemSubType}."
                );
            }

            _cachedItemSubType ??= costumeItem.ItemSubType;
            if (costumeItem.ItemSubType != _cachedItemSubType)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a {_cachedItemSubType}, but {costumeItem.ItemSubType}."
                    );
            }

            return costumeItem;
        }

        private void SetGradeDict(ref GradeDict gradeDict, int grade, ItemSubType itemSubType)
        {
            if (gradeDict.ContainsKey(grade))
            {
                if (gradeDict[grade].ContainsKey(itemSubType))
                {
                    gradeDict[grade][itemSubType]++;
                }
                else
                {
                    gradeDict[grade][itemSubType] = 1;
                }
            }
            else
            {
                gradeDict[grade] = new Dictionary<ItemSubType, int>
                {
                    { itemSubType, 1 },
                };
            }
        }

#region Serialize
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    [MaterialsKey] = new List(MaterialIds.OrderBy(i => i).Select(i => i.Serialize())),
                    [ChargeApKey] = ChargeAp.Serialize(),
                    [AvatarAddressKey] = AvatarAddress.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            ChargeAp = plainValue[ChargeApKey].ToBoolean();
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
#endregion Serialize

#region Helper

        public static HashSet<int> GetSynthesizeResultPool(Grade sourceGrade, ItemSubType subType, CostumeItemSheet sheet)
        {
            return sheet.Values
                .Where(r => r.ItemSubType == subType)
                .Where(r => (Grade)r.Grade == sourceGrade)
                .Select(r => r.Id)
                .ToHashSet();
        }

        public static HashSet<int> GetSynthesizeResultPool(Grade sourceGrade, ItemSubType subType, EquipmentItemSheet sheet)
        {
            return sheet.Values
                .Where(r => r.ItemSubType == subType)
                .Where(r => (Grade)r.Grade == sourceGrade)
                .Select(r => r.Id)
                .ToHashSet();
        }

        /// <summary>
        /// Returns a list of items that may come out as a result of that synthesis.
        /// </summary>
        /// <param name="sourceGrades">grades of material items</param>
        /// <param name="subType">excepted FullCostume,Title</param>
        /// <param name="sheet">CostumeItemSheet to use</param>
        /// <returns>list of items key(int)</returns>
        public static List<int> GetSynthesizeResultPool(List<Grade> sourceGrades, ItemSubType subType, CostumeItemSheet sheet)
        {
            return sheet.Values
                .Where(r => r.ItemSubType == subType)
                .Where(r => sourceGrades.Any(grade => (Grade)r.Grade == GetUpgradeGrade(grade, subType, sheet)))
                .Select(r => r.Id)
                .ToList();
        }

        /// <summary>
        /// Returns a list of items that may come out as a result of that synthesis.
        /// </summary>
        /// <param name="sourceGrades">grades of material items</param>
        /// <param name="subType">excepted Grimoire,Aura</param>
        /// <param name="sheet">EquipmentItemSheet to use</param>
        /// <returns>list of items key(int)</returns>
        public static List<int> GetSynthesizeResultPool(List<Grade> sourceGrades, ItemSubType subType, EquipmentItemSheet sheet)
        {
            return sheet.Values
                .Where(r => r.ItemSubType == subType)
                .Where(r => sourceGrades.Any(grade => (Grade)r.Grade == GetUpgradeGrade(grade, subType, sheet)))
                .Select(r => r.Id)
                .ToList();
        }

        public static Grade GetUpgradeGrade(Grade grade ,ItemSubType subType, CostumeItemSheet sheet)
        {
            var targetGrade = GetTargetGrade(grade);
            var hasGrade = sheet.Values
                .Any(r => r.ItemSubType == subType && r.Grade == (int)targetGrade);

            return hasGrade ? targetGrade : grade;
        }

        public static Grade GetUpgradeGrade(Grade grade ,ItemSubType subType, EquipmentItemSheet sheet)
        {
            var targetGrade = GetTargetGrade(grade);
            var hasGrade = sheet.Values
                .Any(r => r.ItemSubType == subType && r.Grade == (int)targetGrade);

            return hasGrade ? targetGrade : grade;
        }

        private static Grade GetTargetGrade(Grade grade) => grade switch
        {
            Grade.Normal => Grade.Rare,
            Grade.Rare => Grade.Epic,
            Grade.Epic => Grade.Unique,
            Grade.Unique => Grade.Legendary,
            Grade.Legendary => Grade.Divinity,
            Grade.Divinity => Grade.Divinity,
            _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, null),
        };

        public static int GetTargetGrade(int gradeId)
        {
            return gradeId switch
            {
                1 => 2, // Grade.Normal => Grade.Rare
                2 => 3, // Grade.Rare => Grade.Epic
                3 => 4, // Grade.Epic => Grade.Unique
                4 => 5, // Grade.Unique => Grade.Legendary
                5 => 6, // Grade.Legendary => Grade.Divinity
                6 => 6, // Grade.Divinity => Grade.Divinity (Max)
                _ => throw new ArgumentOutOfRangeException(nameof(gradeId), gradeId, null),
            };
        }

        // TODO: move to ItemExtensions
        public static List<Guid> GetItemGuid(ItemBase itemBase) => itemBase switch
        {
            Costume costume => new List<Guid> { costume.ItemId, },
            ItemUsable itemUsable => new List<Guid> { itemUsable.ItemId, },
            _ => throw new ArgumentException($"Unexpected item type: {itemBase.GetType()}", nameof(itemBase)),
        };

        public static List<Guid> GetItemGuids(IEnumerable<ItemBase> itemBases) => itemBases.Select(
            i =>
            {
                return i switch
                {
                    Costume costume => costume.ItemId,
                    ItemUsable itemUsable => itemUsable.ItemId,
                    _ => throw new ArgumentException($"Unexpected item type: {i.GetType()}", nameof(i)),
                };
            }).ToList();

        public static float GetWeight(Grade grade, int itemId, SynthesizeWeightSheet sheet)
        {
            var gradeRow = sheet.Values.FirstOrDefault(r => r.GradeId == (int)grade);
            if (gradeRow == null)
            {
                return 1;
            }

            return gradeRow.WeightDict.TryGetValue(itemId, out var weight) ? weight : 1;
        }

#endregion Helper
    }
}
