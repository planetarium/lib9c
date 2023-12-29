namespace Lib9c.Tests.Action
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RenewAdminStateTest
    {
        private IAccount _stateDelta;
        private long _validUntil;
        private AdminState _adminState;
        private PrivateKey _adminPrivateKey;

        public RenewAdminStateTest()
        {
            _adminPrivateKey = new PrivateKey();
            _validUntil = 1_500_000L;
            _adminState = new AdminState(_adminPrivateKey.Address, _validUntil);
            _stateDelta = new Account(
                MockState.Empty
                    .SetState(Addresses.Admin, _adminState.Serialize()));
        }

        [Fact]
        public void Execute()
        {
            var newValidUntil = _validUntil + 1000;
            var action = new RenewAdminState(newValidUntil);
            var stateDelta = action.Execute(new ActionContext
            {
                PreviousState = _stateDelta,
                Signer = _adminPrivateKey.Address,
            });

            var adminState = new AdminState((Bencodex.Types.Dictionary)stateDelta.GetState(Addresses.Admin));
            Assert.Equal(newValidUntil, adminState.ValidUntil);
            Assert.NotEqual(_validUntil, adminState.ValidUntil);
        }

        [Fact]
        public void RejectSignerExceptAdminAddress()
        {
            var newValidUntil = _validUntil + 1000;
            var action = new RenewAdminState(newValidUntil);
            Assert.Throws<PermissionDeniedException>(() =>
            {
                var userPrivateKey = new PrivateKey();
                action.Execute(new ActionContext
                {
                    PreviousState = _stateDelta,
                    Signer = userPrivateKey.Address,
                });
            });
        }

        [Fact]
        public void RenewAdminStateEvenAlreadyExpired()
        {
            var newValidUntil = _validUntil + 1000;
            var action = new RenewAdminState(newValidUntil);
            var stateDelta = action.Execute(new ActionContext
            {
                BlockIndex = _validUntil + 1,
                PreviousState = _stateDelta,
                Signer = _adminPrivateKey.Address,
            });

            var adminState = new AdminState((Bencodex.Types.Dictionary)stateDelta.GetState(Addresses.Admin));
            Assert.Equal(newValidUntil, adminState.ValidUntil);
            Assert.NotEqual(_validUntil, adminState.ValidUntil);
        }

        [Fact]
        public void LoadPlainValue()
        {
            var action = new RenewAdminState(_validUntil);
            var newAction = new RenewAdminState();
            newAction.LoadPlainValue(action.PlainValue);

            Assert.True(newAction.PlainValue.Equals(action.PlainValue));
        }

        [Fact]
        public void CreatePendingActivationsAfterRenewAdminState()
        {
            var random = new Random();
            var nonce = new byte[40];
            random.NextBytes(nonce);
            var privateKey = new PrivateKey();

            var createPendingActivations = new CreatePendingActivations(new[]
            {
                new PendingActivationState(nonce, privateKey.PublicKey),
            });

            long blockIndex = _validUntil + 1;
            Assert.Throws<PolicyExpiredException>(() => createPendingActivations.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = _stateDelta,
                Signer = _adminPrivateKey.Address,
            }));

            var newValidUntil = _validUntil + 1000;
            var action = new RenewAdminState(newValidUntil);
            var stateDelta = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = _stateDelta,
                Signer = _adminPrivateKey.Address,
            });

            // After 100 blocks.
            blockIndex += 100;

            Assert.True(blockIndex < newValidUntil);
            stateDelta = createPendingActivations.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = stateDelta,
                Signer = _adminPrivateKey.Address,
            });

            Address expectedPendingActivationStateAddress =
                PendingActivationState.DeriveAddress(nonce, privateKey.PublicKey);
            Assert.NotNull(stateDelta.GetState(expectedPendingActivationStateAddress));
        }
    }
}
