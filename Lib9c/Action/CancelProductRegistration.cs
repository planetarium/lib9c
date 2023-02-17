using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("cancel_product_registration")]
    public class CancelProductRegistration : GameAction
    {
        public Address AvatarAddress;
        public List<ProductInfo> ProductInfoList;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            // 주소 검증
            if (ProductInfoList.Any(p => p.AvatarAddress != AvatarAddress) ||
                ProductInfoList.Any(p => p.AgentAddress != context.Signer))
            {
                throw new Exception();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            var productsState = new ProductsState((List) states.GetState(productsStateAddress));
            foreach (var productId in ProductInfoList.Select(productInfo => productInfo.ProductId))
            {
                states = Cancel(productsState, productId, states, avatarState);
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

        public static IAccountStateDelta Cancel(ProductsState productsState, Guid productId, IAccountStateDelta states,
            AvatarState avatarState)
        {
            if (!productsState.ProductIds.Contains(productId))
            {
                throw new Exception();
            }

            productsState.ProductIds.Remove(productId);

            var productAddress = Product.DeriveAddress(productId);
            var product = ProductFactory.Deserialize((List) states.GetState(productAddress));
            if (product.SellerAgentAddress != avatarState.agentAddress || product.SellerAvatarAddress != avatarState.address)
            {
                throw new Exception();
            }

            switch (product)
            {
                case FavProduct favProduct:
                    states = states.TransferAsset(productAddress, avatarState.address,
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

            states = states.SetState(productAddress, Null.Value);
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
