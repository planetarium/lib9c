#nullable enable

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
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Garages
{
    [ActionType("transfer_from_garages")]
    public class TransferFromGarages : GameAction, ITransferFromGarages, IAction
    {
        public IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>?
            FungibleAssetValues { get; private set; }

        /// <summary>
        /// The recipient's inventory address.
        /// If the avatar state is v1, there is no separate inventory,
        /// so it should be execute another action first to migrate the avatar state to v2.
        /// And then, the inventory address will be set.
        /// </summary>
        public Address? RecipientInventoryAddr { get; private set; }

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
                        RecipientInventoryAddr is null
                            ? Null.Value
                            : RecipientInventoryAddr.Serialize(),
                        FungibleIdAndCounts is null
                            ? (IValue)Null.Value
                            : new List(FungibleIdAndCounts.Select(tuple => new List(
                                tuple.fungibleId.Serialize(),
                                (Integer)tuple.count))))
                }
            }.ToImmutableDictionary();

        public TransferFromGarages(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? recipientInventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            (
                FungibleAssetValues,
                RecipientInventoryAddr,
                FungibleIdAndCounts
            ) = GarageUtils.MergeAndSort(
                fungibleAssetValues,
                recipientInventoryAddr,
                fungibleIdAndCounts);
        }

        public TransferFromGarages()
        {
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            (
                FungibleAssetValues,
                RecipientInventoryAddr,
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
            ValidateFields(addressesHex);
            states = TransferFungibleAssetValues(
                context.Signer,
                states);
            return TransferFungibleItems(
                context.Signer,
                states);
        }

        private void ValidateFields(string addressesHex)
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
                foreach (var (_, value) in FungibleAssetValues)
                {
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

            if (!RecipientInventoryAddr.HasValue)
            {
                throw new InvalidActionFieldException(
                    $"[{addressesHex}] {nameof(RecipientInventoryAddr)} is required when " +
                    $"{nameof(FungibleIdAndCounts)} is set.");
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
                states = states.TransferAsset(garageBalanceAddress, balanceAddr, value);
            }

            return states;
        }

        private IAccountStateDelta TransferFungibleItems(
            Address signer,
            IAccountStateDelta states)
        {
            if (RecipientInventoryAddr is null ||
                FungibleIdAndCounts is null)
            {
                return states;
            }

            var inventory = states.GetInventory(RecipientInventoryAddr.Value);
            var fungibleItemTuples = GarageUtils.WithGarageTuples(
                signer,
                states,
                FungibleIdAndCounts);
            foreach (var (_, count, garageAddr, garage) in fungibleItemTuples)
            {
                garage.Add(-count);
                inventory.AddTradableFungibleItem(
                    (ITradableFungibleItem)garage.Item,
                    count);
                states = states.SetState(garageAddr, garage.Serialize());
            }

            return states.SetState(RecipientInventoryAddr.Value, inventory.Serialize());
        }
    }
}
