namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class MarketScenarioTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly AvatarState _sellerAvatarState;
        private readonly Address _sellerAgentAddress2;
        private readonly Address _sellerAvatarAddress2;
        private readonly AvatarState _sellerAvatarState2;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private readonly GameConfigState _gameConfigState;
        private IWorld _initialState;

        public MarketScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _sellerAgentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().Address;
            var rankingMapAddress = new PrivateKey().Address;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            _sellerAvatarState = AvatarState.Create(
                _sellerAvatarAddress,
                _sellerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            _sellerAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);
            _sellerAvatarState.actionPoint = DailyReward.ActionPointMax;

            agentState.avatarAddresses[0] = _sellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().Address;
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().Address;
            _sellerAvatarState2 = AvatarState.Create(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            _sellerAvatarState2.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);
            _sellerAvatarState2.actionPoint = DailyReward.ActionPointMax;

            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            _buyerAgentAddress = new PrivateKey().Address;
            var agentState3 = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().Address;
            var buyerAvatarState = AvatarState.Create(
                _buyerAvatarAddress,
                _buyerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            buyerAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);
            buyerAvatarState.actionPoint = DailyReward.ActionPointMax;

            agentState3.avatarAddresses[0] = _buyerAvatarAddress;

            _currency = Currency.Legacy("NCG", 2, minters: null);
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(_currency).Serialize())
                .SetLegacyState(Addresses.GameConfig, _gameConfigState.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<MaterialItemSheet>(), _tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<ArenaSheet>(), _tableSheets.ArenaSheet.Serialize())
                .SetAgentState(_sellerAgentAddress, agentState)
                .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState)
                .SetActionPoint(_sellerAvatarAddress, DailyReward.ActionPointMax)
                .SetAgentState(_sellerAgentAddress2, agentState2)
                .SetAvatarState(_sellerAvatarAddress2, _sellerAvatarState2)
                .SetActionPoint(_sellerAvatarAddress2, DailyReward.ActionPointMax)
                .SetAgentState(_buyerAgentAddress, agentState3)
                .SetAvatarState(_buyerAvatarAddress, buyerAvatarState);
        }

        [Fact]
        public void Register_And_Buy()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState2.inventory.AddItem(equipment);
            _initialState = _initialState
                .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState)
                .SetAvatarState(_sellerAvatarAddress2, _sellerAvatarState2)
                .MintAsset(context, _buyerAgentAddress, 4 * _currency)
                .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune)
                .MintAsset(context, _sellerAvatarAddress2, 1 * RuneHelper.DailyRewardRune);

            var random = new TestRandom();
            var productInfoList = new List<IProductInfo>();
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Asset = 1 * RuneHelper.StakeRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };

            var ctx = new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                Signer = _sellerAgentAddress,
            };
            ctx.SetRandom(random);
            var nextState = action.Execute(ctx);
            var nextAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp, nextState.GetActionPoint(_sellerAvatarAddress));

            var productsState =
                new ProductsState((List)nextState.GetLegacyState(ProductsState.DeriveAddress(_sellerAvatarAddress)));
            Assert.Equal(2, productsState.ProductIds.Count);
            foreach (var productId in productsState.ProductIds)
            {
                var product =
                    ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(Product.DeriveAddress(productId)));
                ProductType productType;
                switch (product)
                {
                    case FavProduct favProduct:
                        productType = ProductType.FungibleAssetValue;
                        productInfoList.Add(new FavProductInfo
                        {
                            AgentAddress = _sellerAgentAddress,
                            AvatarAddress = _sellerAvatarAddress,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                        });
                        break;
                    case ItemProduct itemProduct:
                        productType = itemProduct.TradableItem is TradableMaterial
                            ? ProductType.Fungible
                            : ProductType.NonFungible;
                        productInfoList.Add(new ItemProductInfo
                        {
                            AgentAddress = _sellerAgentAddress,
                            AvatarAddress = _sellerAvatarAddress,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                            ItemSubType = itemProduct.TradableItem.ItemSubType,
                            TradableId = itemProduct.TradableItem.TradableId,
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var action2 = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress2,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress2,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.ItemId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress2,
                        Price = 1 * _currency,
                        Asset = 1 * RuneHelper.DailyRewardRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            ctx = new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Signer = _sellerAgentAddress2,
            };
            ctx.SetRandom(random);
            var nextState2 = action2.Execute(ctx);
            var nextAvatarState2 = nextState2.GetAvatarState(_sellerAvatarAddress2);
            Assert.Empty(nextAvatarState2.inventory.Items);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp, nextState2.GetActionPoint(_sellerAvatarAddress2));

            var productList2 =
                new ProductsState((List)nextState2.GetLegacyState(ProductsState.DeriveAddress(_sellerAvatarAddress2)));
            Assert.Equal(2, productList2.ProductIds.Count);
            foreach (var productId in productList2.ProductIds)
            {
                var product =
                    ProductFactory.DeserializeProduct((List)nextState2.GetLegacyState(Product.DeriveAddress(productId)));
                ProductType productType;
                switch (product)
                {
                    case FavProduct favProduct:
                        productType = ProductType.FungibleAssetValue;
                        productInfoList.Add(new FavProductInfo
                        {
                            AgentAddress = _sellerAgentAddress2,
                            AvatarAddress = _sellerAvatarAddress2,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                        });
                        break;
                    case ItemProduct itemProduct:
                        productType = itemProduct.TradableItem is TradableMaterial
                            ? ProductType.Fungible
                            : ProductType.NonFungible;
                        productInfoList.Add(new ItemProductInfo
                        {
                            AgentAddress = _sellerAgentAddress2,
                            AvatarAddress = _sellerAvatarAddress2,
                            Price = product.Price,
                            ProductId = productId,
                            Type = productType,
                            ItemSubType = itemProduct.TradableItem.ItemSubType,
                            TradableId = itemProduct.TradableItem.TradableId,
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var action3 = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = productInfoList,
            };

            ctx = new ActionContext
            {
                BlockIndex = 3L,
                PreviousState = nextState2,
                Signer = _buyerAgentAddress,
            };
            ctx.SetRandom(random);
            var latestState = action3.Execute(ctx);

            var buyerAvatarState = latestState.GetAvatarState(_buyerAvatarAddress);
            var totalTax = 0 * _currency;
            foreach (var group in action3.ProductInfos.GroupBy(p => p.AgentAddress))
            {
                var sellerAgentAddress = group.Key;
                var totalPrice = 2 * _currency;
                var tax = totalPrice.DivRem(100, out _) * Buy.TaxRate;
                totalTax += tax;
                var taxedPrice = totalPrice - tax;
                Assert.Equal(taxedPrice, latestState.GetBalance(sellerAgentAddress, _currency));
                foreach (var productInfo in group)
                {
                    var sellerAvatarState = latestState.GetAvatarState(productInfo.AvatarAddress);
                    var sellProductList = new ProductsState((List)latestState.GetLegacyState(ProductsState.DeriveAddress(productInfo.AvatarAddress)));
                    var productId = productInfo.ProductId;
                    Assert.Empty(sellProductList.ProductIds);
                    Assert.Null(latestState.GetLegacyState(Product.DeriveAddress(productId)));
                    var product = ProductFactory.DeserializeProduct((List)nextState2.GetLegacyState(Product.DeriveAddress(productId)));
                    switch (product)
                    {
                        case FavProduct favProduct:
                            Assert.Equal(favProduct.Asset, latestState.GetBalance(_buyerAvatarAddress, favProduct.Asset.Currency));
                            break;
                        case ItemProduct itemProduct:
                            Assert.True(buyerAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 1L, itemProduct.ItemCount));
                            break;
                    }

                    var receipt = new ProductReceipt((List)latestState.GetLegacyState(ProductReceipt.DeriveAddress(productId)));
                    Assert.Equal(productId, receipt.ProductId);
                    Assert.Equal(productInfo.AvatarAddress, receipt.SellerAvatarAddress);
                    Assert.Equal(_buyerAvatarAddress, receipt.BuyerAvatarAddress);
                    Assert.Equal(1 * _currency, receipt.Price);
                    Assert.Equal(3L, receipt.PurchasedBlockIndex);
                    Assert.Contains(sellerAvatarState.mailBox.OfType<ProductSellerMail>(), m => m.ProductId == productInfo.ProductId);
                    Assert.Contains(buyerAvatarState.mailBox.OfType<ProductBuyerMail>(), m => m.ProductId == productInfo.ProductId);
                }
            }

            Assert.True(totalTax > 0 * _currency);
            Assert.Equal(0 * _currency, latestState.GetBalance(_buyerAgentAddress, _currency));
            Assert.Equal(totalTax, latestState.GetBalance(Addresses.RewardPool, _currency));
        }

        [Fact]
        public void Register_And_Cancel()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                    .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState)
                    .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune);
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.ItemId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                RandomSeed = 0,
                Signer = _sellerAgentAddress,
            });

            var nextAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp, nextState.GetActionPoint(_sellerAvatarAddress));

            var marketState = new MarketState(nextState.GetLegacyState(Addresses.Market));
            Assert.Contains(_sellerAvatarAddress, marketState.AvatarAddresses);

            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetLegacyState(productsStateAddress));
            var random = new TestRandom();
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();

                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * _currency, product.Price);
                switch (product)
                {
                    case FavProduct favProduct:
                        assetProductId = favProduct.ProductId;
                        Assert.Equal(1 * RuneHelper.StakeRune, favProduct.Asset);
                        break;
                    case ItemProduct itemProduct:
                        if (itemProduct.Type == ProductType.Fungible)
                        {
                            fungibleProductId = itemProduct.ProductId;
                        }
                        else
                        {
                            nonFungibleProductId = itemProduct.ProductId;
                        }

                        Assert.Equal(1, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                }
            }

            Assert.All(new[] { nonFungibleProductId, fungibleProductId, assetProductId }, productId => Assert.NotEqual(default, productId));
            var action2 = new CancelProductRegistration
            {
                AvatarAddress = _sellerAvatarAddress,
                ProductInfos = new List<IProductInfo>
                {
                    new ItemProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = fungibleProductId,
                        Type = ProductType.Fungible,
                        ItemSubType = tradableMaterial.ItemSubType,
                        TradableId = tradableMaterial.TradableId,
                    },
                    new ItemProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = nonFungibleProductId,
                        Type = ProductType.NonFungible,
                        ItemSubType = equipment.ItemSubType,
                        TradableId = equipment.ItemId,
                    },
                    new FavProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = assetProductId,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var latestState = action2.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                RandomSeed = 0,
                Signer = _sellerAgentAddress,
            });

            var latestAvatarState = latestState.GetAvatarState(_sellerAvatarAddress);
            foreach (var productInfo in action2.ProductInfos)
            {
                Assert.Contains(
                    latestAvatarState.mailBox.OfType<ProductCancelMail>(),
                    m => m.ProductId == productInfo.ProductId
                );
            }

            Assert.Equal(
                DailyReward.ActionPointMax - RegisterProduct.CostAp - CancelProductRegistration.CostAp,
                latestState.GetActionPoint(_sellerAvatarAddress));

            var sellProductList = new ProductsState((List)latestState.GetLegacyState(productsStateAddress));
            Assert.Empty(sellProductList.ProductIds);

            foreach (var productAddress in action2.ProductInfos.Select(productInfo => Product.DeriveAddress(productInfo.ProductId)))
            {
                Assert.Null(latestState.GetLegacyState(productAddress));
                var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, latestState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, latestState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                        Assert.True(latestAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 1L, itemProduct.ItemCount));
                        break;
                }
            }
        }

        [Fact]
        public void Register_And_ReRegister()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial, 2);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                .MintAsset(context, _sellerAvatarAddress, 2 * RuneHelper.StakeRune)
                .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState);
            var action = new RegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 2,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.ItemId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Asset = 2 * RuneHelper.StakeRune,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                RandomSeed = 0,
                Signer = _sellerAgentAddress,
            });

            var nextAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp, nextState.GetActionPoint(_sellerAvatarAddress));

            var marketState = new MarketState(nextState.GetLegacyState(Addresses.Market));
            Assert.Contains(_sellerAvatarAddress, marketState.AvatarAddresses);

            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetLegacyState(productsStateAddress));
            var random = new TestRandom();
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            for (int i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();
                switch (i)
                {
                    case 0:
                        fungibleProductId = guid;
                        break;
                    case 1:
                        assetProductId = guid;
                        break;
                    case 2:
                        nonFungibleProductId = guid;
                        break;
                }

                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, nextState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, nextState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                    {
                        var registerInfo =
                            action.RegisterInfos.OfType<RegisterInfo>().First(r =>
                                r.TradableId == itemProduct.TradableItem.TradableId);
                        Assert.Equal(product.ProductId, guid);
                        Assert.Equal(registerInfo.Price, product.Price);
                        Assert.Equal(registerInfo.ItemCount, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                    }
                }
            }

            var action2 = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                {
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = fungibleProductId,
                            Type = ProductType.Fungible,
                            ItemSubType = tradableMaterial.ItemSubType,
                            TradableId = tradableMaterial.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 1 * _currency,
                            TradableId = tradableMaterial.TradableId,
                            Type = ProductType.Fungible,
                        }
                    ),
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = nonFungibleProductId,
                            Type = ProductType.NonFungible,
                            ItemSubType = equipment.ItemSubType,
                            TradableId = equipment.ItemId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 1 * _currency,
                            TradableId = equipment.ItemId,
                            Type = ProductType.NonFungible,
                        }
                    ),
                    (
                        new FavProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = assetProductId,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            Price = 1 * _currency,
                            Asset = 1 * RuneHelper.StakeRune,
                            Type = ProductType.FungibleAssetValue,
                        }
                    ),
                },
            };
            var ctx = new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                Signer = _sellerAgentAddress,
            };
            ctx.SetRandom(random);
            var latestState = action2.Execute(ctx);

            var latestAvatarState = latestState.GetAvatarState(_sellerAvatarAddress);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp - ReRegisterProduct.CostAp, latestState.GetActionPoint(_sellerAvatarAddress));
            var inventoryItem = Assert.Single(latestAvatarState.inventory.Items);
            Assert.Equal(1, inventoryItem.count);
            Assert.IsType<TradableMaterial>(inventoryItem.item);
            var sellProductList = new ProductsState((List)latestState.GetLegacyState(productsStateAddress));
            Assert.Equal(3, sellProductList.ProductIds.Count);
            foreach (var prevProductId in productsState.ProductIds)
            {
                Assert.DoesNotContain(prevProductId, sellProductList.ProductIds);
            }

            foreach (var newProductId in sellProductList.ProductIds)
            {
                var productAddress = Product.DeriveAddress(newProductId);
                var product = ProductFactory.DeserializeProduct((List)latestState.GetLegacyState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        Assert.Equal(0 * RuneHelper.StakeRune, latestState.GetBalance(Product.DeriveAddress(assetProductId), RuneHelper.StakeRune));
                        Assert.Equal(1 * RuneHelper.StakeRune, latestState.GetBalance(_sellerAvatarAddress, RuneHelper.StakeRune));
                        Assert.Equal(favProduct.Asset, latestState.GetBalance(Product.DeriveAddress(favProduct.ProductId), RuneHelper.StakeRune));
                        break;
                    case ItemProduct itemProduct:
                    {
                        var registerInfo =
                            action2.ReRegisterInfos.Select(r => r.Item2).OfType<RegisterInfo>().First(r =>
                                r.TradableId == itemProduct.TradableItem.TradableId);
                        Assert.Equal(product.ProductId, newProductId);
                        Assert.Equal(registerInfo.Price, product.Price);
                        Assert.Equal(registerInfo.ItemCount, itemProduct.ItemCount);
                        Assert.NotNull(itemProduct.TradableItem);
                        break;
                    }
                }
            }
        }

        [Fact]
        public void ReRegister_Order()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            _initialState = _initialState.SetAvatarState(_sellerAvatarAddress, _sellerAvatarState);

            var digestListAddress = OrderDigestListState.DeriveAddress(_sellerAvatarAddress);
            var orderDigestList = new OrderDigestListState(digestListAddress);
            var reRegisterInfoList = new List<(IProductInfo, IRegisterInfo)>();
            var shopAddressList = new List<Address>();
            foreach (var inventoryItem in _sellerAvatarState.inventory.Items.ToList())
            {
                var tradableItem = (ITradableItem)inventoryItem.item;
                var itemSubType = tradableItem.ItemSubType;
                var orderId = Guid.NewGuid();
                var shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
                var shopState = new ShardedShopStateV2(shardedShopAddress);
                var order = OrderFactory.Create(
                    _sellerAgentAddress,
                    _sellerAvatarAddress,
                    orderId,
                    _currency * 1,
                    tradableItem.TradableId,
                    Order.ExpirationInterval,
                    itemSubType,
                    1
                );
                var sellItem = order.Sell(_sellerAvatarState);
                var orderDigest = order.Digest(_sellerAvatarState, _tableSheets.CostumeStatSheet);
                shopState.Add(orderDigest, Order.ExpirationInterval);
                orderDigestList.Add(orderDigest);
                Assert.True(_sellerAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));
                _initialState = _initialState.SetLegacyState(Addresses.GetItemAddress(tradableItem.TradableId), sellItem.Serialize())
                    .SetLegacyState(Order.DeriveAddress(order.OrderId), order.Serialize())
                    .SetLegacyState(digestListAddress, orderDigestList.Serialize())
                    .SetLegacyState(shardedShopAddress, shopState.Serialize())
                    .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState);

                var productType = tradableItem is TradableMaterial
                    ? ProductType.Fungible
                    : ProductType.NonFungible;
                var productInfo = new ItemProductInfo
                {
                    AgentAddress = _sellerAgentAddress,
                    AvatarAddress = _sellerAvatarAddress,
                    Price = 1 * _currency,
                    ProductId = orderId,
                    Type = productType,
                    Legacy = true,
                };
                var registerInfo = new RegisterInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    ItemCount = 1,
                    Price = 100 * _currency,
                    TradableId = tradableItem.TradableId,
                    Type = productType,
                };

                reRegisterInfoList.Add((productInfo, registerInfo));
                shopAddressList.Add(shardedShopAddress);
            }

            var action = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = reRegisterInfoList,
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = _initialState,
                RandomSeed = 0,
                Signer = _sellerAgentAddress,
            });

            Assert.Empty(new OrderDigestListState((Dictionary)nextState.GetLegacyState(digestListAddress)).OrderDigestList);
            Assert.Contains(
                _sellerAvatarAddress,
                new MarketState((List)nextState.GetLegacyState(Addresses.Market)).AvatarAddresses
            );
            var productsState =
                new ProductsState(
                    (List)nextState.GetLegacyState(ProductsState.DeriveAddress(_sellerAvatarAddress)));
            Assert.Equal(2, productsState.ProductIds.Count);
            foreach (var productId in productsState.ProductIds)
            {
                var productAddress = Product.DeriveAddress(productId);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(productAddress));
                Assert.Equal(100 * _currency, product.Price);
            }

            var nextAvatarState = nextState.GetAvatarState(_sellerAvatarAddress);
            Assert.Equal(DailyReward.ActionPointMax - ReRegisterProduct.CostAp, nextState.GetActionPoint(_sellerAvatarAddress));
            Assert.Empty(nextAvatarState.inventory.Items);

            foreach (var shopAddress in shopAddressList)
            {
                var shopState =
                    new ShardedShopStateV2((Dictionary)nextState.GetLegacyState(shopAddress));
                Assert.Empty(shopState.OrderDigestList);
            }
        }

        [Fact]
        public void HardFork()
        {
            var context = new ActionContext();
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 100L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState
                .SetAvatarState(_sellerAvatarAddress, _sellerAvatarState)
                .MintAsset(context, _buyerAgentAddress, 3 * _currency)
                .MintAsset(context, _sellerAvatarAddress, 1 * RuneHelper.StakeRune);
            var action = new RegisterProduct0
            {
                AvatarAddress = _sellerAvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.ItemId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        Type = ProductType.FungibleAssetValue,
                        Asset = 1 * RuneHelper.StakeRune,
                    },
                },
            };
            var random = new TestRandom();
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousState = _initialState,
                RandomSeed = random.Seed,
                Signer = _sellerAgentAddress,
            });
            Guid fungibleProductId = default;
            Guid nonFungibleProductId = default;
            Guid assetProductId = default;
            var productsStateAddress = ProductsState.DeriveAddress(_sellerAvatarAddress);
            var productsState = new ProductsState((List)nextState.GetLegacyState(productsStateAddress));
            foreach (var product in productsState.ProductIds.Select(Product.DeriveAddress)
                         .Select(productAddress =>
                             ProductFactory.DeserializeProduct(
                                 (List)nextState.GetLegacyState(productAddress))))
            {
                switch (product)
                {
                    case FavProduct favProduct:
                        assetProductId = favProduct.ProductId;
                        break;
                    case ItemProduct itemProduct:
                        if (itemProduct.Type == ProductType.Fungible)
                        {
                            fungibleProductId = itemProduct.ProductId;
                            Assert.Equal(0L, itemProduct.TradableItem.RequiredBlockIndex);
                        }
                        else
                        {
                            nonFungibleProductId = itemProduct.ProductId;
                            Assert.Equal(100L, itemProduct.TradableItem.RequiredBlockIndex);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }
            }

            var productInfos = new List<IProductInfo>
            {
                new ItemProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = fungibleProductId,
                    Type = ProductType.Fungible,
                    ItemSubType = tradableMaterial.ItemSubType,
                    TradableId = tradableMaterial.TradableId,
                },
                new ItemProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = nonFungibleProductId,
                    Type = ProductType.NonFungible,
                    ItemSubType = equipment.ItemSubType,
                    TradableId = equipment.ItemId,
                },
                new FavProductInfo
                {
                    AvatarAddress = _sellerAvatarAddress,
                    AgentAddress = _sellerAgentAddress,
                    Price = 1 * _currency,
                    ProductId = assetProductId,
                    Type = ProductType.FungibleAssetValue,
                },
            };
            //Cancel
            var cancelAction = new CancelProductRegistration
            {
                AvatarAddress = _sellerAvatarAddress,
                ProductInfos = productInfos,
            };
            var canceledState = cancelAction.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                RandomSeed = random.Seed,
                Signer = _sellerAgentAddress,
            });
            var avatarState = canceledState.GetAvatarState(_sellerAvatarAddress);
            Assert.Equal(2, avatarState.inventory.Items.Count);
            Assert.True(avatarState.inventory.TryGetTradableItem(tradableMaterial.TradableId, 0L, 1, out var materialItem));
            Assert.True(avatarState.inventory.TryGetNonFungibleItem(equipment.ItemId, out var equipmentItem));
            var canceledEquipment = Assert.IsAssignableFrom<ItemUsable>(equipmentItem.item);
            Assert.Equal(100L, canceledEquipment.RequiredBlockIndex);
            Assert.Equal(
                1 * Currencies.StakeRune,
                canceledState.GetBalance(_sellerAvatarAddress, Currencies.StakeRune)
            );

            //ReRegister
            var reRegisterAction = new ReRegisterProduct
            {
                AvatarAddress = _sellerAvatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                {
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = fungibleProductId,
                            Type = ProductType.Fungible,
                            ItemSubType = tradableMaterial.ItemSubType,
                            TradableId = tradableMaterial.TradableId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 2 * _currency,
                            TradableId = tradableMaterial.TradableId,
                            Type = ProductType.Fungible,
                        }
                    ),
                    (
                        new ItemProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = nonFungibleProductId,
                            Type = ProductType.NonFungible,
                            ItemSubType = equipment.ItemSubType,
                            TradableId = equipment.ItemId,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            ItemCount = 1,
                            Price = 2 * _currency,
                            TradableId = equipment.ItemId,
                            Type = ProductType.NonFungible,
                        }
                    ),
                    (
                        new FavProductInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            AgentAddress = _sellerAgentAddress,
                            Price = 1 * _currency,
                            ProductId = assetProductId,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = _sellerAvatarAddress,
                            Price = 2 * _currency,
                            Asset = 1 * RuneHelper.StakeRune,
                            Type = ProductType.FungibleAssetValue,
                        }
                    ),
                },
            };
            Assert.Throws<ItemDoesNotExistException>(() => reRegisterAction.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousState = nextState,
                RandomSeed = random.Seed,
                Signer = _sellerAgentAddress,
            }));

            //Buy
            var buyAction = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = productInfos,
            };

            var tradedState = buyAction.Execute(new ActionContext
            {
                BlockIndex = 3L,
                PreviousState = nextState,
                RandomSeed = random.Seed,
                Signer = _buyerAgentAddress,
            });

            var buyerAvatarState = tradedState.GetAvatarState(_buyerAvatarAddress);
            var totalTax = 0 * _currency;
            foreach (var group in buyAction.ProductInfos.GroupBy(p => p.AgentAddress))
            {
                var sellerAgentAddress = group.Key;
                var totalPrice = 3 * _currency;
                var tax = totalPrice.DivRem(100, out _) * Buy.TaxRate;
                totalTax += tax;
                var taxedPrice = totalPrice - tax;
                Assert.Equal(taxedPrice, tradedState.GetBalance(sellerAgentAddress, _currency));
                foreach (var productInfo in group)
                {
                    var sellerAvatarState = tradedState.GetAvatarState(productInfo.AvatarAddress);
                    var sellProductList = new ProductsState((List)tradedState.GetLegacyState(ProductsState.DeriveAddress(productInfo.AvatarAddress)));
                    var productId = productInfo.ProductId;
                    Assert.Empty(sellProductList.ProductIds);
                    Assert.Null(tradedState.GetLegacyState(Product.DeriveAddress(productId)));
                    var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(Product.DeriveAddress(productId)));
                    switch (product)
                    {
                        case FavProduct favProduct:
                            Assert.Equal(favProduct.Asset, tradedState.GetBalance(_buyerAvatarAddress, favProduct.Asset.Currency));
                            break;
                        case ItemProduct itemProduct:
                            Assert.True(buyerAvatarState.inventory.HasTradableItem(itemProduct.TradableItem.TradableId, 3L, itemProduct.ItemCount));
                            break;
                    }

                    var receipt = new ProductReceipt((List)tradedState.GetLegacyState(ProductReceipt.DeriveAddress(productId)));
                    Assert.Equal(productId, receipt.ProductId);
                    Assert.Equal(productInfo.AvatarAddress, receipt.SellerAvatarAddress);
                    Assert.Equal(_buyerAvatarAddress, receipt.BuyerAvatarAddress);
                    Assert.Equal(1 * _currency, receipt.Price);
                    Assert.Equal(3L, receipt.PurchasedBlockIndex);
                    Assert.Contains(sellerAvatarState.mailBox.OfType<ProductSellerMail>(), m => m.ProductId == productInfo.ProductId);
                    Assert.Contains(buyerAvatarState.mailBox.OfType<ProductBuyerMail>(), m => m.ProductId == productInfo.ProductId);
                }
            }

            Assert.True(totalTax > 0 * _currency);
            Assert.Equal(0 * _currency, tradedState.GetBalance(_buyerAgentAddress, _currency));
            Assert.Equal(totalTax, tradedState.GetBalance(Addresses.RewardPool, _currency));
        }

        [Theory]
        [ClassData(typeof(ExecuteWithCustomEquipmentData))]
        public void Grinding_Custom_Equipment_And_Register(int[] equipmentIds, (int, int)[] expected)
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var crystalCurrency = Currencies.Crystal;
            var random = new TestRandom();
            var state = _initialState
                .SetLegacyState(
                    Addresses.GetSheetAddress<CrystalMonsterCollectionMultiplierSheet>(),
                    _tableSheets.CrystalMonsterCollectionMultiplierSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<CrystalEquipmentGrindingSheet>(),
                    _tableSheets.CrystalEquipmentGrindingSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<MaterialItemSheet>(),
                    _tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<StakeRegularRewardSheet>(),
                    _tableSheets.StakeRegularRewardSheet.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetActionPoint(avatarAddress, 120);
            Assert.Equal(0 * crystalCurrency, state.GetBalance(avatarAddress, crystalCurrency));

            state = Execute_Grinding_Custom_Equipment(
                state,
                agentAddress,
                avatarAddress,
                random,
                equipmentIds,
                _tableSheets.EquipmentItemSheet
            );
            var expectedAsset = equipmentIds.Length * crystalCurrency;
            Assert.Equal(expectedAsset, state.GetBalance(agentAddress, crystalCurrency));
            Assert.Equal(115, state.GetActionPoint(avatarAddress));

            state = Execute_Register_Custom_Equipment(
                state,
                agentAddress,
                avatarAddress,
                random,
                expected,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet,
                _tableSheets.MaterialItemSheet
            );
        }

        private static IWorld Execute_Grinding_Custom_Equipment(
            IWorld state,
            Address agentAddress,
            Address avatarAddress,
            IRandom random,
            int[] equipmentRowIds,
            EquipmentItemSheet equipmentItemSheet)
        {
            var avatarState = state.GetAvatarState(avatarAddress);

            var testRandom = new TestRandom();
            // Create equipment with ItemFactory
            var equipmentList = new List<Equipment>();
            foreach (var equipmentItemId in equipmentRowIds)
            {
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentItemSheet[equipmentItemId],
                    testRandom.GenerateRandomGuid(),
                    1);
                equipment.ByCustomCraft = true;
                equipmentList.Add(equipment);
            }

            foreach (var equipment in equipmentList)
            {
                avatarState.inventory.AddItem(equipment);
            }

            state = state.SetInventory(avatarAddress, avatarState.inventory);

            var grinding = new Grinding
            {
                AvatarAddress = avatarAddress,
                EquipmentIds = equipmentList.Select(e => e.ItemId).ToList(),
                ChargeAp = false,
            };
            var nextState = grinding.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddress,
                BlockIndex = 1,
                RandomSeed = random.Seed,
            });

            return nextState;
        }

        private static IWorld Execute_Register_Custom_Equipment(
            IWorld state,
            Address agentAddress,
            Address avatarAddress,
            IRandom random,
            (int id, int count)[] expectedReward,
            WorldSheet worldSheet,
            WorldUnlockSheet worldUnlockSheet,
            MaterialItemSheet materialItemSheet)
        {
            var avatarState = state.GetAvatarState(avatarAddress);
            var price = 1 * Currency.Legacy("NCG", 2, minters: null);
            var registerInfos = expectedReward.Select(expected =>
            {
                materialItemSheet.TryGetValue(expected.id, out var materialRow);
                Assert.NotNull(materialRow);
                Assert.True(materialRow.ItemSubType is ItemSubType.Circle or ItemSubType.Scroll);
                avatarState.inventory.TryGetFungibleItems(materialRow.ItemId, out var items);
                Assert.Single(items);
                var item = items.First();

                Assert.Equal(expected.count, item.count);
                Assert.IsAssignableFrom<ITradableFungibleItem>(item.item);
                return new RegisterInfo
                {
                    AvatarAddress = avatarAddress,
                    Price = price,
                    TradableId = ((ITradableFungibleItem)item.item).TradableId,
                    ItemCount = item.count,
                    Type = ProductType.Fungible,
                };
            });

            for (int i = 0; i < GameConfig.RequireClearedStageLevel.ActionsInShop; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, worldSheet, worldUnlockSheet);
            }

            state = state.SetWorldInformation(avatarAddress, avatarState.worldInformation);

            var register = new RegisterProduct
            {
                AvatarAddress = avatarAddress,
                RegisterInfos = registerInfos,
                ChargeAp = false,
            };
            var nextState = register.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = agentAddress,
                BlockIndex = 2,
                RandomSeed = random.Seed,
            });

            return nextState;
        }

        private class ExecuteWithCustomEquipmentData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                new object[]
                {
                    new[] { 20160000, 20160001, 20160002 },
                    new[]
                    {
                        (600401, 14),
                        (600402, 21),
                    },
                },
                new object[]
                {
                    new[] { 20160000, 20260000, 20360000, 20460000, 20560000 },
                    new[]
                    {
                        (600401, 10),
                        (600402, 15),
                    },
                },
                new object[]
                {
                    new[] { 20160003, 20260000, 20360001, 20360001, 20460001 },
                    new[]
                    {
                        (600401, 24), // 10 + 2 + 4 + 4 + 4 = 24
                        (600402, 47), // 20 + 3 + 8 + 8 + 8 = 47
                    },
                },
                new object[]
                {
                    new[] { 20560003, 20560003, 20560003 },
                    new[]
                    {
                        (600401, 30), // 10 + 10 + 10 = 30
                        (600402, 60), // 20 + 20 + 20 = 60
                    },
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
