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
        private const string ActionTypeText = "grinding";
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
            var world = ctx.PreviousState;
            if (ctx.Rehearsal)
            {
                world = EquipmentIds.Aggregate(world,
                    (current, guid) =>
                        LegacyModule.SetState(current, Addresses.GetItemAddress(guid), MarkChanged));
                world = LegacyModule.SetState(
                    world,
                    MonsterCollectionState.DeriveAddress(context.Signer, 0),
                    MarkChanged);
                world = LegacyModule.SetState(
                    world,
                    MonsterCollectionState.DeriveAddress(context.Signer, 1),
                    MarkChanged);
                world = LegacyModule.SetState(
                    world,
                    MonsterCollectionState.DeriveAddress(context.Signer, 2),
                    MarkChanged);
                world = LegacyModule.SetState(
                    world,
                    MonsterCollectionState.DeriveAddress(context.Signer, 3),
                    MarkChanged);
                world = AvatarModule.MarkChanged(world, AvatarAddress, true, true, true, true);
                world = LegacyModule.MarkBalanceChanged(world, context, GoldCurrencyMock, context.Signer);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding exec started", addressesHex);
            if (!EquipmentIds.Any() || EquipmentIds.Count > Limit)
            {
                throw new InvalidItemCountException();
            }

            AgentState agentState = AgentModule.GetAgentState(world, ctx.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AgentState),
                    AvatarAddress);
            }
            if (!AvatarModule.TryGetAvatarState(
                    world,
                    ctx.Signer,
                    AvatarAddress,
                    out var avatarState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                context.Signer,
                agentState.MonsterCollectionRound
            );

            Dictionary<Type, (Address, ISheet)> sheets = LegacyModule.GetSheets(
                world,
                sheetTypes: new[]
                {
                    typeof(CrystalEquipmentGrindingSheet),
                    typeof(CrystalMonsterCollectionMultiplierSheet),
                    typeof(MaterialItemSheet),
                    typeof(StakeRegularRewardSheet)
                });

            Currency currency = LegacyModule.GetGoldCurrency(world);
            FungibleAssetValue stakedAmount = LegacyModule.GetStakedAmount(world, context.Signer);
            if (stakedAmount == currency * 0 &&
                LegacyModule.TryGetState(world, monsterCollectionAddress, out Dictionary _))
            {
                stakedAmount = LegacyModule.GetBalance(world, monsterCollectionAddress, currency);
            }

            if (avatarState.actionPoint < CostAp)
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

                        GameConfigState gameConfigState = LegacyModule.GetGameConfigState(world);
                        avatarState.actionPoint = gameConfigState.ActionPointMax;
                        break;
                    }
                }
            }

            avatarState.actionPoint -= CostAp;

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

            world = AvatarModule.SetAvatarState(
                world,
                AvatarAddress,
                avatarState,
                true,
                true,
                false,
                false);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding Total Executed Time: {Elapsed}", addressesHex, ended - started);

            world = LegacyModule.MintAsset(world, context, context.Signer, crystal);
            return world;
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
