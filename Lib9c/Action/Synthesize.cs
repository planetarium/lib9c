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
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Synthesize : GameAction
    {
        private const string TypeIdentifier = "synthesize";

        private const string MaterialsKey = "m";
        private const string AvatarAddressKey = "a";
        private const string ItemSubTypeKey = "s";

        private static readonly ItemType[] InvalidItemType =
        {
            ItemType.Costume,
            ItemType.Equipment,
        };

        private static readonly ItemSubType[] InvalidItemSubType =
        {
            ItemSubType.FullCostume,
            ItemSubType.Title,
            ItemSubType.Grimoire,
            ItemSubType.Aura,
        };

#region Fields
        public List<Guid> MaterialIds = new();
        public Address AvatarAddress;
        // ItemSubType만 체크하면 ItemType은 자동으로 체크된다고 가정
        public int ItemSubTypeValue;
#endregion Fields

        // TODO: 세부 기획 정해지면 그에 맞게 테스트 코드 우선 작성
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

            // TODO - Use Sheets

            // TODO - TransferAsset (NCG)

            // Select id to equipment
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
                }

                if (materialCostume != null)
                {
                    materialCostumes.Add(materialCostume);
                }
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
            avatarState.inventory.AddNonFungibleItem(GetSynthesizedItem(context));

            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        // TODO: Use Sheet
        private ItemBase GetSynthesizedItem(IActionContext context)
        {
            // TODO: 기획 상세에 따라 구현, 현재는 임의 아이템 생성
            switch ((ItemSubType)ItemSubTypeValue)
            {
                case ItemSubType.FullCostume:
                    return GetRandomFullCostume(context);
                case ItemSubType.Title:
                    return GetRandomTitle(context);
                case ItemSubType.Aura:
                    //return GetRandomAura(context);
                case ItemSubType.Grimoire:
                    //return GetRandomGrimoire(context);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ItemBase GetRandomFullCostume(IActionContext context)
        {
            return GetRandomCostume(context, 40100000, 30);
        }

        private ItemBase GetRandomTitle(IActionContext context)
        {
            return GetRandomCostume(context, 49900001, 21);
        }

        private ItemBase GetRandomAura(IActionContext context)
        {
            // TODO: 시트 로드 최적화, 테스트 코드라 냅둠
            Dictionary<Type, (Address, ISheet)> sheets = context.PreviousState.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemSheet),
            });
            var sheet = sheets.GetSheet<EquipmentItemSheet>();

            var row = sheet.Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.Aura);
            return ItemFactory.CreateItem(row, context.GetRandom());
        }

        private ItemBase GetRandomGrimoire(IActionContext context)
        {
            // TODO: 시트 로드 최적화, 테스트 코드라 냅둠
            Dictionary<Type, (Address, ISheet)> sheets = context.PreviousState.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemSheet),
            });
            var sheet = sheets.GetSheet<EquipmentItemSheet>();

            var row = sheet.Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.Grimoire);
            return ItemFactory.CreateItem(row, context.GetRandom());
        }

        private ItemBase GetRandomCostume(IActionContext context, int keyBase, int keyUpperBound)
        {
            var random   = context.GetRandom();
            var randomId = keyBase + random.Next(0, keyUpperBound);

            Dictionary<Type, (Address, ISheet)> sheets = context.PreviousState.GetSheets(sheetTypes: new[]
            {
                typeof(CostumeItemSheet),
            });
            var sheet = sheets.GetSheet<CostumeItemSheet>();
            if (!sheet.TryGetValue(randomId, out var costumeRow))
            {
                throw new SheetRowNotFoundException(
                    $"Aborted as the costume row ({randomId}) was failed to load in {nameof(CostumeItemSheet)}", randomId
                );
            }

            return ItemFactory.CreateCostume(costumeRow, Guid.NewGuid());
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
            if (InvalidItemType.Contains(materialEquipment.ItemType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item type: {materialEquipment.ItemType}."
                );
            }

            if (InvalidItemSubType.Contains(materialEquipment.ItemSubType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item sub type: {materialEquipment.ItemSubType}."
                );
            }

            var itemSubType = (ItemSubType)ItemSubTypeValue;
            if (materialEquipment.ItemSubType != itemSubType)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a {itemSubType}, but {materialEquipment.ItemSubType}."
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
            if (InvalidItemType.Contains(costumeItem.ItemType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item type: {costumeItem.ItemType}."
                );
            }

            if (InvalidItemSubType.Contains(costumeItem.ItemSubType))
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a valid item sub type: {costumeItem.ItemSubType}."
                );
            }

            var itemSubType = (ItemSubType)ItemSubTypeValue;
            if (costumeItem.ItemSubType != itemSubType)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not a {itemSubType}, but {costumeItem.ItemSubType}."
                    );
            }

            return costumeItem;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    [MaterialsKey] = new List(MaterialIds.OrderBy(i => i).Select(i => i.Serialize())),
                    [AvatarAddressKey] = AvatarAddress.Serialize(),
                    [ItemSubTypeKey] = (Integer)ItemSubTypeValue,
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            ItemSubTypeValue= (Integer)plainValue[ItemSubTypeKey];
        }
    }
}
