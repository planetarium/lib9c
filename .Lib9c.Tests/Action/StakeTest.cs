namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TableData.Stake;
    using Nekoyume.TypedAddress;
    using Nekoyume.ValidatorDelegation;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class StakeTest
    {
        private readonly IWorld _initialState;
        private readonly Currency _ncg;
        private readonly Currency _gg = Currencies.GuildGold;
        private readonly PublicKey _agentPublicKey = new PrivateKey().PublicKey;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly StakePolicySheet _stakePolicySheet;

        public StakeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

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
            (
                _,
                _agentAddr,
                _avatarAddr,
                _initialState
            ) = InitializeUtil.InitializeStates(
                sheetsOverride: sheetsOverride,
                agentAddr: _agentPublicKey.Address);
            _ncg = _initialState.GetGoldCurrency();
            _stakePolicySheet = _initialState.GetSheet<StakePolicySheet>();
        }

        [Theory]
        [InlineData(long.MinValue, false)]
        [InlineData(0, true)]
        [InlineData(long.MaxValue, true)]
        public void Constructor(long amount, bool success)
        {
            var avatarAddress = new PrivateKey().Address;

            if (success)
            {
                var stake = new Stake(amount, _avatarAddr);
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new Stake(amount, _avatarAddr));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(long.MaxValue)]
        public void Serialization(long amount)
        {
            var action = new Stake(amount, _avatarAddr);
            var ser = action.PlainValue;
            var de = new Stake();
            de.LoadPlainValue(ser);
            Assert.Equal(action.Amount, de.Amount);
            Assert.Equal(ser, de.PlainValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)] // NOTE: 9 is just a random number.
        public void Execute_Throw_MonsterCollectionExistingException(
            int monsterCollectionRound)
        {
            var previousState = _initialState;
            var agentState = previousState.GetAgentState(_agentAddr);
            if (monsterCollectionRound > 0)
            {
                for (var i = 0; i < monsterCollectionRound; i++)
                {
                    agentState.IncreaseCollectionRound();
                }

                previousState = previousState
                    .SetAgentState(_agentAddr, agentState);
            }

            var monsterCollectionAddr =
                MonsterCollectionState.DeriveAddress(_agentAddr, monsterCollectionRound);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddr,
                1,
                0);
            previousState = previousState
                .SetLegacyState(monsterCollectionAddr, monsterCollectionState.Serialize());
            Assert.Throws<MonsterCollectionExistingException>(
                () =>
                    Execute(
                        0,
                        previousState,
                        new TestRandom(),
                        _agentAddr,
                        0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(long.MinValue)]
        public void Execute_Throw_ArgumentOutOfRangeException_Via_Negative_Amount(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    Execute(
                        0,
                        _initialState,
                        new TestRandom(),
                        _agentAddr,
                        amount));
        }

        [Theory]
        [InlineData(nameof(StakePolicySheet))]
        // NOTE: StakePolicySheet in _initialState has V2.
        [InlineData("StakeRegularFixedRewardSheet_V2")]
        // NOTE: StakePolicySheet in _initialState has V2.
        [InlineData("StakeRegularRewardSheet_V2")]
        public void Execute_Throw_StateNullException_Via_Sheet(string sheetName)
        {
            var previousState = _initialState.SetLegacyState(
                Addresses.GetSheetAddress(sheetName),
                Null.Value);
            Assert.Throws<StateNullException>(
                () =>
                    Execute(
                        0,
                        previousState,
                        new TestRandom(),
                        _agentAddr,
                        0));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(49)]
        [InlineData(1)]
        public void Execute_Throw_ArgumentOutOfRangeException_Via_Minimum_Amount(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    Execute(
                        0,
                        _initialState,
                        new TestRandom(),
                        _agentAddr,
                        amount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(0, 50)]
        [InlineData(49, 50)]
        [InlineData(long.MaxValue - 1, long.MaxValue)]
        public void Execute_Throws_NotEnoughFungibleAssetValueException(
            long balance,
            long amount)
        {
            var previousState = _initialState;
            if (balance > 0)
            {
                previousState = _initialState.MintAsset(
                    new ActionContext { Signer = Addresses.Admin, },
                    _agentAddr,
                    _ncg * balance);
            }

            Assert.Throws<NotEnoughFungibleAssetValueException>(
                () =>
                    Execute(
                        0,
                        previousState,
                        new TestRandom(),
                        _agentAddr,
                        amount));
        }

        [Fact]
        public void Execute_Throw_StateNullException_Via_0_Amount()
        {
            Assert.Throws<StateNullException>(
                () =>
                    Execute(
                        0,
                        _initialState,
                        new TestRandom(),
                        _agentAddr,
                        0));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(0, 50, LegacyStakeState.RewardInterval)]
        public void Execute_Success_When_Claimable_With_StakeState(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex)
        {
            var stakeStateAddr = LegacyStakeState.DeriveAddress(_agentAddr);
            var stakeState = new LegacyStakeState(
                address: stakeStateAddr,
                startedBlockIndex: previousStartedBlockIndex);
            Assert.True(stakeState.IsClaimable(blockIndex));
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin, },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin, },
                    stakeStateAddr,
                    _gg * previousAmount)
                .SetLegacyState(stakeStateAddr, stakeState.Serialize());

            var nextState = Execute(
                blockIndex,
                previousState,
                new TestRandom(),
                _agentAddr,
                previousAmount);

            Assert.True(nextState.TryGetStakeState(_agentAddr, out var newStakeState));
            Assert.Equal(blockIndex, newStakeState.ClaimedBlockIndex);
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: RewardInterval of StakePolicySheetFixtures.V2 is 50,400.
        [InlineData(0, 50, 50400)]
        public void Execute_Throw_StakeExistingClaimableException_With_StakeStateV2(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex)
        {
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeStateV2 = new StakeState(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: previousStartedBlockIndex);
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin, },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin, },
                    stakeStateAddr,
                    _gg * previousAmount)
                .SetLegacyState(stakeStateAddr, stakeStateV2.Serialize());
            var nextState = Execute(
                blockIndex,
                previousState,
                new TestRandom(),
                _agentAddr,
                previousAmount);

            Assert.True(nextState.TryGetStakeState(_agentAddr, out var newStakeState));
            Assert.Equal(blockIndex, newStakeState.ClaimedBlockIndex);
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(50)]
        [InlineData(long.MaxValue)]
        public void Execute_Success_When_Staking_State_Null(long amount)
        {
            var world = _initialState.MintAsset(
                new ActionContext { Signer = Addresses.Admin },
                _agentAddr,
                _ncg * amount);
            var height = 0L;
            var promoteAmount = 50;

            var validatorKey = new PrivateKey().PublicKey;
            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, validatorKey, promoteAmount, height++);
            world = DelegationUtil.MakeGuild(world, _agentAddr, validatorKey.Address, height++);

            Execute(
                0,
                world,
                new TestRandom(),
                _agentAddr,
                amount);

            var delegatee = new GuildRepository(world, new ActionContext { }).GetDelegatee(validatorKey.Address);
            world = DelegationUtil.EnsureUnbondedClaimed(
                world, _agentAddr, height + delegatee.UnbondingPeriod);
        }

        [Theory]
        // NOTE: non
        [InlineData(50, 50)]
        [InlineData(long.MaxValue, long.MaxValue)]
        // NOTE: delegate
        [InlineData(0, 500)]
        [InlineData(50, 100)]
        [InlineData(0, long.MaxValue)]
        // NOTE: undelegate
        [InlineData(50, 0)]
        [InlineData(75, 50)]
        [InlineData(long.MaxValue, 0)]
        [InlineData(long.MaxValue, 500)]
        public void Execute_Success_When_Exist_StakeStateV3(
            long previousAmount,
            long amount)
        {
            var interval = previousAmount < amount
                ? LegacyStakeState.RewardInterval : LegacyStakeState.LockupInterval;
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: 0L,
                receivedBlockIndex: interval,
                stateVersion: 3);
            var world = _initialState;
            var height = 0L;
            var promoteAmount = 50;

            var validatorKey = new PrivateKey().PublicKey;
            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, validatorKey, promoteAmount, height);
            world = DelegationUtil.MakeGuild(world, _agentAddr, validatorKey.Address, height);
            if (previousAmount > 0)
            {
                var ncgToStake = _ncg * previousAmount;
                var gg = FungibleAssetValue.Parse(_gg, ncgToStake.GetQuantityString(true));
                world = world.MintAsset(new ActionContext(), _agentAddr, gg);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, gg);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, ncgToStake);

                var guildRepository = new GuildRepository(world, new ActionContext { Signer = _agentAddr });
                var guildParticipant = guildRepository.GetGuildParticipant(_agentAddr);
                var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                guildParticipant.Delegate(guild, gg, height);
                world = guildRepository.World;
            }

            world = world.SetLegacyState(stakeStateAddr, stakeState.Serialize());

            if (amount - previousAmount > 0)
            {
                var ncgToStake = _ncg * (amount - previousAmount);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
            }

            var nextState = Execute(
                height + interval,
                world,
                new TestRandom(),
                _agentAddr,
                amount);

            if (amount > 0)
            {
                Assert.True(nextState.TryGetStakeState(_agentAddr, out StakeState nextStakeState));
                Assert.Equal(3, nextStakeState.StateVersion);
            }

            var delegatee = new GuildRepository(world, new ActionContext { }).GetDelegatee(validatorKey.Address);
            world = DelegationUtil.EnsureUnbondedClaimed(
                nextState, _agentAddr, height + interval + delegatee.UnbondingPeriod);

            var expectedBalance = _ncg * Math.Max(0, previousAmount - amount);
            var actualBalance = world.GetBalance(_agentAddr, _ncg);
            Assert.Equal(expectedBalance, actualBalance);
        }

        [Theory]
        // NOTE: non
        [InlineData(50, 50)]
        [InlineData(long.MaxValue, long.MaxValue)]
        // NOTE: delegate
        [InlineData(0, 500)]
        [InlineData(50, 100)]
        [InlineData(0, long.MaxValue)]
        // NOTE: undelegate
        [InlineData(50, 0)]
        [InlineData(75, 50)]
        [InlineData(long.MaxValue, 0)]
        [InlineData(long.MaxValue, 500)]
        public void Execute_Success_When_Exist_StakeStateV3_Without_Guild(
            long previousAmount,
            long amount)
        {
            var interval = previousAmount < amount
                ? LegacyStakeState.RewardInterval : LegacyStakeState.LockupInterval;
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: 0L,
                receivedBlockIndex: interval,
                stateVersion: 3);
            var world = _initialState;
            var height = 0L;

            if (previousAmount > 0)
            {
                var ncgToStake = _ncg * previousAmount;
                var gg = FungibleAssetValue.Parse(_gg, ncgToStake.GetQuantityString(true));
                world = world.MintAsset(new ActionContext(), _agentAddr, gg);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, gg);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, ncgToStake);
            }

            world = world.SetLegacyState(stakeStateAddr, stakeState.Serialize());

            if (amount - previousAmount > 0)
            {
                var ncgToStake = _ncg * (amount - previousAmount);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
            }

            var nextState = Execute(
                height + interval,
                world,
                new TestRandom(),
                _agentAddr,
                amount);

            if (amount > 0)
            {
                Assert.True(nextState.TryGetStakeState(_agentAddr, out StakeState nextStakeState));
                Assert.Equal(3, nextStakeState.StateVersion);
            }

            var expectedBalance = _ncg * Math.Max(0, previousAmount - amount);
            var actualBalance = nextState.GetBalance(_agentAddr, _ncg);
            var stakeBalance = nextState.GetStaked(_agentAddr);
            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(_ncg * amount, stakeBalance);
        }

        [Theory]
        [InlineData(0, 500, false)]
        [InlineData(50, 100, false)]
        [InlineData(0, long.MaxValue, false)]
        [InlineData(0, 500, true)]
        [InlineData(50, 100, true)]
        [InlineData(0, long.MaxValue, true)]
        public void Execute_Success_When_Validator_Tries_To_Increase_Amount_Without_Claim(
            long previousAmount,
            long amount,
            bool withoutInterval)
        {
            if (previousAmount >= amount)
            {
                throw new ArgumentException(
                    "previousAmount should be less than amount.", nameof(previousAmount));
            }

            var interval = LegacyStakeState.RewardInterval;
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: 0L,
                receivedBlockIndex: interval,
                stateVersion: 3);
            var world = _initialState;
            var height = 0L;
            var promoteAmount = 50;

            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, _agentPublicKey, promoteAmount, height++);

            if (previousAmount > 0)
            {
                var ncgToStake = _ncg * previousAmount;
                var gg = GuildModule.ConvertCurrency(ncgToStake, _gg).TargetFAV;
                world = world.MintAsset(new ActionContext(), _agentAddr, gg);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, gg);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
                world = world.TransferAsset(
                    new ActionContext(), _agentAddr, stakeStateAddr, ncgToStake);
            }

            world = world.SetLegacyState(stakeStateAddr, stakeState.Serialize());

            if (amount - previousAmount > 0)
            {
                var ncgToStake = _ncg * (amount - previousAmount);
                world = world.MintAsset(new ActionContext(), _agentAddr, ncgToStake);
            }

            var nextState = Execute(
                height + (withoutInterval ? 1 : interval),
                world,
                new TestRandom(),
                _agentAddr,
                amount);

            if (amount > 0)
            {
                Assert.True(nextState.TryGetStakeState(_agentAddr, out StakeState nextStakeState));
                Assert.Equal(3, nextStakeState.StateVersion);
            }

            var delegatee = new GuildRepository(world, new ActionContext { }).GetDelegatee(_agentAddr);
            world = DelegationUtil.EnsureUnbondedClaimed(
                nextState, _agentAddr, height + interval + delegatee.UnbondingPeriod);

            var expectedBalance = _ncg * (Math.Max(0, previousAmount - amount) + promoteAmount);
            var expectedStaked = _ncg * amount;
            var actualBalance = world.GetBalance(_agentAddr, _ncg);
            var stakeBalance = world.GetStaked(_agentAddr);
            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedStaked, stakeBalance);
        }

        [Fact]
        public void Execute_Success_When_Validator_Slashed()
        {
            var world = _initialState;
            var height = 0L;
            var promoteAmount = 50;
            var validatorKey = new PrivateKey();
            var validatorAddress = validatorKey.PublicKey.Address;
            var guildMasterKey = new PrivateKey();
            var guildMasterAddress = new GuildAddress(guildMasterKey.Address);
            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, validatorKey.PublicKey, promoteAmount, height++);
            world = DelegationUtil.MakeGuild(
                world, guildMasterAddress, validatorAddress, height++, out var guildAddress);
            world = DelegationUtil.MintNCG(world, _agentAddr, 100, height++);
            world = DelegationUtil.Stake(world, _agentAddr, _avatarAddr, 100, height++);
            world = DelegationUtil.JoinGuild(world, _agentAddr, guildAddress, height++);
            world = DelegationUtil.SlashValidator(world, validatorAddress, 10, height++);

            Assert.True(world.TryGetStakeState(_agentAddr, out var stakeState));
            height += stakeState.CancellableBlockIndex;
            world = DelegationUtil.Stake(world, _agentAddr, _avatarAddr, 0, height++);
            var delegatee = new GuildRepository(world, new ActionContext { }).GetDelegatee(validatorKey.Address);
            world = DelegationUtil.EnsureUnbondedClaimed(
                world, _agentAddr, height++ + delegatee.UnbondingPeriod);

            var actualNCG = world.GetBalance(_agentAddr, _ncg);
            Assert.Equal(_ncg * 90, actualNCG);
        }

        [Fact]
        public void Execute_Success_Exceptional_Unmigrated()
        {
            var world = _initialState;
            var height = 0L;
            var promoteAmount = 50;
            var validatorKey = new PrivateKey();
            var validatorAddress = validatorKey.PublicKey.Address;
            var guildMasterKey = new PrivateKey();
            var guildMasterAddress = new GuildAddress(guildMasterKey.Address);
            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, validatorKey.PublicKey, promoteAmount, height++);
            world = DelegationUtil.MakeGuild(
                world, guildMasterAddress, validatorAddress, height++, out var guildAddress);
            world = DelegationUtil.JoinGuild(world, _agentAddr, guildAddress, height++);
            world = DelegationUtil.MintNCG(world, _agentAddr, 100, height++);
            var stakeStateAddr = LegacyStakeState.DeriveAddress(_agentAddr);
            var stakeState = new LegacyStakeState(
                address: stakeStateAddr,
                startedBlockIndex: height++);
            world = world.TransferAsset(
                new ActionContext { },
                _agentAddr,
                stakeStateAddr,
                world.GetGoldCurrency() * 100);
            world = world.SetLegacyState(stakeStateAddr, stakeState.Serialize());

            Assert.True(world.TryGetStakeState(_agentAddr, out var stakeStateV2));
            Assert.Equal(2, stakeStateV2.StateVersion);
            Assert.Equal(Currencies.GuildGold * 0, world.GetBalance(stakeStateAddr, Currencies.GuildGold));
            var repo = new GuildRepository(world, new ActionContext { });
            var share = repo.GetBond(repo.GetDelegatee(validatorAddress), _agentAddr).Share;
            Assert.Equal((Currencies.GuildGold * 0).RawValue, share);

            if (!StakeStateUtils.TryMigrateV2ToV3(
                new ActionContext { },
                world,
                stakeStateAddr,
                stakeStateV2,
                out var result))
            {
                throw new InvalidOperationException("Failed to migration. Unexpected situation.");
            }

            world = result.Value.world;
            Assert.True(world.TryGetStakeState(_agentAddr, out var stakeStateV3));
            Assert.Equal(3, stakeStateV3.StateVersion);
            Assert.Equal(Currencies.GuildGold * 100, world.GetBalance(stakeStateAddr, Currencies.GuildGold));
            repo.UpdateWorld(world);
            share = repo.GetBond(repo.GetDelegatee(validatorAddress), _agentAddr).Share;
            Assert.Equal((Currencies.GuildGold * 0).RawValue, share);

            world = DelegationUtil.Stake(world, _agentAddr, _avatarAddr, 100, height++);
            Assert.Equal(Currencies.GuildGold * 0, world.GetBalance(stakeStateAddr, Currencies.GuildGold));
            repo.UpdateWorld(world);
            share = repo.GetBond(repo.GetDelegatee(validatorAddress), _agentAddr).Share;
            Assert.Equal((Currencies.GuildGold * 100).RawValue, share);
        }

        [Fact]
        public void Execute_Success_Unmigrated()
        {
            var world = _initialState;
            var height = 0L;
            var promoteAmount = 50;
            var validatorKey = new PrivateKey();
            var validatorAddress = validatorKey.PublicKey.Address;
            var guildMasterKey = new PrivateKey();
            var guildMasterAddress = new GuildAddress(guildMasterKey.Address);
            world = DelegationUtil.EnsureValidatorPromotionReady(
                world, validatorKey.PublicKey, promoteAmount, height++);
            world = DelegationUtil.MakeGuild(
                world, guildMasterAddress, validatorAddress, height++, out var guildAddress);
            world = DelegationUtil.JoinGuild(world, _agentAddr, guildAddress, height++);
            world = DelegationUtil.MintNCG(world, _agentAddr, 100, height++);
            var stakeStateAddr = LegacyStakeState.DeriveAddress(_agentAddr);
            var stakeState = new LegacyStakeState(
                address: stakeStateAddr,
                startedBlockIndex: height++);
            world = world.TransferAsset(
                new ActionContext { },
                _agentAddr,
                stakeStateAddr,
                world.GetGoldCurrency() * 100);
            world = world.SetLegacyState(stakeStateAddr, stakeState.Serialize());
            var repo = new GuildRepository(world, new ActionContext { });
            var share = repo.GetBond(repo.GetDelegatee(validatorAddress), _agentAddr).Share;
            Assert.Equal((Currencies.GuildGold * 0).RawValue, share);
            world = DelegationUtil.Stake(world, _agentAddr, _avatarAddr, 100, height++);
            repo.UpdateWorld(world);
            share = repo.GetBond(repo.GetDelegatee(validatorAddress), _agentAddr).Share;
            Assert.Equal((Currencies.GuildGold * 100).RawValue, share);
        }

        private IWorld Execute(
            long blockIndex,
            IWorld previousState,
            IRandom random,
            Address signer,
            long amount)
        {
            var action = new Stake(amount, _avatarAddr);
            var nextState = action.Execute(
                new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousState = previousState,
                    RandomSeed = random.Seed,
                    Signer = signer,
                });

            var guildRepository = new GuildRepository(nextState, new ActionContext());
            if (guildRepository.TryGetGuildParticipant(new AgentAddress(signer), out var guildParticipant))
            {
                var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                var validator = guildRepository.GetDelegatee(guild.ValidatorAddress);
                var bond = guildRepository.GetBond(validator, signer);
                var amountNCG = _ncg * amount;
                var expectedGG = DelegationUtil.GetGuildCoinFromNCG(amountNCG);
                var expectedShare = validator.ShareFromFAV(expectedGG);
                Assert.Equal(expectedShare, bond.Share);
            }

            if (amount == 0)
            {
                Assert.False(nextState.TryGetStakeState(_agentAddr, out _));
            }
            else if (amount > 0)
            {
                Assert.True(nextState.TryGetStakeState(_agentAddr, out var stakeStateV2));
                Assert.Equal(
                    _stakePolicySheet.StakeRegularFixedRewardSheetValue,
                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName);
                Assert.Equal(
                    _stakePolicySheet.StakeRegularRewardSheetValue,
                    stakeStateV2.Contract.StakeRegularRewardSheetTableName);
                Assert.Equal(
                    _stakePolicySheet.RewardIntervalValue,
                    stakeStateV2.Contract.RewardInterval);
                Assert.Equal(
                    _stakePolicySheet.LockupIntervalValue,
                    stakeStateV2.Contract.LockupInterval);
                Assert.Equal(blockIndex, stakeStateV2.StartedBlockIndex);
                Assert.Equal(0, stakeStateV2.ReceivedBlockIndex);
                Assert.Equal(blockIndex, stakeStateV2.ClaimedBlockIndex);
                Assert.Equal(
                    blockIndex + stakeStateV2.Contract.RewardInterval,
                    stakeStateV2.ClaimableBlockIndex);
            }

            return nextState;
        }
    }
}
