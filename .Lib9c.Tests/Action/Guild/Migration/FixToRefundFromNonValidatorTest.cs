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
            var targets = Enumerable.Range(0, 5).Select(i => (new PrivateKey().Address, (i + 1) * 10)).ToList();
            var plainValue = new FixToRefundFromNonValidator(targets).PlainValue;

            var recon = new FixToRefundFromNonValidator();
            recon.LoadPlainValue(plainValue);
            Assert.Equal(targets, recon.Targets);
        }

        [Fact]
        public void Execute()
        {
            var targets = Enumerable.Range(0, 5).Select(i => (new PrivateKey().Address, (i + 1) * 10)).ToList();

            var world = new FixToRefundFromNonValidator(targets).Execute(new ActionContext
            {
                PreviousState = _world,
                Signer = _adminAddress,
                BlockIndex = 2L,
            });

            foreach (var item in targets)
            {
                Assert.Equal(
                    Currencies.GuildGold * item.Item2,
                    world.GetBalance(StakeState.DeriveAddress(item.Item1), Currencies.GuildGold));
            }

            Assert.Equal(
                Currencies.GuildGold * (500 - targets.Select(t => t.Item2).Sum()),
                world.GetBalance(Addresses.NonValidatorDelegatee, Currencies.GuildGold));
        }

        [Fact]
        public void AssertWhenExecutedByNonAdmin()
        {
            var targets = Enumerable.Range(0, 5).Select(i => (new PrivateKey().Address, (i + 1) * 10)).ToList();

            Assert.Throws<PermissionDeniedException>(() =>
            {
                new FixToRefundFromNonValidator(targets).Execute(new ActionContext
                {
                    PreviousState = _world,
                    Signer = new PrivateKey().Address,
                    BlockIndex = 2L,
                });
            });
        }
    }
}
