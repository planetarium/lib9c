#nullable enable
using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using static Lib9c.SerializeKeys;

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
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, AvatarAddress, MarkChanged);
                world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);
                world = LegacyModule.SetState(world, worldInformationAddress, MarkChanged);
                world = LegacyModule.SetState(world, questListAddress, MarkChanged);
                world = LegacyModule.SetCouponWallet(
                    world,
                    context.Signer,
                    ImmutableDictionary.Create<Guid, Coupon>(),
                    rehearsal: true);
                return world;
            }

            if (!AvatarModule.TryGetAvatarStateV2(
                    world,
                    context.Signer,
                    AvatarAddress,
                    out AvatarState avatarState,
                    out _))
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
            foreach ((int itemId, uint q) in coupon)
            {
                for (uint i = 0U; i < q; i++)
                {
                    ItemBase item = ItemFactory.CreateItem(itemSheets[itemId], context.Random);
                    // XXX: Inventory.AddItem() method silently ignores count if the item is
                    // non-fungible.
                    avatarState.inventory.AddItem(item, count: 1);
                }
            }

            world = AvatarModule.SetAvatarStateV2(world, AvatarAddress, avatarState);
            world = LegacyModule.SetState(
                world,
                inventoryAddress,
                avatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                questListAddress,
                avatarState.questList.Serialize());
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
