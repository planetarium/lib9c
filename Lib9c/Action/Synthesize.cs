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

    /// <summary>
    /// Synthesize action is a type of action that synthesizes items.
    /// TODO: Implement Synthesize action.
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Synthesize : GameAction
    {
        private const string TypeIdentifier = "synthesize";

        private const string MaterialsKey = "m";
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
        public Address AvatarAddress;

        private ItemSubType? _cachedItemSubType;
#endregion Fields

        // TODO: 세부 기획 정해지면 그에 맞게 테스트 코드 우선 작성
        /// <summary>
        /// Execute Synthesize action, Input <see cref="MaterialIds"/> and <see cref="AvatarAddress"/>.
        /// Depending on the probability, you can get different items.
        /// Success: Return new AvatarState with synthesized item in inventory.
        /// </summary>
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
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
            });

            // TODO - TransferAsset (NCG)

            // Select id to equipment
            var sourceGrade = Grade.Normal; // TODO: change to set

            var materialEquipments = new List<Equipment>();
            var materialCostumes = new List<Costume>();
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
                    sourceGrade = (Grade)materialEquipment.Grade;
                }

                if (materialCostume != null)
                {
                    materialCostumes.Add(materialCostume);
                    sourceGrade = (Grade)materialCostume.Grade;
                }
            }

            if (_cachedItemSubType == null)
            {
                throw new InvalidOperationException("ItemSubType is not set.");
            }

            // Unequip items
            foreach (var materialEquipment in materialEquipments)
            {
                materialEquipment.Unequip();
            }
            foreach (var materialEquipment in materialCostumes)
            {
                materialEquipment.Unequip();
            }

            // Remove materials
            foreach (var materialId in MaterialIds)
            {
                avatarState.inventory.RemoveNonFungibleItem(materialId);
            }

            // clone random item
            // TODO: Add items to inventory
            var random = context.GetRandom();
            avatarState.inventory.AddNonFungibleItem(GetSynthesizedItem(sourceGrade, sheets, random, _cachedItemSubType.Value));

            return states.SetAvatarState(AvatarAddress, avatarState, true, true, false, false);
        }

        // TODO: Use Sheet, grade의 set화
        private ItemBase GetSynthesizedItem(Grade grade, Sheets sheets, IRandom random, ItemSubType itemSubTypeValue)
        {
            switch (itemSubTypeValue)
            {
                case ItemSubType.FullCostume:
                    return GetRandomCostume(grade, itemSubTypeValue, sheets, random);
                case ItemSubType.Title:
                    return GetRandomCostume(grade, itemSubTypeValue, sheets, random);
                case ItemSubType.Aura:
                    return GetRandomEquipment(grade, itemSubTypeValue, sheets, random);
                case ItemSubType.Grimoire:
                    return GetRandomEquipment(grade, itemSubTypeValue, sheets, random);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ItemBase GetRandomCostume(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
        {
            var sheet = sheets.GetSheet<CostumeItemSheet>();
            var synthesizeResultPool = GetSynthesizeResultPool(
                new List<Grade>() { grade, },
                itemSubType,
                sheet
            );

            // TODO: add weight
            var randomId = synthesizeResultPool[random.Next(synthesizeResultPool.Count)];
            if (!sheet.TryGetValue(randomId, out var costumeRow))
            {
                throw new SheetRowNotFoundException(
                    $"Aborted as the costume row ({randomId}) was failed to load in {nameof(CostumeItemSheet)}", randomId
                );
            }

            return ItemFactory.CreateItem(costumeRow, random);
        }

        private ItemBase GetRandomEquipment(Grade grade, ItemSubType itemSubType, Sheets sheets, IRandom random)
        {
            var sheet = sheets.GetSheet<EquipmentItemSheet>();
            var synthesizeResultPool = GetSynthesizeResultPool(
                new List<Grade>() { grade, },
                itemSubType,
                sheet
            );

            // TODO: add weight
            var randomId = synthesizeResultPool[random.Next(synthesizeResultPool.Count)];
            if (!sheet.TryGetValue(randomId, out var costumeRow))
            {
                throw new SheetRowNotFoundException(
                    $"Aborted as the equipment row ({randomId}) was failed to load in {nameof(EquipmentItemSheet)}", randomId
                );
            }

            return ItemFactory.CreateItem(costumeRow, random);
        }

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

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    [MaterialsKey] = new List(MaterialIds.OrderBy(i => i).Select(i => i.Serialize())),
                    [AvatarAddressKey] = AvatarAddress.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }

#region Helper

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

#endregion Helper
    }
}
