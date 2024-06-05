namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Bencodex;
    using Bencodex.Types;
    using Lib9c.Renderers;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blockchain.Renderers;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Battle;
    using Nekoyume.Blockchain;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RewardGoldTest
    {
        private readonly AvatarState _avatarState;
        private readonly AvatarState _avatarState2;
        private readonly IWorld _baseState;
        private readonly TableSheets _tableSheets;

        public RewardGoldTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(CharacterSheet)] = string.Join(
                    Environment.NewLine,
                    "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed",
                    "100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.Address;

            var avatarAddress = agentAddress.Derive("avatar");
            _tableSheets = new TableSheets(sheets);

            _avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _avatarState2 = new AvatarState(
                new PrivateKey().Address,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            IActionContext context = new ActionContext();
            _baseState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                .SetLegacyState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(context, GoldCurrencyState.Address, gold.Currency * 100000000000);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WeeklyArenaRankingBoard(bool resetCount, bool updateNext)
        {
            var weekly = new WeeklyArenaState(0);
            weekly.Set(_avatarState, _tableSheets.CharacterSheet);
            weekly[_avatarState.address].Update(
                weekly[_avatarState.address],
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScoreV4);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            IWorld state = new World(_baseState
                .SetLegacyState(weekly.address, weekly.Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize()));
            var blockIndex = 0;

            if (resetCount)
            {
                blockIndex = gameConfigState.DailyArenaInterval;
            }

            if (updateNext)
            {
                weekly[_avatarState.address].Activate();
                blockIndex = gameConfigState.WeeklyArenaInterval;
                // Avoid NRE in test case.
                var nextWeekly = new WeeklyArenaState(1);
                state = new World(state
                    .SetLegacyState(weekly.address, weekly.Serialize())
                    .SetLegacyState(nextWeekly.address, nextWeekly.Serialize()));
            }

            Assert.False(weekly.Ended);
            Assert.Equal(4, weekly[_avatarState.address].DailyChallengeCount);

            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousState = _baseState,
                Miner = default,
            };

            var states = new[]
            {
                action.WeeklyArenaRankingBoard2(ctx, state),
                action.WeeklyArenaRankingBoard(ctx, state),
            };

            foreach (var nextState in states)
            {
                var currentWeeklyState = nextState.GetWeeklyArenaState(0);
                var nextWeeklyState = nextState.GetWeeklyArenaState(1);

                if (resetCount || updateNext)
                {
                    Assert.NotEqual(
                        state.GetLegacyState(WeeklyArenaState.DeriveAddress(0)),
                        nextState.GetLegacyState(WeeklyArenaState.DeriveAddress(0)));
                }
                else
                {
                    Assert.Equal(
                        state.GetLegacyState(WeeklyArenaState.DeriveAddress(0)),
                        nextState.GetLegacyState(WeeklyArenaState.DeriveAddress(0)));
                }

                Assert.NotEqual(
                    state.GetLegacyState(WeeklyArenaState.DeriveAddress(1)),
                    nextState.GetLegacyState(WeeklyArenaState.DeriveAddress(1)));

                if (updateNext)
                {
                    Assert.NotEqual(
                        state.GetLegacyState(WeeklyArenaState.DeriveAddress(2)),
                        nextState.GetLegacyState(WeeklyArenaState.DeriveAddress(2)));
                    Assert.Equal(blockIndex, nextWeeklyState.ResetIndex);
                }

                if (resetCount)
                {
                    var expectedCount = updateNext ? 4 : 5;
                    var expectedIndex = updateNext ? 0 : blockIndex;
                    Assert.Equal(expectedCount, currentWeeklyState[_avatarState.address].DailyChallengeCount);
                    Assert.Equal(expectedIndex, currentWeeklyState.ResetIndex);
                }

                Assert.Equal(updateNext, currentWeeklyState.Ended);
                Assert.Contains(_avatarState.address, currentWeeklyState);
                Assert.Equal(updateNext, nextWeeklyState.ContainsKey(_avatarState.address));
            }
        }

        [Theory]
        // Migration from WeeklyArenaState.Map
        [InlineData(67, 68, 3_808_000L, 2)]
        // Update from WeeklyArenaList
        [InlineData(68, 69, 3_864_000L, 2)]
        // Filter deactivated ArenaInfo
        [InlineData(70, 71, 3_976_000L, 1)]
        public void PrepareNextArena(int prevWeeklyIndex, int targetWeeklyIndex, long blockIndex, int expectedCount)
        {
            var prevWeekly = new WeeklyArenaState(prevWeeklyIndex);
            var avatarAddress = _avatarState.address;
            var inactiveAvatarAddress = _avatarState2.address;
            bool afterUpdate = prevWeeklyIndex >= 68;
            bool filterInactive = blockIndex >= 3_976_000L;
            if (!afterUpdate)
            {
                prevWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
                prevWeekly[avatarAddress].Update(
                    prevWeekly[avatarAddress],
                    BattleLog.Result.Lose,
                    ArenaScoreHelper.GetScoreV4);
                prevWeekly.Set(_avatarState2, _tableSheets.CharacterSheet);

                Assert.Equal(4, prevWeekly[avatarAddress].DailyChallengeCount);
                Assert.False(prevWeekly[avatarAddress].Active);
                Assert.False(prevWeekly[inactiveAvatarAddress].Active);
            }

            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var targetWeekly = new WeeklyArenaState(targetWeeklyIndex);
            var state = _baseState
                .SetLegacyState(prevWeekly.address, prevWeekly.Serialize())
                .SetLegacyState(targetWeekly.address, targetWeekly.Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            if (afterUpdate)
            {
                var prevInfo = new ArenaInfo(_avatarState, _tableSheets.CharacterSheet, true);
                prevInfo.Update(
                    prevInfo,
                    BattleLog.Result.Lose,
                    ArenaScoreHelper.GetScoreV4);

                Assert.Equal(4, prevInfo.DailyChallengeCount);

                var inactiveInfo = new ArenaInfo(_avatarState2, _tableSheets.CharacterSheet, true);
                state = state
                    .SetLegacyState(
                        prevWeekly.address.Derive(avatarAddress.ToByteArray()),
                        prevInfo.Serialize())
                    .SetLegacyState(
                        prevWeekly.address.Derive(inactiveAvatarAddress.ToByteArray()),
                        inactiveInfo.Serialize())
                    .SetLegacyState(
                        prevWeekly.address.Derive("address_list"),
                        List.Empty
                            .Add(avatarAddress.Serialize())
                            .Add(inactiveAvatarAddress.Serialize()));
            }

            Assert.False(prevWeekly.Ended);

            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousState = _baseState,
                Miner = default,
            };

            var nextState = action.PrepareNextArena(ctx, state);
            var currentWeeklyState = nextState.GetWeeklyArenaState(prevWeeklyIndex);
            var preparedWeeklyState = nextState.GetWeeklyArenaState(targetWeeklyIndex);

            Assert.True(currentWeeklyState.Ended);
            Assert.True(
                nextState.TryGetLegacyState(
                    preparedWeeklyState.address.Derive(avatarAddress.ToByteArray()),
                    out Dictionary rawInfo
                )
            );

            var info = new ArenaInfo(rawInfo);

            Assert.Equal(GameConfig.ArenaChallengeCountMax, info.DailyChallengeCount);
            Assert.Equal(1000, info.Score);

            Assert.Equal(
                !filterInactive,
                nextState.TryGetLegacyState(
                    preparedWeeklyState.address.Derive(inactiveAvatarAddress.ToByteArray()),
                    out Dictionary inactiveRawInfo
                )
            );

            if (!filterInactive)
            {
                var inactiveInfo = new ArenaInfo(inactiveRawInfo);

                Assert.Equal(GameConfig.ArenaChallengeCountMax, inactiveInfo.DailyChallengeCount);
                Assert.Equal(1000, inactiveInfo.Score);
            }

            Assert.Empty(preparedWeeklyState.Map);
            Assert.True(
                nextState.TryGetLegacyState(
                    targetWeekly.address.Derive("address_list"),
                    out List rawList
                )
            );

            List<Address> addressList = rawList.ToList(StateExtensions.ToAddress);

            Assert.Contains(avatarAddress, addressList);
            Assert.Equal(!filterInactive, addressList.Contains(inactiveAvatarAddress));
            Assert.Equal(expectedCount, addressList.Count);
        }

        [Fact]
        public void ResetChallengeCount()
        {
            var legacyWeeklyIndex = RankingBattle11.UpdateTargetWeeklyArenaIndex - 1;
            var legacyWeekly = new WeeklyArenaState(legacyWeeklyIndex);
            legacyWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
            legacyWeekly[_avatarState.address].Update(
                legacyWeekly[_avatarState.address],
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScoreV4);

            Assert.Equal(4, legacyWeekly[_avatarState.address].DailyChallengeCount);

            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var migratedWeekly = new WeeklyArenaState(legacyWeeklyIndex + 1);
            var state = _baseState
                .SetLegacyState(legacyWeekly.address, legacyWeekly.Serialize())
                .SetLegacyState(migratedWeekly.address, migratedWeekly.Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            Assert.False(legacyWeekly.Ended);

            var action = new RewardGold();

            var migrationCtx = new ActionContext
            {
                BlockIndex = RankingBattle11.UpdateTargetBlockIndex,
                PreviousState = _baseState,
                Miner = default,
            };

            var arenaInfoAddress = migratedWeekly.address.Derive(_avatarState.address.ToByteArray());
            var addressListAddress = migratedWeekly.address.Derive("address_list");

            Assert.False(state.TryGetLegacyState(arenaInfoAddress, out Dictionary _));
            Assert.False(state.TryGetLegacyState(addressListAddress, out List _));

            // Ready to address list, ArenaInfo state.
            state = action.PrepareNextArena(migrationCtx, state);

            Assert.True(state.TryGetLegacyState(arenaInfoAddress, out Dictionary prevRawInfo));
            Assert.True(state.TryGetLegacyState(addressListAddress, out List _));

            var prevInfo = new ArenaInfo(prevRawInfo);
            prevInfo.Update(
                prevInfo,
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScoreV4);

            Assert.Equal(4, prevInfo.DailyChallengeCount);

            var blockIndex = RankingBattle11.UpdateTargetBlockIndex + gameConfigState.DailyArenaInterval;

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                Miner = default,
            };

            var nextState = action.ResetChallengeCount(ctx, state);

            Assert.True(state.TryGetLegacyState(arenaInfoAddress, out Dictionary rawInfo));
            Assert.True(state.TryGetLegacyState(addressListAddress, out List rawList));

            var updatedWeekly = nextState.GetWeeklyArenaState(migratedWeekly.address);
            var info = new ArenaInfo(rawInfo);
            List<Address> addressList = rawList.ToList(StateExtensions.ToAddress);

            Assert.Empty(updatedWeekly.Map);
            Assert.Equal(blockIndex, updatedWeekly.ResetIndex);
            Assert.Equal(5, info.DailyChallengeCount);
            Assert.Contains(_avatarState.address, addressList);
        }

        [Fact]
        public void GoldDistributedEachAccount()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            Currency currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            Address fund = GoldCurrencyState.Address;
            Address address1 = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Address address2 = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449");
            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = _baseState,
            };

            IWorld delta;

            // 제너시스에 받아야 할 돈들 검사
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999000000, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 1000000, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 1번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 1;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3599번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3599;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13601번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13601;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // Fund 잔액을 초과해서 송금하는 경우
            // EndBlock이 긴 순서대로 송금을 진행하기 때문에, 100이 송금 성공하고 10억이 송금 실패한다.
            ctx.BlockIndex = 2;
            Assert.Throws<InsufficientBalanceException>(() =>
            {
                delta = action.GenesisGoldDistribution(ctx, _baseState);
            });
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));
        }

        [Fact]
        public void MiningReward()
        {
            Address miner = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Currency currency = _baseState.GetGoldCurrency();
            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = _baseState,
                Miner = miner,
            };

            var action = new RewardGold();

            void AssertMinerReward(int blockIndex, string expected)
            {
                ctx.BlockIndex = blockIndex;
                IWorld delta = action.MinerReward(ctx, _baseState);
                Assert.Equal(FungibleAssetValue.Parse(currency, expected), delta.GetBalance(miner, currency));
            }

            // Before halving (10 / 2^0 = 10)
            AssertMinerReward(0, "10");
            AssertMinerReward(1, "10");
            AssertMinerReward(12614400, "10");

            // First halving (10 / 2^1 = 5)
            AssertMinerReward(12614401, "5");
            AssertMinerReward(25228800, "5");

            // Second halving (10 / 2^2 = 2.5)
            AssertMinerReward(25228801, "2.5");
            AssertMinerReward(37843200, "2.5");

            // Third halving (10 / 2^3 = 1.25)
            AssertMinerReward(37843201, "1.25");
            AssertMinerReward(50457600, "1.25");

            // Rewardless era
            AssertMinerReward(50457601, "0");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Genesis_StateRootHash(bool mainnet)
        {
            BlockPolicySource blockPolicySource = new BlockPolicySource();
            NCStagePolicy stagePolicy = new NCStagePolicy(default, 2);
            IBlockPolicy policy = blockPolicySource.GetPolicy();
            Block genesis;
            if (mainnet)
            {
                const string genesisBlockPath = "https://release.nine-chronicles.com/genesis-block-9c-main";
                var uri = new Uri(genesisBlockPath);
                using var client = new HttpClient();
                var rawBlock = await client.GetByteArrayAsync(uri);
                var blockDict = (Bencodex.Types.Dictionary)new Codec().Decode(rawBlock);
                genesis = BlockMarshaler.UnmarshalBlock(blockDict);
            }
            else
            {
                var adminPrivateKey = new PrivateKey();
                var adminAddress = adminPrivateKey.Address;
                var activatedAccounts = ImmutableHashSet<Address>.Empty;
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                var privateKey = new PrivateKey();
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(privateKey, nonce);
                var pendingActivationStates = new List<PendingActivationState>
                {
                    pendingActivation,
                };
                var initializeStates = new InitializeStates(
                    rankingState: new RankingState0(),
                    shopState: new ShopState(),
                    gameConfigState: new GameConfigState(),
                    redeemCodeState: new RedeemCodeState(Bencodex.Types.Dictionary.Empty
                        .Add("address", RedeemCodeState.Address.Serialize())
                        .Add("map", Bencodex.Types.Dictionary.Empty)
                    ),
                    adminAddressState: new AdminState(adminAddress, 1500000),
                    activatedAccountsState: new ActivatedAccountsState(activatedAccounts),
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    goldCurrencyState: new GoldCurrencyState(Currency.Legacy("NCG", 2, null)),
#pragma warning restore CS0618
                    goldDistributions: new GoldDistribution[0],
                    tableSheets: TableSheetsImporter.ImportSheets(),
                    pendingActivationStates: pendingActivationStates.ToArray()
                );
                var tempActionEvaluator = new ActionEvaluator(
                    policyBeginBlockActionsGetter: _ => policy.BeginBlockActions,
                    policyEndBlockActionsGetter: _ => policy.EndBlockActions,
                    stateStore: new TrieStateStore(new MemoryKeyValueStore()),
                    actionTypeLoader: new NCActionLoader());
                genesis = BlockChain.ProposeGenesisBlock(
                    transactions: ImmutableList<Transaction>.Empty
                        .Add(Transaction.Create(
                            0,
                            new PrivateKey(),
                            null,
                            new ActionBase[] { initializeStates }.ToPlainValues()))
                );
            }

            var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain.Create(
                policy: policy,
                store: store,
                stagePolicy: stagePolicy,
                stateStore: stateStore,
                genesisBlock: genesis,
                actionEvaluator: new ActionEvaluator(
                    policyBeginBlockActionsGetter: _ => policy.BeginBlockActions,
                    policyEndBlockActionsGetter: _ => policy.EndBlockActions,
                    stateStore: stateStore,
                    actionTypeLoader: new NCActionLoader()
                ),
                renderers: new IRenderer[] { new ActionRenderer(), new BlockRenderer() }
            );
            Assert.Equal(genesis.StateRootHash, blockChain.Genesis.StateRootHash);
        }

        [Theory]
        [InlineData(5, 4)]
        [InlineData(101, 100)]
        // Skip mead when InsufficientBalanceException occured.
        [InlineData(1, 0)]
        public void TransferMead(int patronMead, int balance)
        {
            var agentKey = new PrivateKey();
            var agentAddress = agentKey.Address;
            var patronAddress = new PrivateKey().Address;
            var contractAddress = agentAddress.GetPledgeAddress();
            IActionContext context = new ActionContext()
            {
                Txs = ImmutableList.Create<ITransaction>(
                    new Transaction(
                        new UnsignedTx(
                            new TxInvoice(
                                null,
                                DateTimeOffset.UtcNow,
                                new TxActionList(new List<IValue>()),
                                maxGasPrice: Currencies.Mead * 4,
                                gasLimit: 4),
                            new TxSigningMetadata(agentKey.PublicKey, 0)),
                        agentKey)),
            };
            IWorld states = new World(MockUtil.MockModernWorldState)
                .MintAsset(context, patronAddress, patronMead * Currencies.Mead)
                .TransferAsset(context, patronAddress, agentAddress, 1 * Currencies.Mead)
                .SetLegacyState(contractAddress, List.Empty.Add(patronAddress.Serialize()).Add(true.Serialize()).Add(balance.Serialize()))
                .BurnAsset(context, agentAddress, 1 * Currencies.Mead);
            Assert.Equal(balance * Currencies.Mead, states.GetBalance(patronAddress, Currencies.Mead));
            Assert.Equal(0 * Currencies.Mead, states.GetBalance(agentAddress, Currencies.Mead));

            var nextState = RewardGold.TransferMead(context, states);
            // transfer mead from patron to agent
            Assert.Equal(0 * Currencies.Mead, nextState.GetBalance(patronAddress, Currencies.Mead));
            Assert.Equal(balance * Currencies.Mead, nextState.GetBalance(agentAddress, Currencies.Mead));
        }

        [Fact]
        public void NoRewardWhenEmptySupply()
        {
            var weekly = new WeeklyArenaState(0);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);

            var currency = Currency.Legacy("NCG", 2, null);
            IWorld states = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(currency, 0).Serialize())
                .SetLegacyState(weekly.address, weekly.Serialize())
                .SetLegacyState(Addresses.GoldDistribution, new List())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            var action = new RewardGold();

            IWorld nextState = action.Execute(
                new ActionContext()
                {
                    BlockIndex = 42,
                    PreviousState = states,
                    Miner = default,
                }
            );

            Assert.Equal(0 * currency, nextState.GetBalance(default, currency));
        }
    }
}
