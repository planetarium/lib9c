namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Exceptions;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class CancelProductRegistrationTest
    {
        private readonly IWorld _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly GameConfigState _gameConfigState;

        public CancelProductRegistrationTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new MockWorld();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = LegacyModule.SetState(
                    _initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(currency);

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = LegacyModule.SetState(
                _initialState,
                GoldCurrencyState.Address,
                _goldCurrencyState.Serialize());
            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.Shop,
                new ShopState().Serialize());
            _initialState = AvatarModule.SetAvatarState(_initialState, _avatarAddress, avatarState);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Execute_Throw_InvalidAddressException(
            bool invalidAvatarAddress,
            bool invalidAgentAddress
        )
        {
            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = new List<IProductInfo>
                {
                    new ItemProductInfo
                    {
                        AvatarAddress = _avatarAddress,
                        AgentAddress = _agentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = Guid.NewGuid(),
                        Type = ProductType.NonFungible,
                    },
                    new ItemProductInfo
                    {
                        AvatarAddress = invalidAvatarAddress
                            ? new PrivateKey().ToAddress()
                            : _avatarAddress,
                        AgentAddress = invalidAgentAddress
                            ? new PrivateKey().ToAddress()
                            : _agentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = Guid.NewGuid(),
                        Type = ProductType.Fungible,
                    },
                },
            };

            var actionContext = new ActionContext
            {
                Signer = _agentAddress,
                BlockIndex = 1L,
                PreviousState = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<InvalidAddressException>(() => action.Execute(actionContext));
        }

        [Fact]
        public void Execute_Throw_ProductNotFoundException()
        {
            var context = new ActionContext();
            var prevState = LegacyModule.MintAsset(
                _initialState,
                context,
                _avatarAddress,
                1 * RuneHelper.StakeRune);
            var registerProduct = new RegisterProduct
            {
                AvatarAddress = _avatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new AssetInfo
                    {
                        AvatarAddress = _avatarAddress,
                        Price = 1 * _goldCurrencyState.Currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var nextWorld = registerProduct.Execute(new ActionContext
            {
                PreviousState = prevState,
                BlockIndex = 1L,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });
            Assert.Equal(
                0 * RuneHelper.StakeRune,
                LegacyModule.GetBalance(nextWorld, _avatarAddress, RuneHelper.StakeRune)
            );
            var productsState =
                new ProductsState(
                    (List)LegacyModule.GetState(nextWorld, ProductsState.DeriveAddress(_avatarAddress)));
            var productId = Assert.Single(productsState.ProductIds);

            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = new List<IProductInfo>
                {
                    new FavProductInfo
                    {
                        AgentAddress = _agentAddress,
                        AvatarAddress = _avatarAddress,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.FungibleAssetValue,
                    },
                    new FavProductInfo
                    {
                        AgentAddress = _agentAddress,
                        AvatarAddress = _avatarAddress,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };

            Assert.Throws<ProductNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousState = nextWorld,
                BlockIndex = 2L,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var productInfos = new List<IProductInfo>();
            for (int i = 0; i < CancelProductRegistration.Capacity + 1; i++)
            {
                productInfos.Add(new ItemProductInfo());
            }

            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = productInfos,
                ChargeAp = false,
            };

            var actionContext = new ActionContext();
            actionContext.PreviousState = new MockWorld();
            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(actionContext));
        }
    }
}
