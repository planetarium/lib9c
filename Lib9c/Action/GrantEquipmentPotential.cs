using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Grants (rolls) latent "potential" options onto an existing equipment owned by the avatar.
    /// The number of slots is determined by the equipment's grade and a material cost is consumed,
    /// both driven by <see cref="EquipmentPotentialGradeSheet"/>; the rolled options come from
    /// <see cref="EquipmentPotentialOptionPoolSheet"/>. This is the granting phase only — the rolled
    /// options are stored on the equipment as (option id, value) pairs, and their interpretation
    /// into stat effects is handled elsewhere.
    ///
    /// <para>
    /// Re-rolling is intentional: running this action again on the same equipment overwrites the
    /// existing potential with a fresh roll and charges the cost again. It does not accumulate slots.
    /// </para>
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class GrantEquipmentPotential : ActionBase
    {
        /// <summary>The on-chain action type identifier.</summary>
        public const string TypeIdentifier = "grant_equipment_potential";

        /// <summary>The avatar that owns the target equipment.</summary>
        public Address AvatarAddress;

        /// <summary>The non-fungible id of the equipment to grant potential options to.</summary>
        public Guid ItemId;

        /// <inheritdoc/>
        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(ItemId.Serialize()));

        /// <inheritdoc/>
        public override void LoadPlainValue(IValue plainValue)
        {
            var values = (List)((Dictionary)plainValue)["values"];
            AvatarAddress = values[0].ToAddress();
            ItemId = values[1].ToGuid();
        }

        /// <inheritdoc/>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    TypeIdentifier,
                    addressesHex,
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in AvatarAddress({AvatarAddress}).");
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"[{addressesHex}] Aborted as the avatar state of the signer was failed to load.");
            }

            if (!avatarState.inventory.TryGetNonFungibleItem<Equipment>(ItemId, out var equipment))
            {
                throw new ItemDoesNotExistException(
                    $"[{addressesHex}] Aborted as the equipment ({ItemId}) was not found in the inventory.");
            }

            // Only regular equipment parts are eligible; Aura/Grimoire and custom-only parts are excluded.
            if (!IsEligibleSubType(equipment.ItemSubType))
            {
                throw new InvalidItemTypeException(
                    $"[{addressesHex}] {equipment.ItemSubType} is not eligible for potential options.");
            }

            var sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentPotentialGradeSheet),
                typeof(EquipmentPotentialOptionPoolSheet),
            });

            var gradeSheet = sheets.GetSheet<EquipmentPotentialGradeSheet>();
            if (!gradeSheet.TryGetValue(equipment.Grade, out var gradeRow) || gradeRow.SlotCount <= 0)
            {
                throw new InvalidItemTypeException(
                    $"[{addressesHex}] Equipment grade {equipment.Grade} has no potential slots.");
            }

            // Consume the material cost for this grade.
            if (gradeRow.CostMaterialCount > 0)
            {
                if (!avatarState.inventory.RemoveMaterial(
                        gradeRow.CostMaterialId, context.BlockIndex, gradeRow.CostMaterialCount))
                {
                    throw new NotEnoughItemException(
                        $"[{addressesHex}] Insufficient material {gradeRow.CostMaterialId}: " +
                        $"{gradeRow.CostMaterialCount} required.");
                }
            }

            // Roll potential options and store them on the equipment (in place; it is the inventory's item).
            var random = context.GetRandom();
            var poolSheet = sheets.GetSheet<EquipmentPotentialOptionPoolSheet>();
            var potential = PotentialHelper.Roll(equipment, gradeRow.SlotCount, poolSheet, random);
            equipment.SetPotential(potential);

            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        private static bool IsEligibleSubType(ItemSubType itemSubType)
        {
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                    return true;
                default:
                    return false;
            }
        }
    }
}
