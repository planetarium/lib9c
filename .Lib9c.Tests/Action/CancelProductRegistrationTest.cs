namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Helper;
    using Lib9c.Model;
    using Lib9c.Model.Item;
    using Lib9c.Model.Mail;
    using Lib9c.Model.Market;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
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

            _initialState = new World(MockUtil.MockModernWorldState);
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(currency);

            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().Address;
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            var rankingMapAddress = new PrivateKey().Address;
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetLegacyState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetLegacyState(Addresses.Shop, new ShopState().Serialize())
                .SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax);
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
                            ? new PrivateKey().Address
                            : _avatarAddress,
                        AgentAddress = invalidAgentAddress
                            ? new PrivateKey().Address
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
                RandomSeed = 0,
            };
            Assert.Throws<InvalidAddressException>(() => action.Execute(actionContext));
        }

        [Fact]
        public void Execute_Throw_ProductNotFoundException()
        {
            var context = new ActionContext();
            var prevState = _initialState.MintAsset(context, _avatarAddress, 1 * RuneHelper.StakeRune);
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
            var nexState = registerProduct.Execute(
                new ActionContext
                {
                    PreviousState = prevState,
                    BlockIndex = 1L,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });
            Assert.Equal(
                0 * RuneHelper.StakeRune,
                nexState.GetBalance(_avatarAddress, RuneHelper.StakeRune)
            );
            var productsState =
                new ProductsState(
                    (List)nexState.GetLegacyState(ProductsState.DeriveAddress(_avatarAddress)));
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

            Assert.Throws<ProductNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = nexState,
                        BlockIndex = 2L,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var productInfos = new List<IProductInfo>();
            for (var i = 0; i < CancelProductRegistration.Capacity + 1; i++)
            {
                productInfos.Add(new ItemProductInfo());
            }

            var action = new CancelProductRegistration
            {
                AvatarAddress = _avatarAddress,
                ProductInfos = productInfos,
                ChargeAp = false,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(new ActionContext()));
        }

        [Theory]
        [InlineData(ProductType.FungibleAssetValue)]
        [InlineData(ProductType.NonFungible)]
        [InlineData(ProductType.Fungible)]
        public void Mail_Serialize_BackwardCompatibility(ProductType productType)
        {
            Product product;
            var gold = _goldCurrencyState.Currency;
            switch (productType)
            {
                case ProductType.FungibleAssetValue:
                    product = new FavProduct
                    {
                        SellerAgentAddress = new PrivateKey().Address,
                        SellerAvatarAddress = new PrivateKey().Address,
                        Asset = 1 * RuneHelper.StakeRune,
                        RegisteredBlockIndex = 1L,
                        ProductId = Guid.NewGuid(),
                        Price = 1 * gold,
                        Type = ProductType.FungibleAssetValue,
                    };
                    break;
                case ProductType.Fungible:
                case ProductType.NonFungible:
                {
                    var tradableItem = productType == ProductType.Fungible
                        ? ItemFactory.CreateTradableMaterial(_tableSheets.MaterialItemSheet.First)
                        : (ITradableItem)ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.First, Guid.NewGuid(), 0L);
                    product = new ItemProduct
                    {
                        SellerAgentAddress = new PrivateKey().Address,
                        SellerAvatarAddress = new PrivateKey().Address,
                        RegisteredBlockIndex = 1L,
                        ProductId = Guid.NewGuid(),
                        Price = 1 * gold,
                        Type = ProductType.NonFungible,
                        ItemCount = 1,
                        TradableItem = tradableItem,
                    };
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(productType), productType, null);
            }

            var mail = new ProductCancelMail(2L, Guid.NewGuid(), 2L, product!.ProductId, product!);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = new ProductCancelMail(serialized);
            Assert.Equal(serialized, deserialized.Serialize());
            // serialized mail on v200220;
            serialized = (Dictionary)serialized.Remove((Text)ProductCancelMail.ProductKey);
            deserialized = new ProductCancelMail(serialized);
            Assert.Equal(deserialized.ProductId, product.ProductId);
            Assert.Null(deserialized.Product);
            // check serialize not throw exception
            deserialized.Serialize();
        }
    }
}
