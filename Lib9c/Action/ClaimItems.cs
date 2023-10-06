using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Extensions;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType(ActionTypeText)]
    public class ClaimItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "claim_items";

        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; private set; }

        public ClaimItems()
        {
        }

        public ClaimItems(IReadOnlyList<(Address, IReadOnlyList<FungibleAssetValue>)> claimData)
        {
            ClaimData = claimData;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(ClaimDataKey, ClaimData.Aggregate(List.Empty, (list, tuple) =>
                {
                    var serializedFungibleAssetValues = tuple.fungibleAssetValues.Select(x => x.Serialize()).Serialize();

                    return list.Add(new List(tuple.address.Bencoded, serializedFungibleAssetValues));
                }));

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            ClaimData = ((List)plainValue[ClaimDataKey])
                .Select(pairValue =>
                {
                    List pair = (List)pairValue;
                    return (
                        new Address(pair[0]),
                        pair[1].ToList(x => x.ToFungibleAssetValue()) as IReadOnlyList<FungibleAssetValue>);
                }).ToList();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);

            var states = context.PreviousState;
            var random = context.GetRandom();
            var itemSheet = LegacyModule.GetSheets(states, containItemSheet: true).GetItemSheet();

            foreach (var (avatarAddress, fungibleAssetValues) in ClaimData)
            {
                var inventory = AvatarModule.GetInventory(states, avatarAddress)
                                ?? throw new FailedLoadStateException(
                                    ActionTypeText,
                                    GetSignerAndOtherAddressesHex(context, avatarAddress),
                                    typeof(Inventory),
                                    avatarAddress);

                foreach (var fungibleAssetValue in fungibleAssetValues)
                {
                    if (fungibleAssetValue.Currency.DecimalPlaces != 0)
                    {
                        throw new ArgumentException(
                            $"DecimalPlaces of fungibleAssetValue for claimItems are not 0: {fungibleAssetValue.Currency.Ticker}");
                    }

                    var parsedTicker = fungibleAssetValue.Currency.Ticker.Split("_");
                    if (parsedTicker.Length != 3
                        || parsedTicker[0] != "Item"
                        || (parsedTicker[1] != "NT" && parsedTicker[1] != "T")
                        || !int.TryParse(parsedTicker[2], out var itemId))
                    {
                        throw new ArgumentException(
                            $"Format of Amount currency's ticker is invalid");
                    }

                    states = LegacyModule.BurnAsset(states, context, context.Signer, fungibleAssetValue);

                    var item = itemSheet[itemId] switch
                    {
                        MaterialItemSheet.Row materialRow => parsedTicker[1] == "T"
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateMaterial(materialRow),
                        var itemRow => ItemFactory.CreateItem(itemRow, random)
                    };

                    inventory.AddItem(item, (int)fungibleAssetValue.RawValue);
                }

                states = AvatarModule.SetInventory(states, avatarAddress, inventory);
            }

            return states;
        }
    }
}
