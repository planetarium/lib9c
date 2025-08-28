using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class BlockHelper
    {
        public static Block ProposeGenesisBlock(
            ValidatorSet validatorSet,
            IDictionary<string, string> tableSheets,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            AdminState? adminState = null,
            AuthorizedMinersState? authorizedMinersState = null,
            IImmutableSet<Address>? activatedAccounts = null,
            bool isActivateAdminAddress = false,
            IEnumerable<string>? credits = null,
            PrivateKey? minerPrivateKey = null,
            PrivateKey? signerPrivateKey = null,
            DateTimeOffset? timestamp = null,
            IEnumerable<ActionBase>? actionBases = null,
            Currency? goldCurrency = null,
            ISet<Address>? assetMinters = null,
            long initialSupply = GoldCurrencyState.DEFAULT_INITIAL_SUPPLY
        )
        {
            if (!tableSheets.TryGetValue(nameof(GameConfigSheet), out var csv))
            {
                throw new KeyNotFoundException(nameof(GameConfigSheet));
            }
            var gameConfigState = new GameConfigState(csv);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(tableSheets[nameof(RedeemCodeListSheet)]);

            minerPrivateKey ??= new PrivateKey();
            activatedAccounts ??= ImmutableHashSet<Address>.Empty;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            goldCurrency ??= Currency.Legacy("NCG", 2, minerPrivateKey.Address);
#pragma warning restore CS0618

            var initialStatesAction = new InitializeStates
            (
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: (Dictionary<string, string>) tableSheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: adminState,
                activatedAccountsState: new ActivatedAccountsState(
                    isActivateAdminAddress && !(adminState is null)  // Can't use 'not pattern' due to Unity
                    ? activatedAccounts.Add(adminState.AdminAddress)
                    : activatedAccounts),
                goldCurrencyState: new GoldCurrencyState(goldCurrency.Value, initialSupply),
                goldDistributions: goldDistributions,
                pendingActivationStates: pendingActivationStates,
                authorizedMinersState: authorizedMinersState,
                creditsState: credits is null ? null : new CreditsState(credits),
                assetMinters: assetMinters
            );
            List<ActionBase> actions = new List<ActionBase>
            {
                initialStatesAction,
            };
            IEnumerable<IAction> systemActions = new IAction[] { };
            if (!(actionBases is null))
            {
                actions.AddRange(actionBases);
            }
            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy();
            var actionLoader = new NCActionLoader();
            var actionEvaluator = new ActionEvaluator(
                policy.PolicyActionsRegistry,
                new TrieStateStore(new MemoryKeyValueStore()),
                actionLoader);
            return
                BlockChain.ProposeGenesisBlock(
                    privateKey: minerPrivateKey,
                    transactions: ImmutableList<Transaction>.Empty
                        .Add(Transaction.Create(
                            0, signerPrivateKey, null, actions.ToPlainValues()))
                        .AddRange(systemActions.Select((sa, index) =>
                            Transaction.Create(
                                index + 1, signerPrivateKey, null, new [] { sa.PlainValue }))),
                    timestamp: timestamp);
        }
    }
}
