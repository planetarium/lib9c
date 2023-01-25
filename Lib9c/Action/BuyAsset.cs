using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("buy_asset")]
    public class BuyAsset : GameAction
    {
        public Address AvatarAddress;
        public IEnumerable<ProductInfo> ProductInfoList;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("BuyItem exec started");

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState, out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            foreach (var productInfo in ProductInfoList.OrderBy(p => p.ProductId).ThenBy(p =>p.Price))
            {
                var sellerAgentAddress = productInfo.AgentAddress;
                var sellerAvatarAddress = productInfo.AvatarAddress;
                var sellerAgentState = states.GetAgentState(sellerAgentAddress);
                if (!sellerAgentState.avatarAddresses.Values.Contains(sellerAvatarAddress))
                {
                    throw new InvalidAddressException();
                }
                var productId = productInfo.ProductId;
                var productListAddress = ProductList.DeriveAddress(sellerAvatarAddress);
                var productList = new ProductList((List)states.GetState(productListAddress));
                if (!productList.ProductIdList.Contains(productId))
                {
                    // 이미 팔렸거나 잘못된 건
                    throw new Exception();
                }

                productList.ProductIdList.Remove(productId);

                var productAddress = Product.DeriveAddress(productId);
                var product = new FavProduct((List) states.GetState(productAddress));
                FungibleAssetValue asset = product.Asset;

                states = states
                    .SetState(productAddress, Null.Value)
                    .SetState(productListAddress, productList.Serialize())
                    .TransferAsset(context.Signer, sellerAgentAddress, product.Price)
                    .TransferAsset(productAddress, AvatarAddress, asset);
            }

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize());
            }

            states = states.SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize());
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("BuyAsset Total Executed Time: {Elapsed}", ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = new List(ProductInfoList.Select(p => p.Serialize())),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ProductInfoList = plainValue["p"].ToList(s => new ProductInfo((List) s));
        }
    }
}
