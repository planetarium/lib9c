namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
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
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private IAccountStateDelta _initialState;

        public MarketScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _sellerAvatarState = new AvatarState(
                _sellerAvatarAddress,
                _sellerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _sellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().ToAddress();
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().ToAddress();
            _sellerAvatarState2 = new AvatarState(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var agentState3 = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            _buyerAvatarState = new AvatarState(
                _buyerAvatarAddress,
                _buyerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState3.avatarAddresses[0] = _buyerAvatarAddress;

            _currency = Currency.Legacy("NCG", 2, minters: null);
            _initialState = new Tests.Action.State()
                .SetState(GoldCurrencyState.Address, new GoldCurrencyState(_currency).Serialize())
                .SetState(_sellerAgentAddress, agentState.Serialize())
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                .SetState(_sellerAgentAddress2, agentState2.Serialize())
                .SetState(_sellerAvatarAddress2, _sellerAvatarState2.Serialize())
                .SetState(_buyerAgentAddress, agentState3.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize());
        }

        [Fact]
        public void Register_And_BuyItem()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState2.inventory.AddItem(equipment);
            _initialState = _initialState
                .SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize())
                .SetState(_sellerAvatarAddress2, _sellerAvatarState2.Serialize())
                .MintAsset(_buyerAgentAddress, 2 * _currency);

            var random = new TestRandom();
            var action = new RegisterItem
            {
                RegisterInfos = new List<RegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = random,
                Signer = _sellerAgentAddress,
            });
            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            var productList =
                new ProductList((List)nextState.GetState(ProductList.DeriveAddress(_sellerAvatarAddress)));
            var productId = Assert.Single(productList.ProductIdList);

            var action2 = new RegisterItem
            {
                RegisterInfos = new List<RegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = _sellerAvatarAddress2,
                        ItemCount = 1,
                        Price = 1 * _currency,
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                },
            };
            var nextState2 = action2.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousStates = nextState,
                Random = random,
                Signer = _sellerAgentAddress2,
            });
            var nextAvatarState2 = nextState2.GetAvatarStateV2(_sellerAvatarAddress2);
            Assert.Empty(nextAvatarState2.inventory.Items);
            var productList2 =
                new ProductList((List)nextState2.GetState(ProductList.DeriveAddress(_sellerAvatarAddress2)));
            var productId2 = Assert.Single(productList2.ProductIdList);

            var action3 = new BuyItem
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfoList = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AgentAddress = _sellerAgentAddress,
                        AvatarAddress = _sellerAvatarAddress,
                        Price = 1 * _currency,
                        ProductId = productId,
                    },
                    new ProductInfo
                    {
                        AgentAddress = _sellerAgentAddress2,
                        AvatarAddress = _sellerAvatarAddress2,
                        Price = 1 * _currency,
                        ProductId = productId2,
                    },
                },
            };

            var latestState = action3.Execute(new ActionContext
            {
                BlockIndex = 3L,
                PreviousStates = nextState2,
                Random = random,
                Signer = _buyerAgentAddress,
            });

            var buyerAvatarState = latestState.GetAvatarStateV2(_buyerAvatarAddress);
            foreach (var productInfo in action3.ProductInfoList)
            {
                Assert.Equal(1 * _currency, latestState.GetBalance(productInfo.AgentAddress, _currency));
                var sellProductList = new ProductList((List)latestState.GetState(ProductList.DeriveAddress(productInfo.AvatarAddress)));
                Assert.Empty(sellProductList.ProductIdList);
                Assert.Equal(Null.Value, latestState.GetState(Product.DeriveAddress(productInfo.ProductId)));
                var product = new Product((List)nextState2.GetState(Product.DeriveAddress(productInfo.ProductId)));
                Assert.True(buyerAvatarState.inventory.HasTradableItem(product.TradableItem.TradableId, 1L, product.ItemCount));
            }

            Assert.Equal(0 * _currency, latestState.GetBalance(_buyerAgentAddress, _currency));
        }

        [Fact]
        public void Register_And_Cancel()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _sellerAvatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _sellerAvatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _sellerAvatarState.inventory.Items.Count);
            _initialState = _initialState.SetState(_sellerAvatarAddress, _sellerAvatarState.Serialize());
            var action = new RegisterItem
            {
                RegisterInfos = new List<RegisterInfo>
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
                        TradableId = equipment.TradableId,
                        Type = ProductType.NonFungible,
                    },
                },
            };
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_sellerAvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);

            var marketState = new MarketState(nextState.GetState(Addresses.Market));
            Assert.Contains(_sellerAvatarAddress, marketState.AvatarAddressList);

            var productListAddress = ProductList.DeriveAddress(_sellerAvatarAddress);
            var productList = new ProductList((List)nextState.GetState(productListAddress));
            var random = new TestRandom();
            for (int i = 0; i < 2; i++)
            {
                var guid = random.GenerateRandomGuid();
                Assert.Contains(guid, productList.ProductIdList);
                var productAddress = Product.DeriveAddress(guid);
                var product = new Product((List)nextState.GetState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * _currency, product.Price);
                Assert.Equal(1, product.ItemCount);
                Assert.NotNull(product.TradableItem);
            }

            var action2 = new CancelItemRegistration
            {
                AvatarAddress = _sellerAvatarAddress,
                ProductInfoList = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = productList.ProductIdList.First(),
                    },
                    new ProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Price = 1 * _currency,
                        ProductId = productList.ProductIdList.Last(),
                    },
                },
            };
            var latestState = action.Execute(new ActionContext
            {
                BlockIndex = 2L,
                PreviousStates = nextState,
                Random = new TestRandom(),
                Signer = _sellerAgentAddress,
            });

            var latestAvatarState = latestState.GetAvatarStateV2(_sellerAvatarAddress);
            var sellProductList = new ProductList((List)latestState.GetState(productListAddress));
            Assert.Empty(sellProductList.ProductIdList);

            foreach (var productAddress in action2.ProductInfoList.Select(productInfo => Product.DeriveAddress(productInfo.ProductId)))
            {
                Assert.Equal(Null.Value, latestState.GetState(productAddress));
                var product = new Product((List)nextState.GetState(productAddress));
                Assert.True(latestAvatarState.inventory.HasTradableItem(product.TradableItem.TradableId, 1L, product.ItemCount));
            }
        }
    }
}
