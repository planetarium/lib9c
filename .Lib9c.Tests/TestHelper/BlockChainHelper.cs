namespace Lib9c.Tests.TestHelper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Lib9c.DevExtensions.Action;
    using Lib9c.Renderers;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public static class BlockChainHelper
    {
        public static BlockChain MakeBlockChain(
            BlockRenderer[] blockRenderers,
            IBlockPolicy policy = null,
            IStagePolicy stagePolicy = null,
            IStore store = null,
            IStateStore stateStore = null)
        {
            PrivateKey adminPrivateKey = new PrivateKey();

            policy ??= new BlockPolicy();
            stagePolicy ??= new VolatileStagePolicy();
            store ??= new DefaultStore(null);
            stateStore ??= new TrieStateStore(new DefaultKeyValueStore(null));
            Block genesis = MakeGenesisBlock(adminPrivateKey.Address, ImmutableHashSet<Address>.Empty);
            return BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBeginBlockActionsGetter: _ => policy.BeginBlockActions,
                    policyEndBlockActionsGetter: _ => policy.EndBlockActions,
                    stateStore: stateStore,
                    actionTypeLoader: new NCActionLoader()
                ),
                renderers: blockRenderers);
        }

        public static Block MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            DateTimeOffset? timestamp = null,
            PendingActivationState[] pendingActivations = null
        )
        {
            PrivateKey privateKey = new PrivateKey();
            if (pendingActivations is null)
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(privateKey, nonce);
                pendingActivations = new[] { pendingActivation };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.ProposeGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                activatedAccounts: activatedAddresses,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue);
        }

        public static MakeInitialStateResult MakeInitialState()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var goldCurrencyState = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var ranking = new RankingState1();
            for (var i = 0; i < RankingState1.RankingMapCapacity; i++)
            {
                ranking.RankingMap[RankingState1.Derive(i)] = new HashSet<Address>().ToImmutableHashSet();
            }

            var sheets = TableSheetsImporter.ImportSheets();
            var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(0);
            var context = new ActionContext();
            var initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetLegacyState(
                    Addresses.GoldDistribution,
                    GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .SetLegacyState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetLegacyState(Addresses.Ranking, ranking.Serialize())
                .SetLegacyState(weeklyArenaAddress, new WeeklyArenaState(0).Serialize());

            foreach (var (key, value) in sheets)
            {
                initialState = initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().Address;

            var agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);

            var avatarAddress = new PrivateKey().Address;
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = avatarAddress;

            var initCurrencyGold = goldCurrencyState.Currency * 100000000000;
            var agentCurrencyGold = goldCurrencyState.Currency * 1000;
            var remainCurrencyGold = initCurrencyGold - agentCurrencyGold;
            initialState = initialState
                .SetLegacyState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetLegacyState(Addresses.Shop, new ShopState().Serialize())
                .MintAsset(context, GoldCurrencyState.Address, initCurrencyGold)
                .TransferAsset(context, Addresses.GoldCurrency, agentAddress,  agentCurrencyGold);

            var action = new CreateTestbed
            {
                weeklyArenaAddress = weeklyArenaAddress,
            };
            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = initialState,
                RandomSeed = 0,
            });

            return new MakeInitialStateResult(
                nextState,
                action,
                agentState,
                avatarState,
                goldCurrencyState,
                rankingMapAddress,
                tableSheets,
                remainCurrencyGold,
                agentCurrencyGold);
        }
    }
}
