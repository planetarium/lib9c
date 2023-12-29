namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class ActivateAccountTest
    {
        [Theory]
        [InlineData(false, true, false, null)]
        [InlineData(true, true, false, typeof(InvalidSignatureException))]
        [InlineData(false, false, false, typeof(PendingActivationDoesNotExistsException))]
        [InlineData(false, true, true, typeof(AlreadyActivatedException))]
        public void Execute(bool invalid, bool pendingExist, bool alreadyActivated, Type exc)
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            Address activatedAddress = default(Address).Derive(ActivationKey.DeriveKey);
            IAccount state = new Account(MockState.Empty);

            if (pendingExist)
            {
                state = state.SetState(pendingActivation.address, pendingActivation.Serialize());
            }

            if (alreadyActivated)
            {
                state = state.SetState(activatedAddress, true.Serialize());
            }

            ActivateAccount action = activationKey.CreateActivateAccount(invalid ? new byte[] { 0x00 } : nonce);

            if (exc is null)
            {
                IAccount nextState = action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = default,
                    BlockIndex = 1,
                });

                Assert.Equal(Null.Value, nextState.GetState(pendingActivation.address));
                Assert.True(nextState.GetState(activatedAddress).ToBoolean());
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = default,
                    BlockIndex = 1,
                }));
            }
        }

        [Fact]
        public void PlainValue()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);

            var action2 = new ActivateAccount();
            action2.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.Signature, action2.Signature);
            Assert.Equal(action.PendingAddress, action2.PendingAddress);
        }
    }
}
