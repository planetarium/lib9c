using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Battle;
using Lib9c.Helper;
using Lib9c.Model.Market;
using Lib9c.Model.Order;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Action
{
    [ActionType("re_register_product2")]
    public class ReRegisterProduct : GameAction
    {
        public const int CostAp = 5;
        public const int Capacity = 100;
        public Address AvatarAddress;
        public List<(IProductInfo, IRegisterInfo)> ReRegisterInfos;
        public bool ChargeAp;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IWorld states = context.PreviousState;

            if (!ReRegisterInfos.Any())
            {
                throw new ListEmptyException($"ReRegisterInfos was empty.");
            }

            if (ReRegisterInfos.Count > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(ReRegisterInfos)} must be less than or equal {Capacity}.");
            }

            var ncg = states.GetGoldCurrency();
            foreach (var (productInfo, registerInfo) in ReRegisterInfos)
            {
                registerInfo.ValidateAddress(AvatarAddress);
                registerInfo.ValidatePrice(ncg);
                registerInfo.Validate();
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
                var marketState = states.TryGetLegacyState(Addresses.Market, out List rawMarketList)
                    ? rawMarketList
                    : List.Empty;
                productsState = new ProductsState();
                marketState = marketState.Add(AvatarAddress.Serialize());
                states = states.SetLegacyState(Addresses.Market, marketState);
            }

            var random = context.GetRandom();
            foreach (var (productInfo, info) in ReRegisterInfos.OrderBy(tuple => tuple.Item2.Type).ThenBy(tuple => tuple.Item2.Price))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                if (productInfo is ItemProductInfo {Legacy: true})
                {
                    // if product is order. it move to products state from sharded shop state.
                    var productType = productInfo.Type;
                    var avatarAddress = avatarState.address;
                    if (productType == ProductType.FungibleAssetValue)
                    {
                        // 잘못된 타입
                        throw new InvalidProductTypeException(
                            $"Order not support {productType}");
                    }

                    var digestListAddress =
                        OrderDigestListState.DeriveAddress(avatarAddress);
                    if (!states.TryGetLegacyState(digestListAddress, out Dictionary rawList))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(OrderDigest)}({digestListAddress}).");
                    }

                    var digestList = new OrderDigestListState(rawList);
                    var orderAddress = Order.DeriveAddress(productInfo.ProductId);
                    if (!states.TryGetLegacyState(orderAddress, out Dictionary rawOrder))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(Order)}({orderAddress}).");
                    }

                    var order = OrderFactory.Deserialize(rawOrder);
                    var itemCount = 1;
                    switch (order)
                    {
                        case FungibleOrder fungibleOrder:
                            itemCount = fungibleOrder.ItemCount;
                            if (productInfo.Type == ProductType.NonFungible)
                            {
                                throw new InvalidProductTypeException(
                                    $"FungibleOrder not support {productType}");
                            }

                            break;
                        case NonFungibleOrder _:
                            if (productInfo.Type == ProductType.Fungible)
                            {
                                throw new InvalidProductTypeException(
                                    $"NoneFungibleOrder not support {productType}");
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(order));
                    }

                    if (order.SellerAvatarAddress != avatarAddress ||
                        order.SellerAgentAddress != context.Signer)
                    {
                        throw new InvalidAddressException();
                    }

                    if (!order.Price.Equals(productInfo.Price))
                    {
                        throw new InvalidPriceException($"order price does not match information. expected: {order.Price} actual: {productInfo.Price}");
                    }

                    var updateSellInfo = new UpdateSellInfo(productInfo.ProductId,
                        productInfo.ProductId, order.TradableId,
                        order.ItemSubType, productInfo.Price, itemCount);
                    states = UpdateSell.Cancel(states, updateSellInfo, addressesHex,
                        avatarState, digestList, context,
                        avatarState.address);
                }
                else
                {
                    states = CancelProductRegistration.Cancel(productsState, productInfo,
                        states, avatarState, context);
                }

                states = RegisterProduct.Register(context, info, avatarState, productsState, states, random);
            }

            states = states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetActionPoint(AvatarAddress, resultActionPoint)
                .SetLegacyState(productsStateAddress, productsState.Serialize());

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = new List(ReRegisterInfos.Select(tuple =>
                    List.Empty.Add(tuple.Item1.Serialize()).Add(tuple.Item2.Serialize()))),
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>();
            var serialized = (List) plainValue["r"];
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                var productList = (List) innerList[0];
                var registerList = (List) innerList[1];
                IRegisterInfo info = ProductFactory.DeserializeRegisterInfo(registerList);
                IProductInfo productInfo = ProductFactory.DeserializeProductInfo(productList);
                ReRegisterInfos.Add((productInfo, info));
            }

            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
