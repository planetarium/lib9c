namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class FaucetAssetTest
    {
        [Theory]
        [InlineData(null, 2L, 2L, true, false, true, false, false, true)]
        [InlineData(null, 1L, 2L, true, false, true, false, false, false)]
        [InlineData(typeof(ActionObsoletedException), 1_500_001L, 1_500_002L, true, false, true, false, false, true)]
        [InlineData(typeof(PolicyExpiredException), 2L, 1L, true, false, true, false, false, true)]
        [InlineData(typeof(PermissionDeniedException), 1L, 2L, false, false, true, false, false, true)]
        [InlineData(typeof(InvalidTransferRecipientException), 2L, 2L, true, true, true, false, false, true)]
        [InlineData(typeof(InvalidTransferUnactivatedRecipientException), 1L, 2L, true, false, false, false, false, true)]
        [InlineData(typeof(InvalidTransferMinterException), 1L, 2L, true, false, true, true, false, true)]
        [InlineData(typeof(InvalidTransferMinterException), 1L, 2L, true, false, true, true, true, true)]
        [InlineData(typeof(InvalidTransferMinterException), 1L, 2L, true, false, true, false, true, true)]
        public void Execute(Type exc, long blockIndex, long validUntil, bool admin, bool sameRecipient, bool activated, bool containSender, bool containRecipient, bool adminExist)
        {
            var sender = new PrivateKey().ToAddress();
            var recipient = sameRecipient ? sender : new PrivateKey().ToAddress();
            IImmutableSet<Address> minters = ImmutableHashSet<Address>.Empty;
            if (containSender)
            {
                minters = minters.Add(sender);
            }

            if (containRecipient)
            {
                minters = minters.Add(recipient);
            }

            if (!containSender && !containRecipient)
            {
                minters = null;
            }

            var currency = new Currency("NCG", 2, minters: minters);
            FungibleAssetValue amount = 100 * currency;
            var adminAddress = admin ? sender : new PrivateKey().ToAddress();
            var states = new State()
                .MintAsset(sender, amount)
                .SetState(Addresses.ActivatedAccount, new ActivatedAccountsState(ImmutableHashSet<Address>.Empty.Add(sender)).Serialize());

            if (activated)
            {
                states = states.SetState(recipient.Derive(ActivationKey.DeriveKey), true.Serialize());
            }

            if (adminExist)
            {
                states = states.SetState(Addresses.Admin, new AdminState(adminAddress, validUntil).Serialize());
            }

            var action = new FaucetAsset(sender, recipient, amount);
            var context = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = states,
                Signer = sender,
            };

            if (exc is null)
            {
                IAccountStateDelta nextState = action.Execute(context);
                if (adminExist)
                {
                    Assert.Equal(0 * currency, nextState.GetBalance(sender, currency));
                    Assert.Equal(amount, nextState.GetBalance(recipient, currency));
                }
                else
                {
                    Assert.Equal(amount, nextState.GetBalance(sender, currency));
                    Assert.Equal(0 * currency, nextState.GetBalance(recipient, currency));
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(context));
            }
        }
    }
}
