#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    using Sheets = Dictionary<Type, (Address, ISheet)>;
    using GradeDict = Dictionary<int, Dictionary<ItemSubType, int>>;

    public struct SynthesizeResult
    {
        public ItemBase ItemBase;
        // TODO: Add more fields
    }

    public static class SynthesizeSimulator
    {
        public struct InputData
        {
            public Sheets Sheets;
            public IRandom RandomObject;
            public GradeDict GradeDict;
        }

        public static List<SynthesizeResult> Simulate(InputData inputData)
        {
            var synthesizeResults = new List<SynthesizeResult>();

            var sheets = inputData.Sheets;
            var synthesizeSheet = sheets.GetSheet<SynthesizeSheet>();
            var random = inputData.RandomObject;
            var gradeDict = inputData.GradeDict;

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
                            $"Aborted as the number of materials for grade {gradeId} and subtype {itemSubType} is not enough."
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
                        synthesizeResults.Add(new SynthesizeResult { ItemBase = synthesizedItem, });
                    }
                }
            }

            return synthesizeResults;
        }

        private static ItemBase GetSynthesizedItem(Grade grade, Sheets sheets, IRandom random, ItemSubType itemSubTypeValue)
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

        private static ItemBase GetRandomCostume(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
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

        private static ItemBase GetRandomEquipment(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
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

        private static float GetRandomValueForItem(Grade grade, HashSet<int> synthesizeResultPool, SynthesizeWeightSheet synthesizeWeightSheet,
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

        public static float GetWeight(Grade grade, int itemId, SynthesizeWeightSheet sheet)
        {
            var gradeRow = sheet.Values.FirstOrDefault(r => r.GradeId == (int)grade);
            if (gradeRow == null)
            {
                return 1;
            }

            return gradeRow.WeightDict.TryGetValue(itemId, out var weight) ? weight : 1;
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
#endregion Helper
    }
}
