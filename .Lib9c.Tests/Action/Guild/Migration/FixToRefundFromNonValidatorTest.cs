namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    // TODO: Remove this test class after the migration is completed.
    public class FixToRefundFromNonValidatorTest : GuildTestBase
    {
        private IWorld _world;
        private Address _adminAddress;

        public FixToRefundFromNonValidatorTest()
        {
            _adminAddress = new PrivateKey().Address;
            var adminState = new AdminState(_adminAddress, 100L);
            _world = World
                .SetLegacyState(Addresses.Admin, adminState.Serialize())
                .MintAsset(new ActionContext { }, Addresses.NonValidatorDelegatee, Currencies.GuildGold * 500);
        }

        [Fact]
        public void PlainValue()
        {
            var addresses = Enumerable.Range(0, 5).Select(_ => new PrivateKey().Address).ToList();
            var amounts = Enumerable.Range(0, 5).Select(i => (i + 1) * 10).ToList();

            var plainValue = new FixToRefundFromNonValidator(
                addresses,
                amounts
            ).PlainValue;

            var recon = new FixToRefundFromNonValidator();
            recon.LoadPlainValue(plainValue);
            Assert.Equal(addresses, recon.Targets);
            Assert.Equal(amounts, recon.Amounts);
        }

        [Fact]
        public void Execute()
        {
            var addresses = Enumerable.Range(0, 5).Select(_ => new PrivateKey().Address).ToList();
            var amounts = Enumerable.Range(0, 5).Select(i => (i + 1) * 10).ToList();

            var world = new FixToRefundFromNonValidator(
                addresses,
                amounts
            ).Execute(new ActionContext
            {
                PreviousState = _world,
                Signer = _adminAddress,
                BlockIndex = 2L,
            });

            foreach (var item in addresses.Select((a, i) => (a, i)))
            {
                Assert.Equal(
                    Currencies.GuildGold * ((item.i + 1) * 10),
                    world.GetBalance(StakeState.DeriveAddress(item.a), Currencies.GuildGold));
            }

            Assert.Equal(
                Currencies.GuildGold * (500 - amounts.Sum()),
                world.GetBalance(Addresses.NonValidatorDelegatee, Currencies.GuildGold));
        }

        [Fact]
        public void AssertWhenDifferentLengthArgument()
        {
            var addresses = Enumerable.Range(0, 5).Select(_ => new PrivateKey().Address).ToList();
            var amounts = Enumerable.Range(0, 4).Select(i => (i + 1) * 10).ToList();

            Assert.Throws<ArgumentException>(() =>
            {
                new FixToRefundFromNonValidator(addresses, amounts);
            });
        }

        [Fact]
        public void AssertWhenExecutedByNonAdmin()
        {
            var addresses = Enumerable.Range(0, 5).Select(_ => new PrivateKey().Address).ToList();
            var amounts = Enumerable.Range(0, 5).Select(i => (i + 1) * 10);

            Assert.Throws<PermissionDeniedException>(() =>
            {
                new FixToRefundFromNonValidator(
                    addresses,
                    amounts
                ).Execute(new ActionContext
                {
                    PreviousState = _world,
                    Signer = new PrivateKey().Address,
                    BlockIndex = 2L,
                });
            });
        }
    }
}
