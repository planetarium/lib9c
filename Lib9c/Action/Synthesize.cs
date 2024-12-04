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

#region Fields
        public List<Guid> MaterialIds = new();
        public bool ChargeAp;
        public Address AvatarAddress;
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
                typeof(MaterialItemSheet),
            });

            // Calculate action point
            var actionPoint = CalculateActionPoint(states, avatarState, sheets, context);

            // Initialize variables
            var gradeDict = SynthesizeSimulator.GetGradeDict(MaterialIds, avatarState, context.BlockIndex,
                addressesHex, out var materialEquipments, out var materialCostumes);

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
            };

            var synthesizedItems = SynthesizeSimulator.Simulate(new SynthesizeSimulator.InputData()
            {
                SynthesizeSheet = sheets.GetSheet<SynthesizeSheet>(),
                SynthesizeWeightSheet = sheets.GetSheet<SynthesizeWeightSheet>(),
                CostumeItemSheet = sheets.GetSheet<CostumeItemSheet>(),
                EquipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>(),
                RandomObject = context.GetRandom(),
                GradeDict = gradeDict,
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
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            MaterialIds = plainValue[MaterialsKey].ToList(StateExtensions.ToGuid);
            ChargeAp = plainValue[ChargeApKey].ToBoolean();
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
#endregion Serialize
    }
}
