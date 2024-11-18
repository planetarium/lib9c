namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild.Migration;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData.Stake;
    using Xunit;

    // TODO: Remove this test class after the migration is completed.
    public class FixToRefundFromNonValidatorTest : GuildTestBase
    {
        private IWorld _world;
        private Currency _ncg;
        private Currency _gg;
        private Address _agentAddress;
        private Address _stakeAddress;
        private Address _adminAddress;

        public FixToRefundFromNonValidatorTest()
        {
            _agentAddress = new PrivateKey().Address;
            _adminAddress = new PrivateKey().Address;
            _stakeAddress = StakeState.DeriveAddress(_agentAddress);
            _ncg = World.GetGoldCurrency();
            _gg = Currencies.GuildGold;
            var sheetsOverride = new Dictionary<string, string>
            {
                {
                    "StakeRegularFixedRewardSheet_V1",
                    StakeRegularFixedRewardSheetFixtures.V1
                },
                {
                    "StakeRegularFixedRewardSheet_V2",
                    StakeRegularFixedRewardSheetFixtures.V2
                },
                {
                    "StakeRegularRewardSheet_V1",
                    StakeRegularRewardSheetFixtures.V1
                },
                {
                    "StakeRegularRewardSheet_V2",
                    StakeRegularRewardSheetFixtures.V2
                },
                {
                    nameof(StakePolicySheet),
                    StakePolicySheetFixtures.V2
                },
            };
            (_, _, _, _world) = InitializeUtil.InitializeStates(
                agentAddr: _agentAddress,
                sheetsOverride: sheetsOverride);
            var stakePolicySheet = _world.GetSheet<StakePolicySheet>();
            var contract = new Contract(stakePolicySheet);
            var adminState = new AdminState(_adminAddress, 100L);
            var stakeState = new StakeState(new Contract(stakePolicySheet), 1L);

            _world = World
                .SetLegacyState(Addresses.Admin, adminState.Serialize())
                .SetLegacyState(_stakeAddress, stakeState.Serialize())
                .MintAsset(new ActionContext { }, Addresses.NonValidatorDelegatee, _gg * 10000)
                .MintAsset(new ActionContext { }, _stakeAddress, _ncg * 10)
                .MintAsset(new ActionContext { }, _stakeAddress, _gg * 5);
        }

        [Fact]
        public void Execute()
        {
            var world = new FixToRefundFromNonValidator(new Address[] { _agentAddress }).Execute(new ActionContext
            {
                PreviousState = _world,
                Signer = _adminAddress,
            });

            Assert.Equal(_gg * 10, world.GetBalance(_stakeAddress, _gg));
            Assert.Equal(_gg * (10000 - 5), world.GetBalance(Addresses.NonValidatorDelegatee, _gg));
        }

        [Fact]
        public void AssertWhenExecutedByNonAdmin()
        {
            Assert.Throws<PermissionDeniedException>(() =>
            {
                new FixToRefundFromNonValidator(new Address[] { _agentAddress }).Execute(new ActionContext
                {
                    PreviousState = _world,
                    Signer = new PrivateKey().Address,
                    BlockIndex = 2L,
                });
            });
        }

        [Fact]
        public void AssertWhenHasSufficientGG()
        {
            var world = _world
                .MintAsset(new ActionContext { }, _stakeAddress, _gg * 5);
            Assert.Throws<InvalidOperationException>(() =>
            {
                new FixToRefundFromNonValidator(new Address[] { _agentAddress }).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = _adminAddress,
                    BlockIndex = 2L,
                });
            });
        }

        [Fact]
        public void AssertWhenLegacyStakeState()
        {
            var stakeState = new LegacyStakeState(_stakeAddress, 0L);
            var world = _world.SetLegacyState(_stakeAddress, stakeState.Serialize());
            Assert.Throws<InvalidOperationException>(() =>
            {
                new FixToRefundFromNonValidator(new Address[] { _agentAddress }).Execute(new ActionContext
                {
                    PreviousState = World,
                    Signer = _adminAddress,
                    BlockIndex = 2L,
                });
            });
        }
    }
}
