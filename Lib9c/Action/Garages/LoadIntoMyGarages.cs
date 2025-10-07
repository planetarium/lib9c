#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Exceptions;
using Lib9c.Model.Garages;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData.Garages;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action.Garages
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
        /// </summary>
        public Address? AvatarAddr { get; private set; }

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
                        AvatarAddr is null
                            ? Null.Value
                            : AvatarAddr.Serialize(),
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
            Address? avatarAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)
        {
            FungibleAssetValues = GarageUtils.MergeAndSort(fungibleAssetValues);
            AvatarAddr = avatarAddr;
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
            AvatarAddr = list[1].Kind == ValueKind.Null
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
            GasTracer.UseGas(1);
            var state = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context);
            ValidateFields(context.Signer, addressesHex);

            var sheet = state.GetSheet<LoadIntoMyGaragesCostSheet>();
            var garageCost = sheet.GetGarageCost(
                FungibleAssetValues?.Select(tuple => tuple.value),
                FungibleIdAndCounts);
            state = state.TransferAsset(
                context,
                context.Signer,
                Addresses.GarageWallet,
                garageCost);

            state = TransferFungibleAssetValues(context, state);
            return TransferFungibleItems(context.Signer, context.BlockIndex, state);
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

            if (!AvatarAddr.HasValue)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] {nameof(AvatarAddr)} is required when " +
                    $"{nameof(FungibleIdAndCounts)} is set.");
            }

            if (!Addresses.CheckInventoryAddrIsContainedInAgent(
                    signer,
                    AvatarAddr.Value))
            {
                throw new InvalidActionFieldException(
                    innerException: new InvalidAddressException(
                        $"[{addressesHex}] {signer} doesn't have permission for {AvatarAddr}."));
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
            IWorld states)
        {
            if (FungibleAssetValues is null)
            {
                return states;
            }

            var garageBalanceAddress =
                Addresses.GetGarageBalanceAddress(context.Signer);
            foreach (var (balanceAddr, value) in FungibleAssetValues)
            {
                states = states.TransferAsset(context, balanceAddr, garageBalanceAddress, value);
            }

            return states;
        }

        private IWorld TransferFungibleItems(
            Address signer,
            long blockIndex,
            IWorld states)
        {
            if (AvatarAddr is null ||
                FungibleIdAndCounts is null)
            {
                return states;
            }

            var avatarState = states.GetAvatarState(AvatarAddr.Value);
            var fungibleItemTuples = GarageUtils.WithGarageStateTuples(
                signer,
                states,
                FungibleIdAndCounts);
            foreach (var (fungibleId, count, garageAddr, garageState) in fungibleItemTuples)
            {
                if (!avatarState.inventory.TryGetFungibleItems(fungibleId, out var outItems))
                {
                    throw new ItemNotFoundException(AvatarAddr.Value, fungibleId);
                }

                if (!avatarState.inventory.RemoveFungibleItem(
                        fungibleId,
                        blockIndex: blockIndex,
                        count))
                {
                    throw new NotEnoughItemException(
                        AvatarAddr.Value,
                        fungibleId,
                        count,
                        outItems.Sum(i => i.count));
                }

                var item = outItems[0].item;
                if (item is not Material material)
                {
                    throw new InvalidCastException(
                        $"Invalid type of {nameof(item)}: " +
                        $"{item.GetType()}");
                }
                if (material is TradableMaterial tradableMaterial)
                {
                    material = new Material(tradableMaterial);
                }

                var garage = garageState is null || garageState is Null
                    ? new FungibleItemGarage(material, 0)
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
                states = states.SetLegacyState(garageAddr, garage.Serialize());
            }

            return states.SetAvatarState(AvatarAddr.Value, avatarState);
        }
    }
}
