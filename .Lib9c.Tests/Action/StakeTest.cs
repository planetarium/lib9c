namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class StakeTest
    {
        private readonly IWorld _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;

        public StakeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new MockWorld();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = LegacyModule.SetState(
                    _initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            var context = new ActionContext();
            _initialState = LegacyModule.SetState(
                _initialState,
                GoldCurrencyState.Address,
                _goldCurrencyState.Serialize());
            _initialState = LegacyModule.MintAsset(
                _initialState,
                context,
                _signerAddress,
                _currency * 100);
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var action = new Stake(200);
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenThereIsMonsterCollection()
        {
            Address monsterCollectionAddress =
                MonsterCollectionState.DeriveAddress(_signerAddress, 0);
            var agentState = new AgentState(_signerAddress)
            {
                avatarAddresses = { [0] = new PrivateKey().ToAddress(), },
            };
            var states = AgentModule.SetAgentState(_initialState, _signerAddress, agentState);
            states = LegacyModule.SetState(
                states,
                monsterCollectionAddress,
                new MonsterCollectionState(monsterCollectionAddress, 1, 0).SerializeV2());
            var action = new Stake(200);
            Assert.Throws<MonsterCollectionExistingException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = states,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenClaimableExisting()
        {
            Address stakeStateAddress = StakeState.DeriveAddress(_signerAddress);
            var context = new ActionContext();
            var states =
                LegacyModule.SetState(_initialState, stakeStateAddress, new StakeState(
                    stakeStateAddress,
                    0).Serialize());
            states = LegacyModule.MintAsset(states, context, stakeStateAddress, _currency * 50);
            var action = new Stake(100);
            Assert.Throws<StakeExistingClaimableException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = states,
                    Signer = _signerAddress,
                    BlockIndex = StakeState.RewardInterval,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenCancelOrUpdateWhileLockup()
        {
            var action = new Stake(51);
            var world = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            // Cancel
            var updateAction = new Stake(0);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // Less
            updateAction = new Stake(50);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // Same (since 4611070)
            if (LegacyModule.TryGetStakeState(world, _signerAddress, out StakeState stakeState))
            {
                world = LegacyModule.SetState(
                    world,
                    stakeState.address,
                    new StakeState(stakeState.address, 4611070 - 100).Serialize());
            }

            updateAction = new Stake(51);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = _signerAddress,
                BlockIndex = 4611070,
            }));

            // At 4611070 - 99, it should be updated.
            Assert.True(
                LegacyModule.TryGetStakeState(
                    updateAction.Execute(
                        new ActionContext
                        {
                            PreviousState = world,
                            Signer = _signerAddress,
                            BlockIndex = 4611070 - 99,
                        }),
                    _signerAddress,
                    out stakeState));
            Assert.Equal(4611070 - 99, stakeState.StartedBlockIndex);
        }

        [Fact]
        public void Execute()
        {
            var action = new Stake(100);
            var world = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.Equal(_currency * 0, LegacyModule.GetBalance(world, _signerAddress, _currency));
            Assert.Equal(
                _currency * 100,
                LegacyModule.GetBalance(world, StakeState.DeriveAddress(_signerAddress), _currency));

            LegacyModule.TryGetStakeState(world, _signerAddress, out StakeState stakeState);
            Assert.Equal(0, stakeState.StartedBlockIndex);
            Assert.Equal(0 + StakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 100, LegacyModule.GetBalance(world, stakeState.address, _currency));
            Assert.Equal(_currency * 0, LegacyModule.GetBalance(world, _signerAddress, _currency));

            var achievements = stakeState.Achievements;
            Assert.False(achievements.Check(0, 0));
            Assert.False(achievements.Check(0, 1));
            Assert.False(achievements.Check(1, 0));

            StakeState producedStakeState = new StakeState(
                stakeState.address,
                stakeState.StartedBlockIndex,
                // Produce a situation that it already received rewards.
                StakeState.LockupInterval - 1,
                stakeState.CancellableBlockIndex,
                stakeState.Achievements);
            world = LegacyModule.SetState(world, stakeState.address, producedStakeState.SerializeV2());
            var cancelAction = new Stake(0);
            world = cancelAction.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval,
            });

            Assert.Equal(Null.Value, LegacyModule.GetState(world, stakeState.address));
            Assert.Equal(_currency * 0, LegacyModule.GetBalance(world, stakeState.address, _currency));
            Assert.Equal(_currency * 100, LegacyModule.GetBalance(world, _signerAddress, _currency));
        }

        [Fact]
        public void Update()
        {
            var action = new Stake(50);
            var world = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);

            LegacyModule.TryGetStakeState(world, _signerAddress, out StakeState stakeState);
            Assert.Equal(0, stakeState.StartedBlockIndex);
            Assert.Equal(0 + StakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 50, account.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 50, account.GetBalance(_signerAddress, _currency));

            var updateAction = new Stake(100);
            world = updateAction.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = _signerAddress,
                BlockIndex = 1,
            });
            account = world.GetAccount(ReservedAddresses.LegacyAccount);

            LegacyModule.TryGetStakeState(world, _signerAddress, out stakeState);
            Assert.Equal(1, stakeState.StartedBlockIndex);
            Assert.Equal(1 + StakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 100, account.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 0, account.GetBalance(_signerAddress, _currency));
        }

        [Fact]
        public void Serialization()
        {
            var action = new Stake(100);
            var deserialized = new Stake();
            deserialized.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.Amount, deserialized.Amount);
        }
    }
}
