namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData.Stake;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ClaimStakeRewardTest
    {
        private const string AgentAddressHex = "0x0000000001000000000100000000010000000001";
        private const int AvatarIndex = 0;
        private static readonly Address AgentAddr = new Address(AgentAddressHex);

        private static readonly Address AvatarAddr =
            Addresses.GetAvatarAddress(AgentAddr, AvatarIndex);

        private readonly IWorld _initialState;
        private readonly Currency _ncg;
        private readonly StakePolicySheet _stakePolicySheet;

        public ClaimStakeRewardTest(ITestOutputHelper outputHelper)
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
            IWorld initialStatesWithAvatarState;
            (
                _,
                _,
                _,
                initialStatesWithAvatarState) = InitializeUtil.InitializeStates(
                agentAddr: AgentAddr,
                avatarIndex: AvatarIndex,
                sheetsOverride: sheetsOverride);
            _initialState = initialStatesWithAvatarState;
            _ncg = initialStatesWithAvatarState.GetGoldCurrency();
            _stakePolicySheet = initialStatesWithAvatarState.GetSheet<StakePolicySheet>();
        }

        // NOTE: object[] {
        //     long startedBlockIndex,
        //     long? receivedBlockIndex,
        //     long stakedBalance,
        //     long blockIndex,
        //     (Address balanceAddr, FungibleAssetValue fav)[] expectedBalances,
        //     (int itemSheetId, int count)[] expectedItems)
        // }
        public static IEnumerable<object[]>
            GetMemberData_Execute_Success_With_StakePolicySheetFixtureV1()
        {
            // NOTE:
            // - minimum required_gold of StakeRegularRewardSheetFixtures.V1 is 50.
            // - RewardInterval of StakePolicySheetFixtures.V1 is 50,400.

            // NOTE: staking level 1
            yield return new object[]
            {
                0, null, 50, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 5),
                    (500_000, 1),
                },
            };

            // NOTE: staking level 2
            yield return new object[]
            {
                0, null, 500, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 62),
                    (500_000, 2),
                },
            };

            // NOTE: staking level 3
            yield return new object[]
            {
                0, null, 5000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 1000),
                    (500_000, 2 + 6),
                },
            };

            // NOTE: staking level 4
            yield return new object[]
            {
                0, null, 50_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 8),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 10_000),
                    (500_000, 2 + 62),
                },
            };

            // NOTE: staking level 5
            yield return new object[]
            {
                0, null, 500_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 83),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 100_000),
                    (500_000, 2 + 625),
                },
            };

            // NOTE: staking level 6
            yield return new object[]
            {
                0, null, 5_000_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 833),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 1_000_000),
                    (500_000, 2 + 6_250),
                },
            };

            // NOTE: staking level 7
            yield return new object[]
            {
                0, null, 10_000_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 1_666),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 2_000_000),
                    (500_000, 2 + 12_500),
                },
            };
        }

        // NOTE: object[] {
        //     long startedBlockIndex,
        //     long? receivedBlockIndex,
        //     long stakedBalance,
        //     long blockIndex,
        //     (Address balanceAddr, FungibleAssetValue fav)[] expectedBalances,
        //     (int itemSheetId, int count)[] expectedItems)
        public static IEnumerable<object[]>
            GetMemberData_Execute_Success_With_StakePolicySheetFixtureV2()
        {
            // NOTE:
            // - minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
            // - RewardInterval of StakePolicySheetFixtures.V2 is 50,400.

            // NOTE: staking level 1
            yield return new object[]
            {
                0, null, 50, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 5),
                    (500_000, 1),
                },
            };

            // NOTE: staking level 2
            yield return new object[]
            {
                0, null, 500, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 125),
                    (500_000, 2),
                },
            };

            // NOTE: staking level 3
            yield return new object[]
            {
                0, null, 5000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 0),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 2500),
                    (500_000, 2 + 12),
                },
            };

            // NOTE: staking level 4
            yield return new object[]
            {
                0, null, 50_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 8),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 25_000),
                    (500_000, 2 + 125),
                },
            };

            // NOTE: staking level 5
            yield return new object[]
            {
                0, null, 500_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 83),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 250_000),
                    (500_000, 2 + 1_250),
                },
            };

            // NOTE: staking level 6
            yield return new object[]
            {
                0, null, 5_000_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 0),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 833),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 2_500_000),
                    (500_000, 2 + 12_500),
                },
            };

            // NOTE: staking level 7
            yield return new object[]
            {
                0, null, 10_000_000, 50_400,
                new (Address balanceAddr, FungibleAssetValue fav)[]
                {
                    (AgentAddr, Currencies.Garage * 100_000),
                    (AvatarAddr, Currencies.GetRune("RUNE_GOLDENLEAF") * 1_666),
                },
                new (int itemSheetId, int count)[]
                {
                    (400_000, 5_000_000),
                    (500_000, 2 + 250_00),
                    (600_201, 200_000),
                    (800_201, 200_000),
                },
            };
        }

        [Fact]
        public void Constructor()
        {
            var action = new ClaimStakeReward(AvatarAddr);
            Assert.Equal(AvatarAddr, action.AvatarAddress);
        }

        [Fact]
        public void Serde()
        {
            var action = new ClaimStakeReward(AvatarAddr);
            var ser = action.PlainValue;
            var des = new ClaimStakeReward();
            des.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.AvatarAddress, des.AvatarAddress);
            Assert.Equal(ser, des.PlainValue);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_When_Staking_State_Null()
        {
            Assert.Throws<FailedLoadStateException>(() =>
                Execute(
                    _initialState,
                    AgentAddr,
                    AvatarAddr,
                    0));

            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var previousState = _initialState.RemoveLegacyState(stakeAddr);
            Assert.Throws<FailedLoadStateException>(() =>
                Execute(
                    previousState,
                    AgentAddr,
                    AvatarAddr,
                    0));
        }

        [Theory]
        [InlineData(0, null, 0)]
        [InlineData(0, null, LegacyStakeState.RewardInterval - 1)]
        [InlineData(0, LegacyStakeState.RewardInterval - 2, LegacyStakeState.RewardInterval - 1)]
        [InlineData(0, LegacyStakeState.RewardInterval, LegacyStakeState.RewardInterval + 1)]
        [InlineData(0, LegacyStakeState.RewardInterval, LegacyStakeState.RewardInterval * 2 - 1)]
        [InlineData(0, LegacyStakeState.RewardInterval * 2 - 2, LegacyStakeState.RewardInterval * 2 - 1)]
        [InlineData(0, LegacyStakeState.RewardInterval * 2, LegacyStakeState.RewardInterval * 2 + 1)]
        public void Execute_Throw_RequiredBlockIndexException_With_StakeState(
            long startedBlockIndex,
            long? receivedBlockIndex,
            long blockIndex)
        {
            var stakeAddr = LegacyStakeState.DeriveAddress(AgentAddr);
            var stakeState = new LegacyStakeState(stakeAddr, startedBlockIndex);
            if (receivedBlockIndex is not null)
            {
                stakeState.Claim((long)receivedBlockIndex);
            }

            var prevState = _initialState
                // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 50)
                .SetLegacyState(stakeAddr, stakeState.Serialize());
            Assert.Throws<RequiredBlockIndexException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Theory]
        // NOTE: RewardInterval of StakePolicySheetFixtures.V2 is 50,400.
        [InlineData(0, null, 0)]
        [InlineData(0, null, 50_400 - 1)]
        [InlineData(0, 50_400 - 1, 50_400 - 1)]
        [InlineData(0, 50_400, 50_400 + 1)]
        [InlineData(0, 50_400, 50_400 * 2 - 1)]
        [InlineData(0, 50_400 * 2 - 2, 50_400 * 2 - 1)]
        [InlineData(0, 50_400 * 2, 50_400 * 2 + 1)]
        public void Execute_Throw_RequiredBlockIndexException_With_StakeStateV2(
            long startedBlockIndex,
            long? receivedBlockIndex,
            long blockIndex)
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(
                _stakePolicySheet,
                startedBlockIndex,
                receivedBlockIndex);
            var prevState = _initialState
                // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 50)
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            Assert.Throws<RequiredBlockIndexException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_When_Sheet_Null()
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(_stakePolicySheet, 0, null);
            var blockIndex = stakeStateV2.StartedBlockIndex + stakeStateV2.Contract.RewardInterval;
            var prevState = _initialState
                // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 50)
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            // NOTE: Set StakeRegularFixedRewardSheetTable to Null
            var sheetAddr = Addresses.GetSheetAddress(
                stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName);
            prevState = prevState.RemoveLegacyState(sheetAddr);
            Assert.Throws<FailedLoadStateException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));

            prevState = _initialState
                // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 50)
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            // NOTE: Set StakeRegularRewardSheetTableName to Null
            sheetAddr = Addresses.GetSheetAddress(
                stakeStateV2.Contract.StakeRegularRewardSheetTableName);
            prevState = prevState.RemoveLegacyState(sheetAddr);
            Assert.Throws<FailedLoadStateException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(49)]
        [InlineData(0)]
        public void Execute_Throw_InsufficientBalanceException(long stakedBalance)
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(_stakePolicySheet, 0, null);
            var blockIndex = stakeStateV2.StartedBlockIndex + stakeStateV2.Contract.RewardInterval;
            var previousState = _initialState.SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            previousState = stakedBalance > 0
                ? previousState.MintAsset(
                    new ActionContext(),
                    stakeAddr,
                    _ncg * stakedBalance)
                : previousState;
            Assert.Throws<InsufficientBalanceException>(() =>
                Execute(
                    previousState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Fact]
        public void Execute_Throw_ArgumentNullException_When_Reward_CurrencyTicker_Null()
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(_stakePolicySheet, 0, null);
            var blockIndex = stakeStateV2.StartedBlockIndex + stakeStateV2.Contract.RewardInterval;
            var prevState = _initialState
                // NOTE: required_gold to receive Currency
                // of StakeRegularRewardSheetFixtures.V2 is 10,000,000.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 10_000_000)
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            // NOTE: Set CurrencyTicker to string.Empty.
            var sheetAddr = Addresses.GetSheetAddress(
                stakeStateV2.Contract.StakeRegularRewardSheetTableName);
            var sheetValue = prevState.GetSheetCsv(sheetAddr);
            sheetValue = string.Join('\n', sheetValue.Split('\n')
                .Select(line => string.Join(',', line.Split(',')
                    .Select((column, index) => index == 5
                        ? string.Empty
                        : column))));
            prevState = prevState.SetLegacyState(sheetAddr, sheetValue.Serialize());
            Assert.Throws<ArgumentNullException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Fact]
        public void
            Execute_Throw_ArgumentNullException_When_Reward_CurrencyTicker_New_CurrencyDecimalPlaces_Null()
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(_stakePolicySheet, 0, null);
            var blockIndex = stakeStateV2.StartedBlockIndex + stakeStateV2.Contract.RewardInterval;
            var prevState = _initialState
                // NOTE: required_gold to receive Currency
                // of StakeRegularRewardSheetFixtures.V2 is 10,000,000.
                .MintAsset(new ActionContext(), stakeAddr, _ncg * 10_000_000)
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            // NOTE: Set CurrencyTicker to string.Empty.
            var sheetAddr = Addresses.GetSheetAddress(
                stakeStateV2.Contract.StakeRegularRewardSheetTableName);
            var sheetValue = prevState.GetSheetCsv(sheetAddr);
            sheetValue = string.Join('\n', sheetValue.Split('\n')
                .Select(line => string.Join(',', line.Split(',')
                    .Select((column, index) =>
                    {
                        return index switch
                        {
                            5 => "NEW_TICKER",
                            6 => string.Empty,
                            _ => column,
                        };
                    }))));
            prevState = prevState.SetLegacyState(sheetAddr, sheetValue.Serialize());
            Assert.Throws<ArgumentNullException>(() =>
                Execute(
                    prevState,
                    AgentAddr,
                    AvatarAddr,
                    blockIndex));
        }

        [Theory]
        [MemberData(nameof(GetMemberData_Execute_Success_With_StakePolicySheetFixtureV1))]
        public void Execute_Success_With_StakeState(
            long startedBlockIndex,
            long? receivedBlockIndex,
            long stakedBalance,
            long blockIndex,
            (Address balanceAddr, FungibleAssetValue fav)[] expectedBalances,
            (int itemSheetId, int count)[] expectedItems)
        {
            var stakeAddr = LegacyStakeState.DeriveAddress(AgentAddr);
            var stakeState = new LegacyStakeState(stakeAddr, startedBlockIndex);
            if (receivedBlockIndex is not null)
            {
                stakeState.Claim((long)receivedBlockIndex);
            }

            var previousState = stakedBalance > 0
                ? _initialState.MintAsset(
                    new ActionContext(),
                    stakeAddr,
                    _ncg * stakedBalance)
                : _initialState;
            previousState = previousState.SetLegacyState(stakeAddr, stakeState.Serialize());
            var nextState = Execute(
                previousState,
                AgentAddr,
                AvatarAddr,
                blockIndex);
            Expect(
                nextState,
                expectedBalances,
                AvatarAddr,
                expectedItems);
        }

        [Theory]
        [MemberData(nameof(GetMemberData_Execute_Success_With_StakePolicySheetFixtureV2))]
        public void Execute_Success_With_StakeStateV2(
            long startedBlockIndex,
            long? receivedBlockIndex,
            long stakedBalance,
            long blockIndex,
            (Address balanceAddr, FungibleAssetValue fav)[] expectedBalances,
            (int itemSheetId, int count)[] expectedItems)
        {
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakeStateV2 = PrepareStakeStateV2(
                _stakePolicySheet,
                startedBlockIndex,
                receivedBlockIndex);
            var previousState = stakedBalance > 0
                ? _initialState.MintAsset(
                    new ActionContext(),
                    stakeAddr,
                    _ncg * stakedBalance)
                : _initialState;
            previousState = previousState.SetLegacyState(stakeAddr, stakeStateV2.Serialize());
            var nextState = Execute(
                previousState,
                AgentAddr,
                AvatarAddr,
                blockIndex);
            Expect(
                nextState,
                expectedBalances,
                AvatarAddr,
                expectedItems);
        }

        [Fact]
        public void Execute_V6()
        {
            var prevState = _initialState;
            var stakeAddr = StakeState.DeriveAddress(AgentAddr);
            var stakePolicySheet = new StakePolicySheet();
            stakePolicySheet.Set(StakePolicySheetFixtures.V6);
            var stakeStateV2 = PrepareStakeStateV2(
                stakePolicySheet,
                0L,
                0L);
            prevState = prevState
                .SetLegacyState(
                    Addresses.GetSheetAddress<StakePolicySheet>(),
                    StakePolicySheetFixtures.V6.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress("StakeRegularRewardSheet_V6"),
                    StakeRegularRewardSheetFixtures.V6.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress("StakeRegularRewardFixedRewardSheet_V3"),
                    StakeRegularFixedRewardSheetFixtures.V3.Serialize())
                .SetLegacyState(stakeAddr, stakeStateV2.Serialize())
                .MintAsset(
                    new ActionContext(),
                    stakeAddr,
                    _ncg * 10000000);
            var agentAddr = AgentAddr;
            var avatarAddr = AvatarAddr;
            var blockIndex = stakePolicySheet.RewardIntervalValue;
            var nextState = Execute(prevState, agentAddr, avatarAddr, blockIndex);
            var avatarState = nextState.GetAvatarState(AvatarAddr);
            var expected = new[]
            {
                (400000, 25000000, true),
                (500000, 125002, true),
                (600201, 200000, false),
                (800201, 200000, false),
                (800202, 200000, false),
            };
            foreach (var (itemId, count, tradable) in expected)
            {
                Assert.True(avatarState.inventory.TryGetItem(itemId, out var inventoryItem));
                Assert.Equal(count, inventoryItem.count);
                if (tradable)
                {
                    Assert.IsType<TradableMaterial>(inventoryItem.item);
                }
                else
                {
                    Assert.IsNotType<TradableMaterial>(inventoryItem.item);
                    Assert.IsType<Material>(inventoryItem.item);
                }
            }
        }

        private static StakeState PrepareStakeStateV2(
            StakePolicySheet stakePolicySheet,
            long startedBlockIndex,
            long? receivedBlockIndex)
        {
            var contract = new Contract(stakePolicySheet);
            return receivedBlockIndex is null
                ? new StakeState(contract, startedBlockIndex)
                : new StakeState(contract, startedBlockIndex, receivedBlockIndex.Value);
        }

        private static IWorld Execute(
            IWorld prevState,
            Address agentAddr,
            Address avatarAddr,
            long blockIndex)
        {
            var stakeAddr = StakeState.DeriveAddress(agentAddr);
            var ncg = prevState.GetGoldCurrency();
            var prevBalance = prevState.GetBalance(agentAddr, ncg);
            var prevStakedBalance = prevState.GetBalance(stakeAddr, ncg);
            var action = new ClaimStakeReward(avatarAddr);
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = prevState,
                Signer = agentAddr,
                BlockIndex = blockIndex,
            });
            var nextBalance = nextState.GetBalance(agentAddr, ncg);
            Assert.Equal(prevBalance, nextBalance);
            var nextStakedBalance = nextState.GetBalance(stakeAddr, ncg);
            Assert.Equal(prevStakedBalance, nextStakedBalance);
            Assert.True(nextState.TryGetStakeStateV2(agentAddr, out var stakeStateV2));
            Assert.Equal(blockIndex, stakeStateV2.ReceivedBlockIndex);
            Assert.True(stakeStateV2.ClaimedBlockIndex <= blockIndex);
            Assert.True(stakeStateV2.ClaimableBlockIndex > blockIndex);

            return nextState;
        }

        private static void Expect(
            IWorldState state,
            (Address balanceAddr, FungibleAssetValue fav)[] expectedBalances,
            Address avatarAddr,
            (int itemSheetId, int count)[] expectedItems)
        {
            if (expectedBalances is not null)
            {
                foreach (var (balanceAddr, fav) in expectedBalances)
                {
                    Assert.Equal(fav, state.GetBalance(balanceAddr, fav.Currency));
                }
            }

            if (expectedItems is not null)
            {
                var inventory = state.GetInventoryV2(avatarAddr);
                foreach (var (itemSheetId, count) in expectedItems)
                {
                    Assert.Equal(count, inventory.Items.First(e => e.item.Id == itemSheetId).count);
                }
            }
        }
    }
}
