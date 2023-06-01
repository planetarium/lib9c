#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.State;
using Nekoyume.Exceptions;
using Nekoyume.Model.Garages;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("load_into_my_garages")]
    public class LoadIntoMyGarages : GameAction, ILoadIntoMyGarages, IAction
    {
        public IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues { get; private set; }

        /// <summary>
        /// This address should belong to one of the signer's avatars.
        /// If the avatar state is v1, there is no separate inventory,
        /// so it should be execute another action first to migrate the avatar state to v2.
        /// And then, the inventory address will be set.
        /// </summary>
        public Address? InventoryAddr { get; private set; }

        public IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>?
            FungibleIdAndCounts { get; private set; }

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
                                (Integer)tuple.count))))
                }
            }.ToImmutableDictionary();

        public LoadIntoMyGarages(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            (
                FungibleAssetValues,
                InventoryAddr,
                FungibleIdAndCounts
            ) = GarageUtils.MergeAndSort(
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts);
        }

        public LoadIntoMyGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            (
                FungibleAssetValues,
                InventoryAddr,
                FungibleIdAndCounts
            ) = GarageUtils.Deserialize(plainValue["l"]);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context);
            ValidateFields(
                context.Signer,
                addressesHex);
            // !!!
            // TODO: Validate and burn costs.
            // !!!
            states = TransferFungibleAssetValues(
                context.Signer,
                states);
            return TransferFungibleItems(
                context.Signer,
                context.BlockIndex,
                states);
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

            if (!Addresses.CheckInventoryAddrIsContainedInAgent(signer,
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

        private IAccountStateDelta TransferFungibleAssetValues(
            Address signer,
            IAccountStateDelta states)
        {
            if (FungibleAssetValues is null)
            {
                return states;
            }

            var garageBalanceAddress =
                Addresses.GetGarageBalanceAddress(signer);
            foreach (var (balanceAddr, value) in FungibleAssetValues)
            {
                states = states.TransferAsset(balanceAddr, garageBalanceAddress, value);
            }

            return states;
        }

        private IAccountStateDelta TransferFungibleItems(
            Address signer,
            long blockIndex,
            IAccountStateDelta states)
        {
            if (InventoryAddr is null ||
                FungibleIdAndCounts is null)
            {
                return states;
            }

            var inventory = states.GetInventory(InventoryAddr.Value);
            var fungibleItemTuples = GarageUtils.WithGarageStateTuples(
                signer,
                states,
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

                var garage = garageState is null || garageState is Null
                    ? new FungibleItemGarage(tradableFungibleItem, 0)
                    : new FungibleItemGarage(garageState);
                // NOTE: Why not compare the garage.Item with tradableFungibleItem?
                //       Because the ITradableFungibleItem.Equals() method compares the
                //       ITradableItem.RequiredBlockIndex property.
                //       The IFungibleItem.FungibleId property fully contains the
                //       specification of the fungible item.
                //       So ITradableItem.RequiredBlockIndex property does not considered
                //       when transferring items via garages.
                if (!garage.Item.FungibleId.Equals(fungibleId))
                {
                    throw new Exception(
                        $"{garageAddr} is not a garage of {fungibleId}.");
                }

                garage.Add(count);
                states = states.SetState(garageAddr, garage.Serialize());
            }

            return states.SetState(InventoryAddr.Value, inventory.Serialize());
        }
    }
}
