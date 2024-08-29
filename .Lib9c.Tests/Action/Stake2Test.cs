namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class Stake2Test
    {
        private readonly IWorld _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;

        public Stake2Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new World(MockUtil.MockModernWorldState);

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().Address;
            var context = new ActionContext();
            _initialState = _initialState
                .SetLegacyState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .MintAsset(context, _signerAddress, _currency * 100);
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var action = new Stake2(200);
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
                avatarAddresses = { [0] = new PrivateKey().Address, },
            };
            var states = _initialState
                .SetAgentState(_signerAddress, agentState)
                .SetLegacyState(
                    monsterCollectionAddress,
                    new MonsterCollectionState(monsterCollectionAddress, 1, 0).Serialize());
            var action = new Stake2(200);
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
            Address stakeStateAddress = LegacyStakeState.DeriveAddress(_signerAddress);
            var context = new ActionContext();
            var states = _initialState
                .SetLegacyState(stakeStateAddress, new LegacyStakeState(stakeStateAddress, 0).Serialize())
                .MintAsset(context, stakeStateAddress, _currency * 50);
            var action = new Stake2(100);
            Assert.Throws<StakeExistingClaimableException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = states,
                    Signer = _signerAddress,
                    BlockIndex = LegacyStakeState.RewardInterval,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenCancelOrUpdateWhileLockup()
        {
            var action = new Stake2(51);
            var states = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            // Cancel
            var updateAction = new Stake2(0);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // Less
            updateAction = new Stake2(50);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // Same (since 4611070)
            if (states.TryGetStakeState(_signerAddress, out LegacyStakeState stakeState))
            {
                states = states.SetLegacyState(
                    stakeState.address,
                    new LegacyStakeState(stakeState.address, 4611070 - 100).Serialize());
            }

            updateAction = new Stake2(51);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = 4611070,
            }));

            // At 4611070 - 99, it should be updated.
            Assert.True(updateAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = 4611070 - 99,
            }).TryGetStakeState(_signerAddress, out stakeState));
            Assert.Equal(4611070 - 99, stakeState.StartedBlockIndex);
        }

        [Fact]
        public void Execute()
        {
            var action = new Stake2(100);
            var states = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * 100,
                states.GetBalance(LegacyStakeState.DeriveAddress(_signerAddress), _currency));

            states.TryGetStakeState(_signerAddress, out LegacyStakeState stakeState);
            Assert.Equal(0, stakeState.StartedBlockIndex);
            Assert.Equal(0 + LegacyStakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 100, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));

            var achievements = stakeState.Achievements;
            Assert.False(achievements.Check(0, 0));
            Assert.False(achievements.Check(0, 1));
            Assert.False(achievements.Check(1, 0));

            LegacyStakeState producedLegacyStakeState = new LegacyStakeState(
                stakeState.address,
                stakeState.StartedBlockIndex,
                // Produce a situation that it already received rewards.
                LegacyStakeState.LockupInterval - 1,
                stakeState.CancellableBlockIndex,
                stakeState.Achievements);
            states = states.SetLegacyState(stakeState.address, producedLegacyStakeState.SerializeV2());
            var cancelAction = new Stake2(0);
            states = cancelAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = LegacyStakeState.LockupInterval,
            });

            Assert.Null(states.GetLegacyState(stakeState.address));
            Assert.Equal(_currency * 0, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 100, states.GetBalance(_signerAddress, _currency));
        }

        [Fact]
        public void Update()
        {
            var action = new Stake2(50);
            var states = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            states.TryGetStakeState(_signerAddress, out LegacyStakeState stakeState);
            Assert.Equal(0, stakeState.StartedBlockIndex);
            Assert.Equal(0 + LegacyStakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 50, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 50, states.GetBalance(_signerAddress, _currency));

            var updateAction = new Stake2(100);
            states = updateAction.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            });

            states.TryGetStakeState(_signerAddress, out stakeState);
            Assert.Equal(1, stakeState.StartedBlockIndex);
            Assert.Equal(1 + LegacyStakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 100, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));
        }

        [Fact]
        public void RestrictForStakeStateV2()
        {
            var action = new Stake2(50);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = _initialState.SetLegacyState(
                    LegacyStakeState.DeriveAddress(_signerAddress),
                    new StakeState(
                        new Contract(
                            "StakeRegularFixedRewardSheet_V1",
                            "StakeRegularRewardSheet_V1",
                            50400,
                            201600),
                        0).Serialize()),
                Signer = _signerAddress,
                BlockIndex = 0,
            }));
        }

        [Fact]
        public void Serialization()
        {
            var action = new Stake2(100);
            var deserialized = new Stake2();
            deserialized.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.Amount, deserialized.Amount);
        }
    }
}
