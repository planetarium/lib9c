using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("cancel_product_registration2")]
    public class CancelProductRegistration : GameAction
    {
        public const int CostAp = 5;
        public const int Capacity = 100;
        public Address AvatarAddress;
        public List<IProductInfo> ProductInfos;
        public bool ChargeAp;
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IWorld states = context.PreviousState;

            if (!ProductInfos.Any())
            {
                throw new ListEmptyException("ProductInfos was empty.");
            }

            if (ProductInfos.Count > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(ProductInfos)} must be less than or equal {Capacity}.");
            }

            foreach (var productInfo in ProductInfos)
            {
                productInfo.ValidateType();
                if (productInfo.AvatarAddress != AvatarAddress ||
                    productInfo.AgentAddress != context.Signer)
                {
                    throw new InvalidAddressException();
                }
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException("failed to load avatar state");
            }

            if (!states.TryGetActionPoint(AvatarAddress, out var actionPoint))
            {
                actionPoint = avatarState.actionPoint;
            }

            var resultActionPoint = avatarState.inventory.UseActionPoint(actionPoint,
                CostAp,
                ChargeAp,
                states.GetSheet<MaterialItemSheet>(),
                context.BlockIndex);
            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            ProductsState productsState;
            if (states.TryGetLegacyState(productsStateAddress, out List rawProductList))
            {
                productsState = new ProductsState(rawProductList);
            }
            else
            {
                // cancel order before product registered case.
                var marketState = states.TryGetLegacyState(Addresses.Market, out List rawMarketList)
                    ? rawMarketList
                    : List.Empty;
                productsState = new ProductsState();
                marketState = marketState.Add(AvatarAddress.Serialize());
                states = states.SetLegacyState(Addresses.Market, marketState);
            }
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            foreach (var productInfo in ProductInfos)
            {
                if (productInfo is ItemProductInfo {Legacy: true})
                {
                    var productType = productInfo.Type;
                    var orderAddress = Order.DeriveAddress(productInfo.ProductId);
                    if (!states.TryGetLegacyState(orderAddress, out Dictionary rawOrder))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(Order)}({orderAddress}).");
                    }

                    var order = OrderFactory.Deserialize(rawOrder);
                    switch (order)
                    {
                        case FungibleOrder _:
                            if (productInfo.Type == ProductType.NonFungible)
                            {
                                throw new InvalidProductTypeException($"FungibleOrder not support {productType}");
                            }

                            break;
                        case NonFungibleOrder _:
                            if (productInfo.Type == ProductType.Fungible)
                            {
                                throw new InvalidProductTypeException($"NoneFungibleOrder not support {productType}");
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(order));
                    }

                    states = SellCancellation.Cancel(context, states, avatarState, addressesHex, order);
                }
                else
                {
                    states = Cancel(productsState, productInfo, states, avatarState, context);
                }
            }

            states = states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetActionPoint(AvatarAddress, resultActionPoint)
                .SetLegacyState(productsStateAddress, productsState.Serialize());

            return states;
        }

        public static IWorld Cancel(
            ProductsState productsState,
            IProductInfo productInfo,
            IWorld states,
            AvatarState avatarState,
            IActionContext context)
        {
            var productId = productInfo.ProductId;
            if (!productsState.ProductIds.Contains(productId))
            {
                throw new ProductNotFoundException($"can't find product {productId}");
            }

            productsState.ProductIds.Remove(productId);

            var productAddress = Product.DeriveAddress(productId);
            var product = ProductFactory.DeserializeProduct((List) states.GetLegacyState(productAddress));
            product.Validate(productInfo);

            switch (product)
            {
                case FavProduct favProduct:
                    states = states.TransferAsset(context, productAddress, avatarState.address,
                        favProduct.Asset);
                    break;
                case ItemProduct itemProduct:
                    switch (itemProduct.TradableItem)
                    {
                        case Costume costume:
                            avatarState.UpdateFromAddCostume(costume, true);
                            break;
                        case ItemUsable itemUsable:
                            avatarState.UpdateFromAddItem(itemUsable, true);
                            break;
                        case TradableMaterial tradableMaterial:
                        {
                            avatarState.UpdateFromAddItem(tradableMaterial, itemProduct.ItemCount, true);
                            break;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product));
            }

            var mail = new ProductCancelMail(context.BlockIndex, productId, context.BlockIndex, productId, product);
            avatarState.Update(mail);
            states = states.RemoveLegacyState(productAddress);
            return states;
        }


        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = new List(ProductInfos.Select(p => p.Serialize())),
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ProductInfos = plainValue["p"].ToList(s => ProductFactory.DeserializeProductInfo((List) s));
            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
