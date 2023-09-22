#nullable enable
using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action.Coupons
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
            context.UseGas(1);
            var world = context.PreviousState;
            if (context.Rehearsal)
            {
                world = AvatarModule.MarkChanged(world, AvatarAddress, true, true, true, true);
                world = LegacyModule.SetCouponWallet(
                    world,
                    context.Signer,
                    ImmutableDictionary.Create<Guid, Coupon>(),
                    rehearsal: true);
                return world;
            }

            if (!AvatarModule.TryGetAvatarState(
                    world,
                    context.Signer,
                    AvatarAddress,
                    out AvatarState avatarState))
            {
                return world;
            }

            var wallet = LegacyModule.GetCouponWallet(world, context.Signer);
            if (!wallet.TryGetValue(CouponId, out var coupon))
            {
                return world;
            }

            wallet = wallet.Remove(CouponId);
            var itemSheets = LegacyModule.GetItemSheet(world);
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

            world = AvatarModule.SetAvatarState(
                world,
                AvatarAddress,
                avatarState,
                true,
                true,
                true,
                true);
            world = LegacyModule.SetCouponWallet(world, context.Signer, wallet);
            return world;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("coupon_id", new Binary(CouponId.ToByteArray()))
                .Add("avatar_address", new Binary(AvatarAddress.ByteArray));

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            CouponId = new Guid((Binary)plainValue["coupon_id"]);
            AvatarAddress = new Address(plainValue["avatar_address"]);
        }
    }
}
