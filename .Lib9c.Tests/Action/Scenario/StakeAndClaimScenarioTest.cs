namespace Lib9c.Tests.Action.Scenario
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Stake;
    using Nekoyume.ValidatorDelegation;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// This class is used for testing the stake and claim scenario with patching table sheets.
    /// This class considers only AvatarStateV2 not AvatarState.
    /// </summary>
    public class StakeAndClaimScenarioTest
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IWorld _initialStateWithoutStakePolicySheet;
        private readonly Currency _ncg;

        public StakeAndClaimScenarioTest(ITestOutputHelper output)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output)
                .CreateLogger();

            var tuple = InitializeUtil.InitializeStates(
                sheetsOverride: new Dictionary<string, string>
                {
                    {
                        nameof(GameConfigSheet),
                        GameConfigSheetFixtures.Default
                    },
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
                });
            _agentAddr = tuple.agentAddr;
            _avatarAddr = tuple.avatarAddr;
            _initialStateWithoutStakePolicySheet = tuple.world;
            _ncg = _initialStateWithoutStakePolicySheet.GetGoldCurrency();
        }

        [Fact]
        public void Test()
        {
            // Mint NCG to agent.
            var state = MintAsset(
                _initialStateWithoutStakePolicySheet,
                _agentAddr,
                10_000_000 * _ncg,
                0);

            // Stake 50 NCG via stake2.
            const long stakedAmount = 50;
            const int stake2BlockIndex = 1;
            state = Stake2(state, _agentAddr, stakedAmount, stake2BlockIndex);

            // Validate staked.
            var stakedNCG = stakedAmount * _ncg;
            ValidateStakedState(state, _agentAddr, stakedNCG, stake2BlockIndex);

            // Claim stake reward via claim_stake_reward9.
            state = ClaimStakeReward9(
                state,
                _agentAddr,
                _avatarAddr,
                stake2BlockIndex + LegacyStakeState.LockupInterval);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                stake2BlockIndex,
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                LegacyStakeState.RewardInterval,
                LegacyStakeState.LockupInterval);

            var validatorKey = new PrivateKey().PublicKey;
            state = DelegationUtil.EnsureValidatorPromotionReady(state, validatorKey, 0L);
            state = DelegationUtil.MakeGuild(state, _agentAddr, validatorKey.Address, 0L);

            var withdrawHeight = stake2BlockIndex + LegacyStakeState.LockupInterval + 1;
            // Withdraw stake via stake3.
            state = Stake3(state, _agentAddr, _avatarAddr, 0, withdrawHeight);

            var unbondedHeight = withdrawHeight + ValidatorDelegatee.ValidatorUnbondingPeriod;
            state = DelegationUtil.EnsureUnbondedClaimed(
                state, _agentAddr, unbondedHeight);

            // Stake 50 NCG via stake3 before patching.
            var firstStake3BlockIndex = unbondedHeight;
            state = Stake3(
                state,
                _agentAddr,
                _avatarAddr,
                stakedAmount,
                firstStake3BlockIndex);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                firstStake3BlockIndex,
                "StakeRegularFixedRewardSheet_V2",
                "StakeRegularRewardSheet_V2",
                50_400,
                201_600);

            // Patch StakePolicySheet and so on.
            state = state.SetLegacyState(
                Addresses.GetSheetAddress(nameof(StakePolicySheet)),
                StakePolicySheetFixtures.V3.Serialize());

            // Stake 50 NCG via stake3 after patching.
            state = Stake3(
                state,
                _agentAddr,
                _avatarAddr,
                stakedAmount,
                firstStake3BlockIndex + 1);

            // Validate staked.
            ValidateStakedStateV2(
                state,
                _agentAddr,
                stakedNCG,
                firstStake3BlockIndex + 1,
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                40,
                150);
        }

        private static IWorld MintAsset(
            IWorld state,
            Address recipient,
            FungibleAssetValue amount,
            long blockIndex)
        {
            return state.MintAsset(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = Addresses.Admin,
                    BlockIndex = blockIndex,
                },
                recipient,
                amount);
        }

        private static IWorld Stake2(
            IWorld state,
            Address agentAddr,
            long stakingAmount,
            long blockIndex)
        {
            var stake2 = new Stake2(stakingAmount);
            return stake2.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = agentAddr,
                    BlockIndex = blockIndex,
                });
        }

        private static IWorld Stake3(
            IWorld state,
            Address agentAddr,
            Address avatarAddr,
            long stakingAmount,
            long blockIndex)
        {
            var stake3 = new Stake(stakingAmount, avatarAddr);
            return stake3.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = agentAddr,
                    BlockIndex = blockIndex,
                });
        }

        private static IWorld ClaimStakeReward9(
            IWorld state,
            Address agentAddr,
            Address avatarAddr,
            long blockIndex)
        {
            var claimStakingReward9 = new ClaimStakeReward(avatarAddr);
            return claimStakingReward9.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = agentAddr,
                    BlockIndex = blockIndex,
                });
        }

        private static void ValidateStakedState(
            IWorldState state,
            Address agentAddr,
            FungibleAssetValue expectStakedAmount,
            long expectStartedBlockIndex)
        {
            var stakeAddr = LegacyStakeState.DeriveAddress(agentAddr);
            var actualStakedAmount = state.GetStaked(agentAddr);
            Assert.Equal(expectStakedAmount, actualStakedAmount);
            var stakeState = new LegacyStakeState((Dictionary)state.GetLegacyState(stakeAddr));
            Assert.Equal(expectStartedBlockIndex, stakeState.StartedBlockIndex);
        }

        private static void ValidateStakedStateV2(
            IWorldState state,
            Address agentAddr,
            FungibleAssetValue expectStakedAmount,
            long expectStartedBlockIndex,
            string expectStakeRegularFixedRewardSheetName,
            string expectStakeRegularRewardSheetName,
            long expectRewardInterval,
            long expectLockupInterval)
        {
            var stakeAddr = LegacyStakeState.DeriveAddress(agentAddr);
            var actualStakedAmount = state.GetStaked(agentAddr);
            Assert.Equal(expectStakedAmount, actualStakedAmount);
            var stakeState = new StakeState(state.GetLegacyState(stakeAddr));
            Assert.Equal(expectStartedBlockIndex, stakeState.StartedBlockIndex);
            Assert.Equal(
                expectStakeRegularFixedRewardSheetName,
                stakeState.Contract.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                expectStakeRegularRewardSheetName,
                stakeState.Contract.StakeRegularRewardSheetTableName);
            Assert.Equal(expectRewardInterval, stakeState.Contract.RewardInterval);
            Assert.Equal(expectLockupInterval, stakeState.Contract.LockupInterval);
        }
    }
}
