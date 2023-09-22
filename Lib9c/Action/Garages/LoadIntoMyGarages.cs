#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Extensions;
using Nekoyume.Exceptions;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.Garages;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData.Garages;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action.Garages
{
    [ActionType("load_into_my_garages")]
    public class LoadIntoMyGarages : GameAction, ILoadIntoMyGaragesV1, IAction
    {
        public IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues { get; private set; }

        /// <summary>
        /// This address should belong to one of the signer's avatars.
        /// If the avatar state is v1, there is no separate inventory,
        /// so it should be execute another action first to migrate the avatar state to v2.
        /// And then, the inventory address will be set.
        /// After v2, the inventory address is equal to its avatar address,
        /// but still need another action first to migrate.
        /// </summary>
        public Address? InventoryAddr { get; private set; }

        public IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts { get; private set; }

        public string? Memo { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                {
                    "l",
                    new List(
                        FungibleAssetValues is null
                            ? (IValue)Null.Value
                            : new List(FungibleAssetValues.Select(tuple => new List(
                                tuple.balanceAddr.Serialize(),
                                tuple.value.Serialize()))),
                        InventoryAddr is null
                            ? Null.Value
                            : InventoryAddr.Serialize(),
                        FungibleIdAndCounts is null
                            ? (IValue)Null.Value
                            : new List(FungibleIdAndCounts.Select(tuple => new List(
                                tuple.fungibleId.Serialize(),
                                (Integer)tuple.count))),
                        string.IsNullOrEmpty(Memo)
                            ? (IValue)Null.Value
                            : (Text)Memo)
                }
            }.ToImmutableDictionary();

        public LoadIntoMyGarages(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            InventoryAddr = inventoryAddr;
            FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCounts);
            Memo = memo;
        }

        public LoadIntoMyGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = plainValue["l"];
            if (serialized is null || serialized is Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            if (!(serialized is List list))
            {
                throw new ArgumentException(
                    $"The type of {nameof(serialized)} must be bencodex list.");
            }

            var fungibleAssetValues = list[0].Kind == ValueKind.Null
                ? null
                : ((List)list[0]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToAddress(),
                        l2[1].ToFungibleAssetValue());
                });
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            InventoryAddr = list[1].Kind == ValueKind.Null
                ? (Address?)null
                : list[1].ToAddress();
            var fungibleIdAndCounts = list[2].Kind == ValueKind.Null
                ? null
                : ((List)list[2]).Select(e =>
                {
                    var l2 = (List)e;
                    return (
                        l2[0].ToItemId(),
                        (int)((Integer)l2[1]).Value);
                });
            FungibleIdAndCounts = GarageUtils.MergeAndSort(fungibleIdAndCounts);
            Memo = list[3].Kind == ValueKind.Null
                ? null
                : (string)(Text)list[3];
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var world = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context);
            ValidateFields(context.Signer, addressesHex);

            var sheet = LegacyModule.GetSheet<LoadIntoMyGaragesCostSheet>(world);
            var garageCost = sheet.GetGarageCost(
                FungibleAssetValues?.Select(tuple => tuple.value),
                FungibleIdAndCounts);
            world = LegacyModule.TransferAsset(
                world,
                context,
                context.Signer,
                Addresses.GarageWallet,
                garageCost);

            world = TransferFungibleAssetValues(context, world);
            return TransferFungibleItems(context.Signer, context.BlockIndex, world);
        }

        private void ValidateFields(
            Address signer,
            string addressesHex)
        {
            if (FungibleAssetValues is null &&
                FungibleIdAndCounts is null)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] Either FungibleAssetValues or FungibleIdAndCounts " +
                    "must be set.");
            }

            if (FungibleAssetValues != null)
            {
                foreach (var (balanceAddr, value) in FungibleAssetValues)
                {
                    if (!Addresses.CheckAgentHasPermissionOnBalanceAddr(
                            signer,
                            balanceAddr))
                    {
                        throw new InvalidActionFieldException(
                            innerException: new InvalidAddressException(
                                $"[{addressesHex}] {signer} doesn't have permission for " +
                                $"{balanceAddr}."));
                    }

                    if (value.Sign < 0)
                    {
                        throw new InvalidActionFieldException(
                            $"[{addressesHex}] FungibleAssetValue.Sign must be positive.");
                    }
                }
            }

            if (FungibleIdAndCounts is null)
            {
                return;
            }

            if (!InventoryAddr.HasValue)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] {nameof(InventoryAddr)} is required when " +
                    $"{nameof(FungibleIdAndCounts)} is set.");
            }

            // FIXME: This may cause mistakes.
            if (!Addresses.CheckInventoryAddrIsContainedInAgent(
                    signer,
                    InventoryAddr.Value) &&
                !Addresses.CheckAvatarAddrIsContainedInAgent(
                    signer,
                    InventoryAddr.Value))
            {
                throw new InvalidActionFieldException(
                    innerException: new InvalidAddressException(
                        $"[{addressesHex}] {signer} doesn't have permission for {InventoryAddr}."));
            }

            foreach (var (fungibleId, count) in FungibleIdAndCounts)
            {
                if (count < 0)
                {
                    throw new InvalidActionFieldException(
                        $"[{addressesHex}] Count of fungible id must be positive." +
                        $" {fungibleId}, {count}");
                }
            }
        }

        private IWorld TransferFungibleAssetValues(
            IActionContext context,
            IWorld world)
        {
            if (FungibleAssetValues is null)
            {
                return world;
            }

            var garageBalanceAddress =
                Addresses.GetGarageBalanceAddress(context.Signer);
            foreach (var (balanceAddr, value) in FungibleAssetValues)
            {
                world = LegacyModule.TransferAsset(
                    world,
                    context,
                    balanceAddr,
                    garageBalanceAddress,
                    value);
            }

            return world;
        }

        private IWorld TransferFungibleItems(
            Address signer,
            long blockIndex,
            IWorld world)
        {
            if (InventoryAddr is null ||
                FungibleIdAndCounts is null)
            {
                return world;
            }

            Inventory inventory;
            try
            {
                // Try load inventory from v2 avatar state first. If not exist, try v2.
                inventory = AvatarModule.GetInventory(world, InventoryAddr.Value);
            }
            catch (FailedLoadStateException)
            {
                inventory = LegacyModule.GetInventory(
                    world,
                    InventoryAddr.Value.Derive(LegacyInventoryKey));
            }

            var fungibleItemTuples = GarageUtils.WithGarageStateTuples(
                signer,
                world,
                FungibleIdAndCounts);
            foreach (var (fungibleId, count, garageAddr, garageState) in fungibleItemTuples)
            {
                if (!inventory.TryGetTradableFungibleItems(
                        fungibleId,
                        requiredBlockIndex: null,
                        blockIndex: blockIndex,
                        out var outItems))
                {
                    throw new ItemNotFoundException(InventoryAddr.Value, fungibleId);
                }

                var itemArr = outItems as Inventory.Item[] ?? outItems.ToArray();
                var tradableFungibleItem = (ITradableFungibleItem)itemArr[0].item;
                if (!inventory.RemoveTradableFungibleItem(
                        fungibleId,
                        requiredBlockIndex: null,
                        blockIndex: blockIndex,
                        count))
                {
                    throw new NotEnoughItemException(
                        InventoryAddr.Value,
                        fungibleId,
                        count,
                        itemArr.Sum(item => item.count));
                }

                IFungibleItem fungibleItem = tradableFungibleItem switch
                {
                    TradableMaterial tradableMaterial => new Material(tradableMaterial),
                    _ => throw new InvalidCastException(
                        $"Invalid type of {nameof(tradableFungibleItem)}: " +
                        $"{tradableFungibleItem.GetType()}")
                };

                var garage = garageState is null || garageState is Null
                    ? new FungibleItemGarage(fungibleItem, 0)
                    : new FungibleItemGarage(garageState);
                // NOTE:
                // Why not compare the garage.Item with tradableFungibleItem?
                // Because the ITradableFungibleItem.Equals() method compares the
                // ITradableItem.RequiredBlockIndex property.
                // The IFungibleItem.FungibleId property fully contains the
                // specification of the fungible item.
                // So ITradableItem.RequiredBlockIndex property does not considered
                // when transferring items via garages.
                if (!garage.Item.FungibleId.Equals(fungibleId))
                {
                    throw new Exception(
                        $"{garageAddr} is not a garage of {fungibleId}.");
                }

                garage.Load(count);
                world = LegacyModule.SetState(world, garageAddr, garage.Serialize());
            }

            return LegacyModule.SetState(world, InventoryAddr.Value, inventory.Serialize());
        }
    }
}
