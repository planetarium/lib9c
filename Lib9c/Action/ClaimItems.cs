using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Helper;
using Lib9c.Model.Mail;
using Lib9c.Model.State;
using Lib9c.Module;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using static Lib9c.SerializeKeys;

namespace Lib9c.Action
{
    [ActionType(ActionTypeText)]
    public class ClaimItems : GameAction, IClaimItems
    {
        private const string ActionTypeText = "claim_items";
        private const int MaxClaimDataCount = 100;

        public IReadOnlyList<(Address address, IReadOnlyList<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; private set; }
        public string Memo;

        public ClaimItems()
        {
        }

        public ClaimItems(IReadOnlyList<(Address, IReadOnlyList<FungibleAssetValue>)> claimData, string memo = null)
        {
            ClaimData = claimData;
            Memo = memo;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            GetPlainValueInternal();

        private IImmutableDictionary<string, IValue> GetPlainValueInternal()
        {
            var dict = ImmutableDictionary<string, IValue>.Empty
                .Add(ClaimDataKey, ClaimData.Aggregate(List.Empty, (list, tuple) =>
                {
                    var serializedFungibleAssetValues = tuple.fungibleAssetValues.Select(x => x.Serialize()).Serialize();

                    return list.Add(new List(tuple.address.Bencoded, serializedFungibleAssetValues));
                }));
            if (!string.IsNullOrEmpty(Memo))
            {
                dict = dict.Add(MemoKey, Memo.Serialize());
            }

            return dict;
        }

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
            if (plainValue.ContainsKey(MemoKey))
            {
                if (plainValue[MemoKey] is Text t && !string.IsNullOrEmpty(t))
                {
                    Memo = t;
                }
                else
                {
                    throw new ArgumentException(nameof(PlainValue));
                }
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            if (ClaimData.Count > MaxClaimDataCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ClaimData),
                    ClaimData.Count,
                    $"ClaimData should be less than {MaxClaimDataCount}");
            }

            var states = context.PreviousState;
            var random = context.GetRandom();
            var itemSheet = states.GetSheets(containItemSheet: true).GetItemSheet();

            foreach (var (avatarAddress, fungibleAssetValues) in ClaimData)
            {
                var avatarState = states.GetAvatarState(avatarAddress, getQuestList: false, getWorldInformation: false)
                            ?? throw new FailedLoadStateException(
                                ActionTypeText,
                                GetSignerAndOtherAddressesHex(context, avatarAddress),
                                typeof(AvatarState),
                                avatarAddress);

                var favs = new List<FungibleAssetValue>();
                var items = new List<(int id, int count)>();
                foreach (var fungibleAssetValue in fungibleAssetValues)
                {
                    var tokenCurrency = fungibleAssetValue.Currency;
                    if (Currencies.IsWrappedCurrency(tokenCurrency))
                    {
                        var currency = Currencies.GetUnwrappedCurrency(tokenCurrency);
                        var recipientAddress =
                            Currencies.PickAddress(currency, avatarState.agentAddress,
                                avatarAddress);
                        var fav = FungibleAssetValue.FromRawValue(currency, fungibleAssetValue.RawValue);
                        states = states
                            .BurnAsset(context, context.Signer, fungibleAssetValue)
                            .MintAsset(context, recipientAddress, fav);
                        favs.Add(fav);
                    }
                    else
                    {
                        (bool tradable, int itemId) = Currencies.ParseItemCurrency(tokenCurrency);
                        states = states.BurnAsset(context, context.Signer, fungibleAssetValue);

                        // FIXME: This is an implementation bug in the Inventory class,
                        // but we'll deal with it temporarily here.
                        // If Pluggable AEV ever becomes a reality,
                        // it's only right that this is fixed in Inventory.
                        var itemRow = itemSheet[itemId];
                        var itemCount = (int)fungibleAssetValue.RawValue;
                        avatarState.inventory.MintItem(itemRow, itemCount, tradable, random);
                        items.Add((itemRow.Id, itemCount));
                    }
                }

                var mailBox = avatarState.mailBox;
                var mail = new ClaimItemsMail(context.BlockIndex, random.GenerateRandomGuid(), context.BlockIndex, favs, items, Memo);
                mailBox.Add(mail);
                mailBox.CleanUp();
                avatarState.mailBox = mailBox;
                states = states.SetAvatarState(avatarAddress, avatarState, setWorldInformation: false, setQuestList: false);
            }

            return states;
        }
    }
}
