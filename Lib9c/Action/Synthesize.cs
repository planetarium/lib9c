#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Helper;
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

            var synthesizedItems = SynthesizeSimulator.Simulate(new SynthesizeSimulator.InputData()
            {
                Sheets = sheets,
                RandomObject = context.GetRandom(),
                GradeDict = gradeDict,
            });

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

            // Add synthesized items to inventory
            foreach (var item in synthesizedItems)
            {
                avatarState.inventory.AddNonFungibleItem(item.ItemBase);
            }

            return states.SetAvatarState(AvatarAddress, avatarState, true, true, false, false);
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
                    [AvatarAddressKey] = AvatarAddress.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
#endregion Serialize
    }
}
