using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("grinding2")]
    public class Grinding : GameAction, IGrindingV1
    {
        public const int CostAp = 5;
        public const int Limit = 50;
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public bool ChargeAp;

        Address IGrindingV1.AvatarAddress => AvatarAddress;
        List<Guid> IGrindingV1.EquipmentsIds => EquipmentIds;
        bool IGrindingV1.ChargeAp => ChargeAp;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            IWorld states = ctx.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding exec started", addressesHex);
            if (!EquipmentIds.Any() || EquipmentIds.Count > Limit)
            {
                throw new InvalidItemCountException();
            }

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("");
            }

            if (!states.TryGetAvatarState(ctx.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException("");
            }

            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                context.Signer,
                agentState.MonsterCollectionRound
            );

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(CrystalEquipmentGrindingSheet),
                typeof(CrystalMonsterCollectionMultiplierSheet),
                typeof(MaterialItemSheet),
                typeof(StakeRegularRewardSheet)
            });

            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue stakedAmount = states.GetStakedAmount(context.Signer);
            if (stakedAmount == currency * 0 &&
                states.TryGetLegacyState(monsterCollectionAddress, out Dictionary _))
            {
                stakedAmount = states.GetBalance(monsterCollectionAddress, currency);
            }

            if (!states.TryGetActionPoint(AvatarAddress, out var actionPoint))
            {
                actionPoint = avatarState.actionPoint;
            }

            if (actionPoint < CostAp)
            {
                switch (ChargeAp)
                {
                    case false:
                        throw new NotEnoughActionPointException("");
                    case true:
                    {
                        MaterialItemSheet.Row row = sheets.GetSheet<MaterialItemSheet>()
                            .OrderedList
                            .First(r => r.ItemSubType == ItemSubType.ApStone);
                        if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex))
                        {
                            throw new NotEnoughMaterialException("not enough ap stone.");
                        }
                        actionPoint = DailyReward.ActionPointMax;
                        break;
                    }
                }
            }

            actionPoint -= CostAp;

            List<Equipment> equipmentList = new List<Equipment>();
            foreach (var equipmentId in EquipmentIds)
            {
                if(avatarState.inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment))
                {
                    if (equipment.RequiredBlockIndex > context.BlockIndex)
                    {
                        throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                    }
                }
                else
                {
                    // Invalid Item Type.
                    throw new ItemDoesNotExistException($"Can't find Equipment. {equipmentId}");
                }

                if (!avatarState.inventory.RemoveNonFungibleItem(equipmentId))
                {
                    throw new ItemDoesNotExistException($"Can't find Equipment. {equipmentId}");
                }
                equipmentList.Add(equipment);
            }

            FungibleAssetValue crystal = CrystalCalculator.CalculateCrystal(
                context.Signer,
                equipmentList,
                stakedAmount,
                false,
                sheets.GetSheet<CrystalEquipmentGrindingSheet>(),
                sheets.GetSheet<CrystalMonsterCollectionMultiplierSheet>(),
                sheets.GetSheet<StakeRegularRewardSheet>()
            );

            var mail = new GrindingMail(
                ctx.BlockIndex,
                Id,
                ctx.BlockIndex,
                EquipmentIds.Count,
                crystal
            );
            avatarState.Update(mail);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetAvatarState(AvatarAddress, avatarState, true, true, false, false)
                .SetActionPoint(AvatarAddress, actionPoint)
                .MintAsset(context, context.Signer, crystal);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["e"] = new List(EquipmentIds.OrderBy(i => i).Select(i => i.Serialize())),
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
