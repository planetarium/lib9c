namespace Lib9c.Tests.Model.Order
{
    using System;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class FungibleAssetValueOrderTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;

        public FungibleAssetValueOrderTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                _agentAddress,
                _avatarAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "name"
            );
            _currency = Currency.Legacy("NCG", 2, minters: null);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(InsufficientBalanceException))]
        public void Validate(Type exc)
        {
            var crystal = CrystalCalculator.CRYSTAL;
            var order = new FungibleAssetValueOrder(
                _agentAddress,
                _avatarAddress,
                Guid.NewGuid(),
                _currency * 1,
                Guid.NewGuid(),
                0L,
                crystal * 1
            );
            if (exc is null)
            {
                IAccountStateDelta states = new Tests.Action.State()
                    .MintAsset(_avatarAddress, order.Asset);
                order.Validate(states);
            }
            else
            {
                Assert.Throws(exc, () => order.Validate(new Tests.Action.State()));
            }
        }
    }
}
