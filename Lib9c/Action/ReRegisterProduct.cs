using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("re_register_item")]
    public class ReRegisterProduct : GameAction
    {
        public Address AvatarAddress;
        public List<(ProductInfo, IRegisterInfo)> ReRegisterInfoList;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (ReRegisterInfoList.Any(tuple =>
                    tuple.Item1.AvatarAddress != AvatarAddress ||
                    tuple.Item2.AvatarAddress != AvatarAddress))
            {
                throw new Exception();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            ProductsState productsState;
            if (states.TryGetState(productsStateAddress, out List rawProductList))
            {
                productsState = new ProductsState(rawProductList);
            }
            else
            {
                var marketState = states.TryGetState(Addresses.Market, out List rawMarketList)
                    ? new MarketState(rawMarketList)
                    : new MarketState();
                productsState = new ProductsState();
                marketState.AvatarAddresses.Add(AvatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }
            foreach (var (productInfo, info) in ReRegisterInfoList.OrderBy(tuple => tuple.Item2.Type).ThenBy(tuple => tuple.Item2.Price))
            {
                if (productInfo.Legacy)
                {
                    if (info.Type == ProductType.FungibleAssetValue)
                    {
                        // 잘못된 타입
                        throw new Exception();
                    }
                    var digestListAddress = OrderDigestListState.DeriveAddress(AvatarAddress);
                    var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                    if (!states.TryGetState(digestListAddress, out Dictionary rawList))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(OrderDigest)}({digestListAddress}).");
                    }
                    var digestList = new OrderDigestListState(rawList);
                    var orderAddress = Order.DeriveAddress(productInfo.ProductId);
                    if (!states.TryGetState(orderAddress, out Dictionary rawOrder))
                    {
                        throw new FailedLoadStateException(
                            $"{addressesHex} failed to load {nameof(Order)}({orderAddress}).");
                    }
                    var order = OrderFactory.Deserialize(rawOrder);
                    switch (order)
                    {
                        case FungibleOrder _:
                            if (info.Type == ProductType.NonFungible)
                            {
                                throw new Exception();
                            }
                            break;
                        case NonFungibleOrder _:
                            if (info.Type == ProductType.Fungible)
                            {
                                throw new Exception();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(order));
                    }
                    var updateSellInfo = new UpdateSellInfo(productInfo.ProductId, productInfo.ProductId, order.TradableId, order.ItemSubType, info.Price, 1);
                    states = UpdateSell.Cancel(states, updateSellInfo, addressesHex,
                        avatarState, digestList, context,
                        avatarState.address, new Stopwatch());
                }
                else
                {
                    states = CancelProductRegistration.Cancel(productsState, productInfo.ProductId,
                        states, avatarState, context.BlockIndex);
                }

                states = RegisterProduct.Register(context, info, avatarState, productsState, states);
            }

            states = states
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productsStateAddress, productsState.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize());
            }
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = new List(ReRegisterInfoList.Select(tuple =>
                    List.Empty.Add(tuple.Item1.Serialize()).Add(tuple.Item2.Serialize()))),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ReRegisterInfoList = new List<(ProductInfo, IRegisterInfo)>();
            var serialized = (List) plainValue["r"];
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                var registerList = (List) innerList[1];
                IRegisterInfo info =
                    registerList[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue
                        ? (IRegisterInfo) new AssetInfo(registerList)
                        : new RegisterInfo(registerList);
                ReRegisterInfoList.Add((new ProductInfo((List)innerList[0]), info));
            }
        }
    }
}
