using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("item_enhancement7")]
    public class ItemEnhancement7 : GameAction, IItemEnhancementV2
    {
        public const int RequiredBlockCount = 1;

        public static readonly Address BlacksmithAddress = Addresses.Blacksmith;

        public Guid itemId;
        public Guid materialId;
        public Address avatarAddress;
        public int slotIndex;

        Guid IItemEnhancementV2.ItemId => itemId;
        Guid IItemEnhancementV2.MaterialId => materialId;
        Address IItemEnhancementV2.AvatarAddress => avatarAddress;
        int IItemEnhancementV2.SlotIndex => slotIndex;

        [Serializable]
        public class ResultModel : AttachmentActionResult
        {
            protected override string TypeId => "itemEnhancement.result";
            public Guid id;
            public IEnumerable<Guid> materialItemIdList;
            public BigInteger gold;
            public int actionPoint;

            public ResultModel()
            {
            }

            public ResultModel(Bencodex.Types.Dictionary serialized)
                : base(serialized)
            {
                id = serialized["id"].ToGuid();
                materialItemIdList = serialized["materialItemIdList"].ToList(StateExtensions.ToGuid);
                gold = serialized["gold"].ToBigInteger();
                actionPoint = serialized["actionPoint"].ToInteger();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "materialItemIdList"] = materialItemIdList
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                    [(Text) "actionPoint"] = actionPoint.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement exec started", addressesHex);

            var agentState = states.GetAgentState(ctx.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(ctx.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable enhancementItem))
            {
                throw new ItemDoesNotExistException(
                    $"{addressesHex}Aborted as the NonFungibleItem ({itemId}) was failed to load from avatar's inventory."
                );
            }

            if (enhancementItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the equipment to enhance ({itemId}) is not available yet; it will be available at the block #{enhancementItem.RequiredBlockIndex}."
                );
            }

            if (!(enhancementItem is Equipment enhancementEquipment))
            {
                throw new InvalidCastException(
                    $"{addressesHex}Aborted as the item is not a {nameof(Equipment)}, but {enhancementItem.GetType().Name}."

                );
            }

            var slotState = states.GetCombinationSlotStateLegacy(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the slot state was failed to load. #{slotIndex}");
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException($"{addressesHex}Aborted as the slot state was failed to invalid. #{slotIndex}");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get Equipment: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (enhancementEquipment.level > 9)
            {
                // Maximum level exceeded.
                throw new EquipmentLevelExceededException(
                    $"{addressesHex}Aborted due to invalid equipment level: {enhancementEquipment.level} < 9"
                );
            }

            var result = new ResultModel
            {
                itemUsable = enhancementEquipment,
                materialItemIdList = new[] { materialId }
            };

            var requiredAP = GetRequiredAp();
            if (avatarState.actionPoint < requiredAP)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredAP}"
                );
            }

            var enhancementCostSheet = states.GetSheet<EnhancementCostSheet>();
            var requiredNCG = GetRequiredNCG(enhancementCostSheet, enhancementEquipment.Grade, enhancementEquipment.level + 1);

            avatarState.actionPoint -= requiredAP;
            result.actionPoint = requiredAP;

            if (requiredNCG > 0)
            {
                states = states.TransferAsset(
                    ctx,
                    ctx.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * requiredNCG
                );
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the signer does not have a necessary material ({materialId})."
                );
            }

            if (materialItem.RequiredBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"{addressesHex}Aborted as the material ({materialId}) is not available yet; it will be available at the block #{materialItem.RequiredBlockIndex}."
                );
            }

            if (!(materialItem is Equipment materialEquipment))
            {
                throw new InvalidCastException(
                    $"{addressesHex}Aborted as the material item is not an {nameof(Equipment)}, but {materialItem.GetType().Name}."
                );
            }

            if (enhancementEquipment.ItemId == materialId)
            {
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as an equipment to enhance ({materialId}) was used as a material too."
                );
            }

            if (materialEquipment.ItemSubType != enhancementEquipment.ItemSubType)
            {
                // Invalid ItemSubType
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as the material item is not a {enhancementEquipment.ItemSubType}, but {materialEquipment.ItemSubType}."
                );
            }

            if (materialEquipment.Grade != enhancementEquipment.Grade)
            {
                // Invalid Grade
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as grades of the equipment to enhance ({enhancementEquipment.Grade}) and a material ({materialEquipment.Grade}) does not match."
                );
            }

            if (materialEquipment.level != enhancementEquipment.level)
            {
                // Invalid level
                throw new InvalidMaterialException(
                    $"{addressesHex}Aborted as levels of the equipment to enhance ({enhancementEquipment.level}) and a material ({materialEquipment.level}) does not match."
                );
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Get Material: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            materialEquipment.Unequip();

            enhancementEquipment.Unequip();

            enhancementEquipment = UpgradeEquipment(enhancementEquipment);

            var requiredBlockIndex = ctx.BlockIndex + RequiredBlockCount;
            enhancementEquipment.Update(requiredBlockIndex);
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Upgrade Equipment: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            result.gold = requiredNCG;

            avatarState.inventory.RemoveNonFungibleItem(materialId);
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Remove Materials: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            var random = ctx.GetRandom();
            var mail = new ItemEnhanceMail(result, ctx.BlockIndex, random.GenerateRandomGuid(), requiredBlockIndex);
            result.id = mail.id;

            avatarState.inventory.RemoveNonFungibleItem(enhancementEquipment);

            avatarState.Update(mail);
            avatarState.UpdateFromItemEnhancement(enhancementEquipment);

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            avatarState.UpdateQuestRewards(materialSheet);

            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);

            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Update AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();
            states = states.SetAvatarState(avatarAddress, avatarState);
            sw.Stop();
            Log.Verbose("{AddressesHex}ItemEnhancement Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}ItemEnhancement Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.SetLegacyState(slotAddress, slotState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var dict = new Dictionary<string, IValue>
                {
                    ["itemId"] = itemId.Serialize(),
                    ["materialId"] = materialId.Serialize(),
                    ["avatarAddress"] = avatarAddress.Serialize(),
                    ["slotIndex"] = slotIndex.Serialize(),
                };

                return dict.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            itemId = plainValue["itemId"].ToGuid();
            materialId = plainValue["materialId"].ToGuid();
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            if (plainValue.TryGetValue((Text) "slotIndex", out var value))
            {
                slotIndex = value.ToInteger();
            }
        }

        public static BigInteger GetRequiredNCG(EnhancementCostSheet costSheet, int grade, int level)
        {
            var row = costSheet
                .OrderedList
                .FirstOrDefault(x => x.Grade == grade && x.Level == level);

            return row?.Cost ?? 0;
        }

        public static Equipment UpgradeEquipment(Equipment equipment)
        {
            equipment.LevelUpV1();
            return equipment;
        }

        public static int GetRequiredAp()
        {
            return GameConfig.EnhanceEquipmentCostAP;
        }
    }
}
