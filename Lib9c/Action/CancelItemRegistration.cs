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
    [ActionType("cancel_item_registration")]
    public class CancelItemRegistration : GameAction
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

            var productListAddress = ProductList.DeriveAddress(AvatarAddress);
            var productList = new ProductList((List) states.GetState(productListAddress));
            foreach (var productId in ProductInfoList.Select(productInfo => productInfo.ProductId))
            {
                if (!productList.ProductIdList.Contains(productId))
                {
                    throw new Exception();
                }

                productList.ProductIdList.Remove(productId);

                var productAddress = Product.DeriveAddress(productId);
                var product = new ItemProduct((List) states.GetState(productAddress));
                switch (product.TradableItem)
                {
                    case Costume costume:
                        avatarState.UpdateFromAddCostume(costume, true);
                        break;
                    case ItemUsable itemUsable:
                        avatarState.UpdateFromAddItem(itemUsable, true);
                        break;
                    case TradableMaterial tradableMaterial:
                    {
                        avatarState.UpdateFromAddItem(tradableMaterial, product.ItemCount, true);
                        break;
                    }
                }
                states = states.SetState(productAddress, Null.Value);
            }

            states = states
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productListAddress, productList.Serialize());

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
                ["p"] = new List(ProductInfoList.Select(p => p.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ProductInfoList = plainValue["p"].ToList(s => new ProductInfo((List) s));
        }
    }
}
