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
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                account = account
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetCouponWallet(
                        context.Signer,
                        ImmutableDictionary.Create<Guid, Coupon>(),
                        rehearsal: true);
                return world.SetAccount(account);
            }

            if (!account.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out AvatarState avatarState,
                    out _))
            {
                return world;
            }

            var wallet = account.GetCouponWallet(context.Signer);
            if (!wallet.TryGetValue(CouponId, out var coupon))
            {
                return world;
            }

            wallet = wallet.Remove(CouponId);
            var itemSheets = account.GetItemSheet();
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

            account = account
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetCouponWallet(context.Signer, wallet);
            return world.SetAccount(account);
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
