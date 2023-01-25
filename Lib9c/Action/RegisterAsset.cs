using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_asset")]
    public class RegisterAsset : GameAction
    {
        public IEnumerable<AssetInfo> AssetInfoList;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (AssetInfoList.Select(r => r.AvatarAddress).Distinct().Count() != 1)
            {
                // 판매자는 동일해야함
                throw new Exception();
            }

            if (AssetInfoList.Any(r => r.Type != ProductType.FungibleAssetValue))
            {
                // 아이템은 별도 처리
                throw new Exception();
            }

            var avatarAddress = AssetInfoList.First().AvatarAddress;
            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }
            var productListAddress = ProductList.DeriveAddress(avatarAddress);
            ProductList productList;
            if (states.TryGetState(productListAddress, out List rawProductList))
            {
                productList = new ProductList(rawProductList);
            }
            else
            {
                productList = new ProductList();
                var marketState = states.TryGetState(Addresses.Market, out List rawMarketList)
                    ? new MarketState(rawMarketList)
                    : new MarketState();
                marketState.AvatarAddressList.Add(avatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }

            foreach (var registerInfo in AssetInfoList.OrderBy(a => a.Asset).ThenBy(a => a.Price))
            {
                Guid productId = context.Random.GenerateRandomGuid();
                Address productAddress = Product.DeriveAddress(productId);
                FungibleAssetValue asset = registerInfo.Asset;
                var product = new FavProduct
                {
                    ProductId = productId,
                    Price = registerInfo.Price,
                    Asset = asset,
                };
                states = states
                    .TransferAsset(avatarAddress, productAddress, asset)
                    .SetState(productAddress, product.Serialize());
                productList.ProductIdList.Add(productId);
            }

            states = states
                .SetState(avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productListAddress, productList.Serialize());
            if (migrationRequired)
            {
                states = states
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize());
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = new List(AssetInfoList.Select(a => a.Serialize()))
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AssetInfoList = plainValue["a"].ToList(des => new AssetInfo((List) des));
        }
    }
}
