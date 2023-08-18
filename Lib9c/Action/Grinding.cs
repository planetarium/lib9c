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
using Nekoyume.Action.Extensions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("grinding")]
    public class Grinding : GameAction, IGrindingV1
    {
        public const int CostAp = 5;
        public const int Limit = 10;
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
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                var account = world.GetAccount(ReservedAddresses.LegacyAccount);
                account = EquipmentIds.Aggregate(account,
                    (current, guid) =>
                        current.SetState(Addresses.GetItemAddress(guid), MarkChanged));
                account = account
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 0), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 1), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 2), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 3), MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .MarkBalanceChanged(context, GoldCurrencyMock, context.Signer);
                return world.SetAccount(account);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding exec started", addressesHex);
            if (!EquipmentIds.Any() || EquipmentIds.Count > Limit)
            {
                throw new InvalidItemCountException();
            }

            if (!AvatarModule.TryGetAgentAvatarStatesV2(world, ctx.Signer, AvatarAddress, out var agentState,
                    out var avatarState, out bool migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                context.Signer,
                agentState.MonsterCollectionRound
            );

            Dictionary<Type, (Address, ISheet)> sheets = world
                .GetAccount(ReservedAddresses.LegacyAccount)
                .GetSheets(
                    sheetTypes: new[]
                    {
                        typeof(CrystalEquipmentGrindingSheet),
                        typeof(CrystalMonsterCollectionMultiplierSheet),
                        typeof(MaterialItemSheet),
                        typeof(StakeRegularRewardSheet)
                    });

            Currency currency = world.GetAccount(ReservedAddresses.LegacyAccount).GetGoldCurrency();
            FungibleAssetValue stakedAmount = 0 * currency;
            if (world.GetAccount(ReservedAddresses.LegacyAccount)
                .TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                stakedAmount = world.GetAccount(ReservedAddresses.LegacyAccount)
                    .GetBalance(stakeState.address, currency);
            }
            else
            {
                if (world.GetAccount(ReservedAddresses.LegacyAccount)
                    .TryGetState(monsterCollectionAddress, out Dictionary _))
                {
                    stakedAmount = world.GetAccount(ReservedAddresses.LegacyAccount)
                        .GetBalance(monsterCollectionAddress, currency);
                }
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

                        GameConfigState gameConfigState = world
                            .GetAccount(ReservedAddresses.LegacyAccount)
                            .GetGameConfigState();
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

            if (migrationRequired)
            {
                var account = world.GetAccount(ReservedAddresses.LegacyAccount);
                account = account
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(questListAddress, avatarState.questList.Serialize());
                world = world.SetAccount(account);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Grinding Total Executed Time: {Elapsed}", addressesHex, ended - started);
            world = AvatarModule.SetAvatarStateV2(world, AvatarAddress, avatarState);
            return world.SetAccount(world.GetAccount(ReservedAddresses.LegacyAccount)
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .MintAsset(context, context.Signer, crystal));
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
