using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.Model.State;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Action.Guild.Migration.LegacyModels;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at NCG distribution(7e4515b6e14cc5d6eb716d5ebb587ab04b4246f9)
    /// Updated at https://github.com/planetarium/lib9c/pull/19
    /// Updated at https://github.com/planetarium/lib9c/pull/36
    /// Updated at https://github.com/planetarium/lib9c/pull/42
    /// Updated at https://github.com/planetarium/lib9c/pull/55
    /// Updated at https://github.com/planetarium/lib9c/pull/57
    /// Updated at https://github.com/planetarium/lib9c/pull/60
    /// Updated at https://github.com/planetarium/lib9c/pull/102
    /// Updated at https://github.com/planetarium/lib9c/pull/128
    /// Updated at https://github.com/planetarium/lib9c/pull/167
    /// Updated at https://github.com/planetarium/lib9c/pull/422
    /// Updated at https://github.com/planetarium/lib9c/pull/747
    /// Updated at https://github.com/planetarium/lib9c/pull/798
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("initialize_states")]
    public class InitializeStates : GameAction, IInitializeStatesV1
    {
        public IValue ValidatorSet { get; set; } = Null.Value;
        public Dictionary Ranking { get; set; } = Dictionary.Empty;
        public Dictionary Shop { get; set; } = Dictionary.Empty;
        public Dictionary<string, string> TableSheets { get; set; }
        public Dictionary GameConfig { get; set; } = Dictionary.Empty;
        public Dictionary RedeemCode { get; set; } = Dictionary.Empty;

        public Dictionary AdminAddressState { get; set; }

        public Dictionary ActivatedAccounts { get; set; } = Dictionary.Empty;

        public Dictionary GoldCurrency { get; set; } = Dictionary.Empty;

        public List GoldDistributions { get; set; } = List.Empty;

        public List PendingActivations { get; set; } = List.Empty;

        // This property can contain null:
        public Dictionary AuthorizedMiners { get; set; }

        // This property can contain null:
        public Dictionary Credits { get; set; }

        public ISet<Address> AssetMinters { get; set; }

        Dictionary IInitializeStatesV1.Ranking => Ranking;
        Dictionary IInitializeStatesV1.Shop => Shop;
        Dictionary<string, string> IInitializeStatesV1.TableSheets => TableSheets;
        Dictionary IInitializeStatesV1.GameConfig => GameConfig;
        Dictionary IInitializeStatesV1.RedeemCode => RedeemCode;
        Dictionary IInitializeStatesV1.AdminAddressState => AdminAddressState;
        Dictionary IInitializeStatesV1.ActivatedAccounts => ActivatedAccounts;
        Dictionary IInitializeStatesV1.GoldCurrency => GoldCurrency;
        List IInitializeStatesV1.GoldDistributions => GoldDistributions;
        List IInitializeStatesV1.PendingActivations => PendingActivations;
        Dictionary IInitializeStatesV1.AuthorizedMiners => AuthorizedMiners;
        Dictionary IInitializeStatesV1.Credits => Credits;

        public InitializeStates()
        {
        }

        public InitializeStates(
            ValidatorSet validatorSet,
            RankingState0 rankingState,
            ShopState shopState,
            Dictionary<string, string> tableSheets,
            GameConfigState gameConfigState,
            RedeemCodeState redeemCodeState,
            ActivatedAccountsState activatedAccountsState,
            GoldCurrencyState goldCurrencyState,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            AdminState adminAddressState = null,
            AuthorizedMinersState authorizedMinersState = null,
            CreditsState creditsState = null,
            ISet<Address> assetMinters = null)
        {
            ValidatorSet = validatorSet.Bencoded;
            Ranking = (Dictionary)rankingState.Serialize();
            Shop = (Dictionary)shopState.Serialize();
            TableSheets = tableSheets;
            GameConfig = (Dictionary)gameConfigState.Serialize();
            RedeemCode = (Dictionary)redeemCodeState.Serialize();
            AdminAddressState = (Dictionary)adminAddressState?.Serialize();
            ActivatedAccounts = (Dictionary)activatedAccountsState.Serialize();
            GoldCurrency = (Dictionary)goldCurrencyState.Serialize();
            GoldDistributions = new List(goldDistributions.Select(d => d.Serialize()));
            PendingActivations = new List(pendingActivationStates.Select(p => p.Serialize()));

            if (!(authorizedMinersState is null))
            {
                AuthorizedMiners = (Dictionary)authorizedMinersState.Serialize();
            }

            if (!(creditsState is null))
            {
                Credits = (Dictionary)creditsState.Serialize();
            }

            if (!(assetMinters is null))
            {
                AssetMinters = assetMinters;
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var weeklyArenaState = new WeeklyArenaState(0);

            var rankingState = new RankingState0(Ranking);

            if (ctx.BlockIndex != 0)
            {
                return states;
            }

#pragma warning disable LAA1002
            states = TableSheets
                .Aggregate(states, (current, pair) =>
                    current.SetLegacyState(Addresses.TableSheet.Derive(pair.Key), pair.Value.Serialize()));
            states = rankingState.RankingMap
                .Aggregate(states, (current, pair) =>
                    current.SetLegacyState(pair.Key, new RankingMapState(pair.Key).Serialize()));
#pragma warning restore LAA1002
            states = states
                .SetLegacyState(weeklyArenaState.address, weeklyArenaState.Serialize())
                .SetLegacyState(RankingState0.Address, Ranking)
                .SetLegacyState(ShopState.Address, Shop)
                .SetLegacyState(GameConfigState.Address, GameConfig)
                .SetLegacyState(RedeemCodeState.Address, RedeemCode)
                .SetLegacyState(ActivatedAccountsState.Address, ActivatedAccounts)
                .SetLegacyState(GoldCurrencyState.Address, GoldCurrency)
                .SetLegacyState(Addresses.GoldDistribution, GoldDistributions)
                .SetDelegationMigrationHeight(0);

            if (!(AdminAddressState is null))
            {
                states = states.SetLegacyState(AdminState.Address, AdminAddressState);
            }

            if (!(AuthorizedMiners is null))
            {
                states = states.SetLegacyState(
                    AuthorizedMinersState.Address,
                    AuthorizedMiners
                );
            }

            foreach (var rawPending in PendingActivations)
            {
                states = states.SetLegacyState(
                    new PendingActivationState((Dictionary)rawPending).address,
                    rawPending
                );
            }

            if (!(Credits is null))
            {
                states = states.SetLegacyState(CreditsState.Address, Credits);
            }

            var currencyState = new GoldCurrencyState(GoldCurrency);
            if (currencyState.InitialSupply > 0)
            {
                states = states.MintAsset(
                    ctx,
                    GoldCurrencyState.Address,
                    currencyState.Currency * currencyState.InitialSupply
                );
            }

            if (AssetMinters is { })
            {
                states = states.SetLegacyState(
                    Addresses.AssetMinters,
                    new List(AssetMinters.Select(addr => addr.Serialize()))
                );
            }

            var validatorSet = new ValidatorSet(ValidatorSet);
            foreach (var validator in validatorSet.Validators)
            {
                var delegationFAV = FungibleAssetValue.FromRawValue(
                    ValidatorDelegatee.ValidatorDelegationCurrency, validator.Power);
                states = states.MintAsset(ctx, StakeState.DeriveAddress(validator.OperatorAddress), delegationFAV);

                var validatorRepository = new ValidatorRepository(states, ctx);
                var validatorDelegatee = validatorRepository.CreateDelegatee(
                    validator.PublicKey, ValidatorDelegatee.DefaultCommissionPercentage);
                var validatorDelegator = validatorRepository.GetDelegator(validator.OperatorAddress);
                validatorDelegatee.Bond(validatorDelegator, delegationFAV, context.BlockIndex);

                var guildRepository = new GuildRepository(validatorRepository);
                var guildDelegatee = guildRepository.CreateDelegatee(validator.OperatorAddress);
                var guildDelegator = guildRepository.GetDelegator(validator.OperatorAddress);
                guildDelegator.Delegate(guildDelegatee, delegationFAV, context.BlockIndex);
                states = guildRepository.World;
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var rv = ImmutableDictionary<string, IValue>.Empty
                .Add("validator_set", ValidatorSet)
                .Add("ranking_state", Ranking)
                .Add("shop_state", Shop)
                .Add("table_sheets",
#pragma warning disable LAA1002
                    new Dictionary(TableSheets.Select(pair =>
                        new KeyValuePair<IKey, IValue>(
                            (Text)pair.Key, (Text)pair.Value))))
#pragma warning restore LAA1002
                .Add("game_config_state", GameConfig)
                .Add("redeem_code_state", RedeemCode)
                .Add("activated_accounts_state", ActivatedAccounts)
                .Add("gold_currency_state", GoldCurrency)
                .Add("gold_distributions", GoldDistributions)
                .Add("pending_activation_states", PendingActivations);

                if (!(AdminAddressState is null))
                {
                    rv = rv.Add("admin_address_state", AdminAddressState);
                }

                if (!(AuthorizedMiners is null))
                {
                    rv = rv.Add("authorized_miners_state", AuthorizedMiners);
                }

                if (!(Credits is null))
                {
                    rv = rv.Add("credits_state", Credits);
                }

                if (!(AssetMinters is null))
                {
                    rv = rv.Add("asset_minters", new List(AssetMinters.Select(addr => addr.Serialize())));
                }

                return rv;
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            ValidatorSet = plainValue["validator_set"];
            Ranking = (Dictionary) plainValue["ranking_state"];
            Shop = (Dictionary) plainValue["shop_state"];
            TableSheets = ((Dictionary) plainValue["table_sheets"])
                .ToDictionary(
                pair => (string)(Text) pair.Key,
                pair => (string)(Text) pair.Value
                );
            GameConfig = (Dictionary) plainValue["game_config_state"];
            RedeemCode = (Dictionary) plainValue["redeem_code_state"];
            ActivatedAccounts = (Dictionary)plainValue["activated_accounts_state"];
            GoldCurrency = (Dictionary)plainValue["gold_currency_state"];
            GoldDistributions = (List)plainValue["gold_distributions"];
            PendingActivations = (List)plainValue["pending_activation_states"];

            if (plainValue.TryGetValue("admin_address_state", out var adminAddressState))
            {
                AdminAddressState = (Dictionary)adminAddressState;
            }

            if (plainValue.TryGetValue("authorized_miners_state", out IValue authorizedMiners))
            {
                AuthorizedMiners = (Dictionary)authorizedMiners;
            }

            if (plainValue.TryGetValue("credits_state", out IValue credits))
            {
                Credits = (Dictionary)credits;
            }

            if (plainValue.TryGetValue("asset_minters", out IValue assetMinters))
            {
                AssetMinters = ((List)assetMinters).Select(addr => addr.ToAddress()).ToHashSet();
            }
        }
    }
}
