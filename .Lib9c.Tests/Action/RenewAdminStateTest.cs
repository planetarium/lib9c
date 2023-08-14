namespace Lib9c.Tests.Action
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class RenewAdminStateTest
    {
        private IWorld _stateDelta;
        private long _validUntil;
        private AdminState _adminState;
        private PrivateKey _adminPrivateKey;

        public RenewAdminStateTest()
        {
            _adminPrivateKey = new PrivateKey();
            _validUntil = 1_500_000L;
            _adminState = new AdminState(_adminPrivateKey.ToAddress(), _validUntil);
            _stateDelta = new MockWorld();
            _stateDelta = LegacyModule.SetState(_stateDelta, Addresses.Admin, _adminState.Serialize());
        }

        [Fact]
        public void Execute()
        {
            var newValidUntil = _validUntil + 1000;
            var action = new RenewAdminState(newValidUntil);
            var stateDelta = action.Execute(new ActionContext
            {
                PreviousState = _stateDelta,
                Signer = _adminPrivateKey.ToAddress(),
            }).GetAccount(ReservedAddresses.LegacyAccount);

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
                    Signer = userPrivateKey.ToAddress(),
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
                Signer = _adminPrivateKey.ToAddress(),
            }).GetAccount(ReservedAddresses.LegacyAccount);

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
    }
}
