namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class BuyProductTest
    {
        private static readonly Address BuyerAgentAddress = new ("47d082a115c63e7b58b1532d20e631538eafadde");
        private static readonly Address BuyerAvatarAddress = new ("340f110b91d0577a9ae0ea69ce15269436f217da");
        private static readonly Address SellerAgentAddress = new ("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
        private static readonly Address SellerAvatarAddress = new ("Fb90278C67f9b266eA309E6AE8463042f5461449");
        private static readonly Guid ProductId = Guid.NewGuid();
        private static readonly Currency Gold = Currency.Legacy("NCG", 2, null);
        private static readonly TableSheets TableSheets = new (TableSheetsImporter.ImportSheets());

        private static readonly ITradableItem TradableItem =
            (ITradableItem)ItemFactory.CreateItemUsable(TableSheets.EquipmentItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Armor), Guid.NewGuid(), 1L);

        private readonly Address _sellerAgentAddress2;
        private readonly Address _sellerAvatarAddress2;
        private readonly AvatarState _buyerAvatarState;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Guid _orderId;
        private IWorld _initialState;

        public BuyProductTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var context = new ActionContext();
            _initialState = new World(MockUtil.MockModernWorldState);
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(Gold);

            var sellerAgentState = new AgentState(SellerAgentAddress);
            var rankingMapAddress = new PrivateKey().Address;
            var sellerAvatarState = AvatarState.Create(
                SellerAvatarAddress,
                SellerAgentAddress,
                0,
                TableSheets.GetAvatarSheets(),
                rankingMapAddress);
            sellerAvatarState.worldInformation = new WorldInformation(
                0,
                TableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            sellerAgentState.avatarAddresses[0] = SellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().Address;
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().Address;
            var sellerAvatarState2 = AvatarState.Create(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
                0,
                TableSheets.GetAvatarSheets(),
                rankingMapAddress);
            sellerAvatarState2.worldInformation = new WorldInformation(
                0,
                TableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            var buyerAgentState = new AgentState(BuyerAgentAddress);
            _buyerAvatarState = AvatarState.Create(
                BuyerAvatarAddress,
                BuyerAgentAddress,
                0,
                TableSheets.GetAvatarSheets(),
                rankingMapAddress);
            _buyerAvatarState.worldInformation = new WorldInformation(
                0,
                TableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            buyerAgentState.avatarAddresses[0] = BuyerAvatarAddress;

            _orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            _initialState = _initialState
                .SetLegacyState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetAgentState(SellerAgentAddress, sellerAgentState)
                .SetAvatarState(SellerAvatarAddress, sellerAvatarState)
                .SetAgentState(_sellerAgentAddress2, agentState2)
                .SetAvatarState(_sellerAvatarAddress2, sellerAvatarState2)
                .SetAgentState(BuyerAgentAddress, buyerAgentState)
                .SetAvatarState(BuyerAvatarAddress, _buyerAvatarState)
                .MintAsset(context, BuyerAgentAddress, _goldCurrencyState.Currency * 1);
        }

        public static IEnumerable<object[]> Execute_MemberData()
        {
            yield return new object[]
            {
                new ExecuteMember
                {
                    ProductInfos = new List<IProductInfo>(),
                    Exc = typeof(ListEmptyException),
                },
                new ExecuteMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new ItemProductInfo
                        {
                            AvatarAddress = BuyerAvatarAddress,
                        },
                        new ItemProductInfo
                        {
                            AgentAddress = BuyerAgentAddress,
                        },
                        new FavProductInfo
                        {
                            AvatarAddress = BuyerAvatarAddress,
                        },
                        new FavProductInfo
                        {
                            AgentAddress = BuyerAgentAddress,
                        },
                    },
                    Exc = typeof(InvalidAddressException),
                },
                new ExecuteMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new ItemProductInfo
                        {
                            AvatarAddress = SellerAvatarAddress,
                            AgentAddress = SellerAgentAddress,
                        },
                    },
                    Exc = typeof(ProductNotFoundException),
                    ProductsState = new ProductsState(),
                },
                new ExecuteMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new FavProductInfo
                        {
                            AvatarAddress = SellerAvatarAddress,
                            AgentAddress = SellerAgentAddress,
                            ProductId = ProductId,
                            Price = 2 * Gold,
                            Type = ProductType.FungibleAssetValue,
                        },
                    },
                    Exc = typeof(InvalidPriceException),
                    ProductsState = new ProductsState
                    {
                        ProductIds = new List<Guid>
                        {
                            ProductId,
                        },
                    },
                    Product = new FavProduct
                    {
                        SellerAgentAddress = SellerAgentAddress,
                        SellerAvatarAddress = SellerAvatarAddress,
                        Asset = 1 * RuneHelper.StakeRune,
                        RegisteredBlockIndex = 1L,
                        ProductId = ProductId,
                        Price = 1 * Gold,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
                new ExecuteMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new ItemProductInfo
                        {
                            AvatarAddress = SellerAvatarAddress,
                            AgentAddress = SellerAgentAddress,
                            ProductId = ProductId,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                            TradableId = Guid.NewGuid(),
                            ItemSubType = ItemSubType.Belt,
                        },
                    },
                    Exc = typeof(InvalidTradableIdException),
                    ProductsState = new ProductsState
                    {
                        ProductIds = new List<Guid>
                        {
                            ProductId,
                        },
                    },
                    Product = new ItemProduct
                    {
                        SellerAgentAddress = SellerAgentAddress,
                        SellerAvatarAddress = SellerAvatarAddress,
                        RegisteredBlockIndex = 1L,
                        ProductId = ProductId,
                        Price = 1 * Gold,
                        Type = ProductType.NonFungible,
                        ItemCount = 1,
                        TradableItem = TradableItem,
                    },
                },
                new ExecuteMember
                {
                    ProductInfos = new IProductInfo[]
                    {
                        new ItemProductInfo
                        {
                            AvatarAddress = SellerAvatarAddress,
                            AgentAddress = SellerAgentAddress,
                            ProductId = ProductId,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                            TradableId = TradableItem.TradableId,
                            ItemSubType = ItemSubType.Belt,
                        },
                    },
                    Exc = typeof(InvalidItemTypeException),
                    ProductsState = new ProductsState
                    {
                        ProductIds = new List<Guid>
                        {
                            ProductId,
                        },
                    },
                    Product = new ItemProduct
                    {
                        SellerAgentAddress = SellerAgentAddress,
                        SellerAvatarAddress = SellerAvatarAddress,
                        RegisteredBlockIndex = 1L,
                        ProductId = ProductId,
                        Price = 1 * Gold,
                        Type = ProductType.NonFungible,
                        ItemCount = 1,
                        TradableItem = TradableItem,
                    },
                },
            };
        }

        [Theory]
        [MemberData(nameof(Execute_MemberData))]
        public void Execute_Throw_Exception(params ExecuteMember[] validateMembers)
        {
            foreach (var validateMember in validateMembers)
            {
                var previousState = _initialState;
                var productsState = validateMember.ProductsState;
                if (!(productsState is null))
                {
                    previousState = previousState.SetLegacyState(
                        ProductsState.DeriveAddress(SellerAvatarAddress),
                        productsState.Serialize());
                }

                var product = validateMember.Product;
                if (!(product is null))
                {
                    previousState = previousState.SetLegacyState(
                        Product.DeriveAddress(product.ProductId),
                        product.Serialize());
                }

                foreach (var productInfo in validateMember.ProductInfos)
                {
                    var action = new BuyProduct
                    {
                        AvatarAddress = BuyerAvatarAddress,
                        ProductInfos = new[] { productInfo, },
                    };
                    Assert.Throws(
                        validateMember.Exc,
                        () => action.Execute(
                            new ActionContext
                            {
                                PreviousState = previousState,
                                RandomSeed = 0,
                                Signer = BuyerAgentAddress,
                            }));
                }
            }
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var productInfos = new List<ItemProductInfo>();
            for (var i = 0; i < BuyProduct.Capacity + 1; i++)
            {
                productInfos.Add(new ItemProductInfo());
            }

            var action = new BuyProduct
            {
                AvatarAddress = _sellerAvatarAddress2,
                ProductInfos = productInfos,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(new ActionContext()));
        }

        [Fact]
        public void Mail_Serialize_BackwardCompatibility()
        {
            var favProduct = new FavProduct
            {
                SellerAgentAddress = SellerAgentAddress,
                SellerAvatarAddress = SellerAvatarAddress,
                Asset = 1 * RuneHelper.StakeRune,
                RegisteredBlockIndex = 1L,
                ProductId = ProductId,
                Price = 1 * Gold,
                Type = ProductType.FungibleAssetValue,
            };
            var itemProduct = new ItemProduct
            {
                SellerAgentAddress = SellerAgentAddress,
                SellerAvatarAddress = SellerAvatarAddress,
                RegisteredBlockIndex = 1L,
                ProductId = ProductId,
                Price = 1 * Gold,
                Type = ProductType.NonFungible,
                ItemCount = 1,
                TradableItem = TradableItem,
            };

            var buyerMail = new ProductBuyerMail(1L, ProductId, 1L, ProductId, favProduct);
            var buyerSerialized = (Dictionary)buyerMail.Serialize();
            var buyerDeserialized = new ProductBuyerMail(buyerSerialized);
            Assert.Equal(buyerSerialized, buyerDeserialized.Serialize());
            // serialized mail on v200220 buyerMail
            buyerSerialized = (Dictionary)buyerSerialized.Remove((Text)ProductBuyerMail.ProductKey);
            buyerDeserialized = new ProductBuyerMail(buyerSerialized);
            Assert.Equal(buyerDeserialized.ProductId, ProductId);
            Assert.Null(buyerDeserialized.Product);
            // check serialize not throw exception
            buyerDeserialized.Serialize();

            var sellerMail = new ProductSellerMail(1L, ProductId, 1L, ProductId, itemProduct);
            var sellerSerialized = (Dictionary)sellerMail.Serialize();
            var sellerDeserialized = new ProductSellerMail(sellerSerialized);
            Assert.Equal(sellerSerialized, sellerDeserialized.Serialize());
            // serialized mail on v200220 sellerMail
            sellerSerialized = (Dictionary)buyerSerialized.Remove((Text)ProductBuyerMail.ProductKey);
            sellerDeserialized = new ProductSellerMail(sellerSerialized);
            Assert.Equal(sellerDeserialized.ProductId, ProductId);
            Assert.Null(sellerDeserialized.Product);
            // check serialize not throw exception
            sellerDeserialized.Serialize();
        }

        public class ExecuteMember
        {
            public IEnumerable<IProductInfo> ProductInfos { get; set; }

            public Product Product { get; set; }

            public ProductsState ProductsState { get; set; }

            public Type Exc { get; set; }
        }
    }
}
