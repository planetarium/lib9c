using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;

namespace Nekoyume.Action.Coupons
{
    [Serializable]
    [ActionType("transfer_coupons")]
    public sealed class TransferCoupons : GameAction
    {
        public TransferCoupons()
        {
        }

        public TransferCoupons(
            IImmutableDictionary<Address, IImmutableSet<Guid>> couponsPerRecipient)
        {
            CouponsPerRecipient = couponsPerRecipient;
        }

        public IImmutableDictionary<Address, IImmutableSet<Guid>> CouponsPerRecipient
        {
            get;
            private set;
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var signerWallet = account.GetCouponWallet(context.Signer);
            var orderedRecipients = CouponsPerRecipient.OrderBy(pair => pair.Key);
            foreach ((Address recipient, IImmutableSet<Guid> couponIds) in orderedRecipients)
            {
                if (recipient == context.Signer)
                {
                    continue;
                }

                var recipientWallet = account.GetCouponWallet(recipient);
                foreach (Guid id in couponIds)
                {
                    if (!signerWallet.TryGetValue(id, out var coupon))
                    {
                        throw new FailedLoadStateException(
                            $"Failed to load a coupon (id: {id})."
                        );
                    }

                    signerWallet = signerWallet.Remove(id);
                    recipientWallet = recipientWallet.Add(id, coupon);
                }

                account = account.SetCouponWallet(recipient, recipientWallet, context.Rehearsal);
            }

            account = account.SetCouponWallet(context.Signer, signerWallet, context.Rehearsal);
            return world.SetAccount(account);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(
                    "couponsPerRecipient",
                    new Bencodex.Types.Dictionary(
                        CouponsPerRecipient.ToImmutableDictionary(
                            pair => new Binary(pair.Key.ByteArray),
                pair => (IValue)new Bencodex.Types.List(
                    pair.Value.OrderBy(id => id).Select(id => id.ToByteArray())))));

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue) =>
            CouponsPerRecipient = ((Bencodex.Types.Dictionary)plainValue["couponsPerRecipient"])
                .ToImmutableDictionary(
                    pair => new Address(pair.Key),
                    pair => (IImmutableSet<Guid>)((Bencodex.Types.List)pair.Value).Select(
                        value => new Guid((Binary)value)
                        ).ToImmutableHashSet()
            );
    }
}
