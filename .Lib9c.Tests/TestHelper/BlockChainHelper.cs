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
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
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
            Block genesis = MakeGenesisBlock(adminPrivateKey.ToAddress(), ImmutableHashSet<Address>.Empty);
            return BlockChain.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
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
            IWorld initialState = new Tests.Action.MockWorld();
            initialState = LegacyModule.SetState(
                initialState,
                GoldCurrencyState.Address,
                goldCurrencyState.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                Addresses.GoldDistribution,
                GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                Addresses.GameConfig,
                new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                Addresses.Ranking,
                ranking.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                weeklyArenaAddress,
                new WeeklyArenaState(0).Serialize());

            foreach (var (key, value) in sheets)
            {
                initialState = LegacyModule.SetState(
                    initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();

            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
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
            initialState = LegacyModule.SetState(
                initialState,
                GoldCurrencyState.Address,
                goldCurrencyState.Serialize());
            initialState = AgentModule.SetAgentState(initialState, agentAddress, agentState);
            initialState = AvatarModule.SetAvatarState(
                initialState,
                avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);
            initialState = LegacyModule.SetState(
                initialState,
                Addresses.Shop,
                new ShopState().Serialize());
            initialState = LegacyModule.MintAsset(
                initialState,
                context,
                GoldCurrencyState.Address,
                initCurrencyGold);
            initialState = LegacyModule.TransferAsset(
                initialState,
                context,
                Addresses.GoldCurrency,
                agentAddress,
                agentCurrencyGold);

            var action = new CreateTestbed
            {
                weeklyArenaAddress = weeklyArenaAddress,
            };
            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = initialState,
                Random = new TestRandom(),
                Rehearsal = false,
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
