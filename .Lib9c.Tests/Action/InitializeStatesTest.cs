namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Consensus;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class InitializeStatesTest
    {
        private readonly Dictionary<string, string> _sheets;

        public InitializeStatesTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
        }

        [Fact]
        public void Execute()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (privateKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: new AdminState(
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    1500000
                ),
                activatedAccountsState: new ActivatedAccountsState(),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: new[] { pendingActivation }
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Signer = minterKey.Address,
                Miner = default,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var addresses = new List<Address>()
            {
                Addresses.Ranking,
                Addresses.Shop,
                Addresses.GameConfig,
                Addresses.RedeemCode,
                Addresses.Admin,
                Addresses.ActivatedAccount,
                Addresses.GoldCurrency,
                Addresses.GoldDistribution,
                activationKey.PendingAddress,
            };
            addresses.AddRange(_sheets.Select(kv => Addresses.TableSheet.Derive(kv.Key)));

            foreach (var address in addresses)
            {
                Assert.NotNull(genesisState.GetLegacyState(address));
            }
        }

        [Fact]
        public void ExecuteWithAuthorizedMinersState()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (privateKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: new AdminState(
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    1500000
                ),
                activatedAccountsState: new ActivatedAccountsState(),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: new[] { pendingActivation },
                authorizedMinersState: new AuthorizedMinersState(
                    interval: 50,
                    validUntil: 1000,
                    miners: new[] { default(Address) }
                )
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var fetchedState = new AuthorizedMinersState(
                (Dictionary)genesisState.GetLegacyState(AuthorizedMinersState.Address)
            );

            Assert.Equal(50, fetchedState.Interval);
            Assert.Equal(1000, fetchedState.ValidUntil);
            Assert.Equal(new[] { default(Address) }, fetchedState.Miners);
        }

        [Fact]
        public void ExecuteWithActivateAdminKey()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var adminAddress = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (privateKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: new AdminState(adminAddress, 1500000),
                activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty.Add(adminAddress)),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: new[] { pendingActivation }
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var fetchedState = new ActivatedAccountsState(
                (Dictionary)genesisState.GetLegacyState(Addresses.ActivatedAccount));

            Assert.Contains(adminAddress, fetchedState.Accounts);
        }

        [Fact]
        public void ExecuteWithCredits()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var adminAddress = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            var creditState = new CreditsState(
                new[]
                {
                    "John Smith",
                    "홍길동",
                    "山田太郎",
                }
            );
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (minterKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: new AdminState(adminAddress, 1500000),
                activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty.Add(adminAddress)),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: new PendingActivationState[0],
                creditsState: creditState
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var fetchedState = new CreditsState(
                (Dictionary)genesisState.GetLegacyState(CreditsState.Address));

            Assert.Equal(creditState.Names, fetchedState.Names);
        }

        [Fact]
        public void ExecuteWithoutAdminState()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions =
                GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (privateKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: null,
                activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: new[] { pendingActivation }
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var fetchedState = new ActivatedAccountsState(
                (Dictionary)genesisState.GetLegacyState(Addresses.ActivatedAccount));
            Assert.Empty(fetchedState.Accounts);

            Assert.Null(genesisState.GetLegacyState(Addresses.Admin));
        }

        [Fact]
        public void ExecuteWithoutInitialSupply()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributions = Array.Empty<GoldDistribution>();
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            var validatorSet = new ValidatorSet(new List<Validator> { new (privateKey.PublicKey, 10) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: null,
                activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty),
                goldCurrencyState: new GoldCurrencyState(ncg, 0),
                goldDistributions: goldDistributions,
                pendingActivationStates: Array.Empty<PendingActivationState>()
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            Assert.Equal(0 * ncg, genesisState.GetBalance(GoldCurrencyState.Address, ncg));
        }

        [Fact]
        public void ExecuteWithAssetMinters()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(_sheets[nameof(RedeemCodeListSheet)]);
            var goldDistributionCsvPath = GoldDistributionTest.CreateFixtureCsvFile();
            var goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var minterKey = new PrivateKey();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var ncg = Currency.Legacy("NCG", 2, minterKey.Address);
#pragma warning restore CS0618
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var adminAddress = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            var validatorSet = new ValidatorSet(
                new List<Validator> { new (minterKey.PublicKey, 1_000_000_000_000_000_000) });

            var action = new InitializeStates(
                validatorSet: validatorSet,
                rankingState: new RankingState0(),
                shopState: new ShopState(),
                tableSheets: _sheets,
                gameConfigState: gameConfigState,
                redeemCodeState: new RedeemCodeState(redeemCodeListSheet),
                adminAddressState: new AdminState(adminAddress, 1500000),
                activatedAccountsState: new ActivatedAccountsState(ImmutableHashSet<Address>.Empty.Add(adminAddress)),
                goldCurrencyState: new GoldCurrencyState(ncg),
                goldDistributions: goldDistributions,
                pendingActivationStates: Array.Empty<PendingActivationState>(),
                assetMinters: new[] { default(Address) }.ToHashSet()
            );

            var genesisState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                Miner = default,
                Signer = minterKey.Address,
                PreviousState = new World(MockUtil.MockModernWorldState),
            });

            var assetMinters = Assert.IsType<List>(genesisState.GetLegacyState(Addresses.AssetMinters));
            Assert.Contains(default(Address).Serialize(), assetMinters);
        }
    }
}
