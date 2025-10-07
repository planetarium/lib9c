#nullable enable
using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.Module;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Action.Coupons
{
    [Serializable]
    [ActionType("redeem_coupon")]
    public sealed class RedeemCoupon : GameAction
    {
        public Guid CouponId { get; private set; }
        public Address AvatarAddress { get; private set; }

        public RedeemCoupon()
        {
        }

        public RedeemCoupon(Guid couponId, Address avatarAddress)
        {
            CouponId = couponId;
            AvatarAddress = avatarAddress;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            if (!states.TryGetAvatarState(
                    context.Signer,
                    AvatarAddress,
                    out AvatarState avatarState))
            {
                return states;
            }

            var wallet = states.GetCouponWallet(context.Signer);
            if (!wallet.TryGetValue(CouponId, out var coupon))
            {
                return states;
            }

            wallet = wallet.Remove(CouponId);
            var itemSheets = states.GetItemSheet();
            var random = context.GetRandom();
            foreach ((int itemId, uint q) in coupon)
            {
                for (uint i = 0U; i < q; i++)
                {
                    ItemBase item = ItemFactory.CreateItem(itemSheets[itemId], random);
                    // XXX: Inventory.AddItem() method silently ignores count if the item is
                    // non-fungible.
                    avatarState.inventory.AddItem(item, count: 1);
                }
            }

            return states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetCouponWallet(context.Signer, wallet);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("coupon_id", new Binary(CouponId.ToByteArray()))
                .Add("avatar_address", new Binary(AvatarAddress.ByteArray));

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            CouponId = new Guid(((Binary)plainValue["coupon_id"]).ToByteArray());
            AvatarAddress = new Address(plainValue["avatar_address"]);
        }
    }
}
