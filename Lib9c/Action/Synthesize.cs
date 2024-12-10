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
using Nekoyume.Model.EnumType;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    using Extensions;
    using Sheets = Dictionary<Type, (Address, ISheet)>;
    using GradeDict = Dictionary<int, Dictionary<ItemSubType, int>>;

    /// <summary>
    /// Synthesize action is a type of action that synthesizes items.
    /// <value>MaterialIds: Id list of items as material</value>
    /// <value><br/>ChargeAp: Whether to charge action points with action execution</value>
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Synthesize : GameAction
    {
        private const string TypeIdentifier = "synthesize";

        private const string MaterialsKey = "m";
        private const string ChargeApKey = "c";
        private const string AvatarAddressKey = "a";
        private const string GradeKey = "g";
        private const string ItemSubTypeKey = "i";

#region Fields
        /// <summary>
        /// Id list of items as material.
        /// </summary>
        public List<Guid> MaterialIds = new();
        /// <summary>
        /// Whether to charge action points with action execution.
        /// </summary>
        public bool ChargeAp;
        /// <summary>
        /// AvatarAddress of the signer.
        /// </summary>
        public Address AvatarAddress;
        /// <summary>
        /// MaterialGrade of the material item.
        /// </summary>
        public int MaterialGradeId;
        /// <summary>
        /// ItemSubType of the material item.
        /// </summary>
        public int MaterialItemSubTypeId;
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
            var materialGrade = (Grade)MaterialGradeId;
            var materialItemSubType = (ItemSubType)MaterialItemSubTypeId;

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
                typeof(MaterialItemSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(SkillSheet),
            });

            // Calculate action point
            var actionPoint = CalculateActionPoint(states, avatarState, sheets, context);
            var materialItems = SynthesizeSimulator.GetMaterialList(
                MaterialIds,
                avatarState,
                context.BlockIndex,
                materialGrade,
                materialItemSubType,
                addressesHex
            );

            // Unequip items (if necessary)
            foreach (var materialItem in materialItems)
            {
                switch (materialItem)
                {
                    case Equipment equipment:
                        equipment.Unequip();
                        break;
                    case Costume costume:
                        costume.Unequip();
                        break;
                }
            }

            if (materialItems.Count == 0 || materialItems.Count != MaterialIds.Count)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex} Aborted as the material item is not valid."
                );
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
            };

            var synthesizedItems = SynthesizeSimulator.Simulate(new SynthesizeSimulator.InputData()
            {
                Grade = materialGrade,
                ItemSubType = materialItemSubType,
                MaterialCount = materialItems.Count,
                SynthesizeSheet = sheets.GetSheet<SynthesizeSheet>(),
                SynthesizeWeightSheet = sheets.GetSheet<SynthesizeWeightSheet>(),
                CostumeItemSheet = sheets.GetSheet<CostumeItemSheet>(),
                EquipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>(),
                EquipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>(),
                EquipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                EquipmentItemOptionSheet = sheets.GetSheet<EquipmentItemOptionSheet>(),
                SkillSheet = sheets.GetSheet<SkillSheet>(),
                BlockIndex = context.BlockIndex,
                RandomObject = context.GetRandom(),
            });

            // Add synthesized items to inventory
            foreach (var item in synthesizedItems)
            {
                avatarState.inventory.AddNonFungibleItem(item.ItemBase);
            }

            return states
                   .SetActionPoint(AvatarAddress, actionPoint)
                   .SetAvatarState(AvatarAddress, avatarState, true, true, false, false);
        }

        private long CalculateActionPoint(IWorld states, AvatarState avatarState, Sheets sheets, IActionContext context)
        {
            if (!states.TryGetActionPoint(AvatarAddress, out var actionPoint))
            {
                actionPoint = avatarState.actionPoint;
            }

            if (actionPoint < GameConfig.ActionCostAP)
            {
                switch (ChargeAp)
                {
                    case false:
                        throw new NotEnoughActionPointException("Action point is not enough. for synthesize.");
                    case true:
                    {
                        var row = sheets.GetSheet<MaterialItemSheet>()
                                                          .OrderedList?
                                                          .First(r => r.ItemSubType == ItemSubType.ApStone);
                        if (row == null)
                        {
                            throw new SheetRowNotFoundException(
                                nameof(MaterialItemSheet),
                                ItemSubType.ApStone.ToString()
                            );
                        }

                        if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex))
                        {
                            throw new NotEnoughMaterialException("not enough ap stone.");
                        }
                        actionPoint = DailyReward.ActionPointMax;
                        break;
                    }
                }
            }

            actionPoint -= GameConfig.ActionCostAP;
            return actionPoint;
        }

#region Serialize
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    [MaterialsKey] = new List(MaterialIds.OrderBy(i => i).Select(i => i.Serialize())),
                    [ChargeApKey] = ChargeAp.Serialize(),
                    [AvatarAddressKey] = AvatarAddress.Serialize(),
                    [GradeKey] = (Integer)MaterialGradeId,
                    [ItemSubTypeKey] = (Integer)MaterialItemSubTypeId,
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            ChargeAp = plainValue[ChargeApKey].ToBoolean();
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            MaterialGradeId = (Integer)plainValue[GradeKey];
            MaterialItemSubTypeId = (Integer)plainValue[ItemSubTypeKey];
        }
#endregion Serialize
    }
}
