namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class BuyTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly Address _shardedShopStateAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private IAccountStateDelta _initialState;

        public BuyTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            var currency = new Currency("NCG", 2, minters: null);
            _goldCurrencyState = new GoldCurrencyState(currency);

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var sellerAgentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var sellerAvatarState = new AvatarState(
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
            sellerAgentState.avatarAddresses[0] = _sellerAvatarAddress;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var buyerAgentState = new AgentState(_buyerAgentAddress);
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
            buyerAgentState.avatarAddresses[0] = _buyerAvatarAddress;

            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                100);

            var shardedShopStates = new Dictionary<Address, ShardedShopState>();

            var itemTypeKeys = new List<ItemSubType>()
            {
                ItemSubType.Weapon,
                ItemSubType.Armor,
                ItemSubType.Belt,
                ItemSubType.Necklace,
                ItemSubType.Ring,
                ItemSubType.Food,
                ItemSubType.FullCostume,
                ItemSubType.HairCostume,
                ItemSubType.EarCostume,
                ItemSubType.EyeCostume,
                ItemSubType.TailCostume,
                ItemSubType.Title,
            };

            foreach (var itemSubType in itemTypeKeys)
            {
                foreach (var addressKey in ShardedShopState.AddressKeys)
                {
                    Address address = ShardedShopState.DeriveAddress(itemSubType, addressKey);
                    shardedShopStates[address] = new ShardedShopState(address);
                    if (addressKey == "6" && itemSubType == ItemSubType.Weapon)
                    {
                        Guid productId = new Guid("6f460c1a-755d-48e4-ad67-65d5f519dbc8");
                        var shopItem = new ShopItem(
                            _sellerAgentAddress,
                            _sellerAvatarAddress,
                            productId,
                            new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                            100,
                            equipment);
                        shardedShopStates[address].Register(shopItem);
                        _shardedShopStateAddress = address;
                    }
                }
            }

            foreach (var (address, shardedShopState) in shardedShopStates)
            {
                _initialState = _initialState.SetState(address, shardedShopState.Serialize());
            }

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
                .MintAsset(_buyerAgentAddress, _goldCurrencyState.Currency * 100);
        }

        public static IEnumerable<object[]> GetExecuteMemberData()
        {
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell.ExpiredBlockIndex,
                    Price = 10,
                    ContainsInInventory = true,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 20,
                    ContainsInInventory = false,
                },
            };
            yield return new object[]
            {
                new ShopItemData()
                {
                    ItemType = ItemType.Costume,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = 0,
                    Price = 10,
                    ContainsInInventory = false,
                },
                new ShopItemData()
                {
                    ItemType = ItemType.Equipment,
                    ItemId = Guid.NewGuid(),
                    SellerAgentAddress = new PrivateKey().ToAddress(),
                    SellerAvatarAddress = new PrivateKey().ToAddress(),
                    RequiredBlockIndex = Sell.ExpiredBlockIndex,
                    Price = 50,
                    ContainsInInventory = true,
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(params ShopItemData[] shopItems)
        {
            AvatarState buyerAvatarState = _initialState.GetAvatarState(_buyerAvatarAddress);
            List<Buy.PurchaseInfo> purchaseInfos = new List<Buy.PurchaseInfo>();
            Dictionary<Address, ShardedShopState> shardedShopStates = new Dictionary<Address, ShardedShopState>();
            ShopState legacyShopState = _initialState.GetShopState();
            foreach (var shopItemData in shopItems)
            {
                var (sellerAvatarState, sellerAgentState) =
                    CreateAvatarState(shopItemData.SellerAgentAddress, shopItemData.SellerAvatarAddress);

                INonFungibleItem nonFungibleItem;
                ItemSubType itemSubType;
                Guid productId = shopItemData.ItemId;

                if (shopItemData.ItemType == ItemType.Equipment)
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        shopItemData.ItemId,
                        shopItemData.RequiredBlockIndex);
                    nonFungibleItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                }
                else
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, shopItemData.ItemId);
                    costume.Update(shopItemData.RequiredBlockIndex);
                    nonFungibleItem = costume;
                    itemSubType = costume.ItemSubType;
                }

                Address shardedShopAddress = ShardedShopState.DeriveAddress(itemSubType, productId);
                ShardedShopState shopState = shardedShopStates.ContainsKey(shardedShopAddress)
                    ? shardedShopStates[shardedShopAddress]
                    : new ShardedShopState(_initialState.GetState(shardedShopAddress));

                ShopItem shopItem = new ShopItem(
                    sellerAgentState.address,
                    sellerAvatarState.address,
                    productId,
                    shopItemData.Price * _goldCurrencyState.Currency,
                    shopItemData.RequiredBlockIndex,
                    nonFungibleItem);
                shopState.Register(shopItem);
                shardedShopStates[shardedShopAddress] = shopState;

                Assert.Single(shopState.Products);
                Assert.Equal(shopItemData.RequiredBlockIndex, nonFungibleItem.RequiredBlockIndex);

                // Case for backward compatibility of `Buy`
                if (shopItemData.ContainsInInventory)
                {
                    sellerAvatarState.inventory.AddItem((ItemBase)nonFungibleItem);
                }
                else
                {
                    legacyShopState.Register(shopItem);
                }

                Assert.Equal(
                    shopItemData.ContainsInInventory,
                    sellerAvatarState.inventory.TryGetNonFungibleItem(productId, out _)
                );

                var purchaseInfo = new Buy.PurchaseInfo(
                    shopItem.ProductId,
                    shopItem.SellerAgentAddress,
                    shopItem.SellerAvatarAddress,
                    itemSubType
                );
                purchaseInfos.Add(purchaseInfo);

                var result = new DailyReward.DailyRewardResult()
                {
                    id = default,
                    materials = new Dictionary<Material, int>(),
                };

                for (var i = 0; i < 100; i++)
                {
                    var mail = new DailyRewardMail(result, i, default, 0);
                    sellerAvatarState.Update(mail);
                    buyerAvatarState.Update(mail);
                }

                Assert.Equal(shopItemData.RequiredBlockIndex, nonFungibleItem.RequiredBlockIndex);
                Assert.Equal(
                    shopItemData.ContainsInInventory,
                    sellerAvatarState.inventory.TryGetNonFungibleItem(shopItemData.ItemId, out _)
                );
                _initialState = _initialState
                    .SetState(sellerAvatarState.address, sellerAvatarState.Serialize())
                    .SetState(_buyerAvatarAddress, buyerAvatarState.Serialize())
                    .SetState(Addresses.Shop, legacyShopState.Serialize())
                    .SetState(shardedShopAddress, shopState.Serialize());
            }

            Assert.Single(legacyShopState.Products);

            var buyAction = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = purchaseInfos,
            };
            var nextState = buyAction.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            FungibleAssetValue totalTax = 0 * _goldCurrencyState.Currency;
            FungibleAssetValue totalPrice = 0 * _goldCurrencyState.Currency;
            var goldCurrencyState = nextState.GetGoldCurrency();
            var nextBuyerAvatarState = nextState.GetAvatarState(_buyerAvatarAddress);
            Assert.Equal(2, nextBuyerAvatarState.mailBox.Count);

            foreach (var purchaseInfo in purchaseInfos)
            {
                Address shardedShopAddress =
                    ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                var prevShopState = new ShardedShopState(_initialState.GetState(shardedShopAddress));
                ShopItem shopItem = prevShopState.Products.Values.First(r => r.ProductId == purchaseInfo.productId);
                Guid itemId = shopItem.ItemUsable?.ItemId ?? shopItem.Costume.ItemId;
                var tax = shopItem.Price.DivRem(100, out _) * Buy.TaxRate;
                var taxedPrice = shopItem.Price - tax;
                totalTax += tax;
                totalPrice += shopItem.Price;

                var nextShopState = new ShardedShopState(nextState.GetState(shardedShopAddress));
                Assert.True(nextShopState.Products.Count < prevShopState.Products.Count);
                Assert.True(
                    nextBuyerAvatarState.inventory.TryGetNonFungibleItem(
                        itemId,
                        out INonFungibleItem outNonFungibleItem)
                );

                Assert.Equal(1, outNonFungibleItem.RequiredBlockIndex);

                var nextSellerAvatarState = nextState.GetAvatarState(purchaseInfo.sellerAvatarAddress);
                Assert.False(
                    nextSellerAvatarState.inventory.TryGetNonFungibleItem(
                        itemId,
                        out INonFungibleItem _)
                );
                Assert.Single(nextSellerAvatarState.mailBox);

                var sellerGold = nextState.GetBalance(purchaseInfo.sellerAgentAddress, goldCurrencyState);
                Assert.Equal(taxedPrice, sellerGold);
            }

            var goldCurrencyGold = nextState.GetBalance(Addresses.GoldCurrency, goldCurrencyState);
            Assert.Equal(totalTax, goldCurrencyGold);
            var buyerGold = nextState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            var prevBuyerGold = _initialState.GetBalance(_buyerAgentAddress, goldCurrencyState);
            Assert.Equal(prevBuyerGold - totalPrice, buyerGold);
            ShopState nextLegacyShopState = nextState.GetShopState();
            Assert.Empty(nextLegacyShopState.Products);
        }

        [Fact]
        public void Execute_Throw_InvalidAddressException()
        {
            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                default,
                _buyerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = new State(),
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                default,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = new State(),
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_buyerAvatarState)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };
            _initialState = _initialState.SetState(_buyerAvatarAddress, avatarState.Serialize());

            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                default,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Food
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
                {
                    BlockIndex = 0,
                    PreviousStates = _initialState,
                    Random = new TestRandom(),
                    Signer = _buyerAgentAddress,
                })
            );
        }

        [Fact]
        public void Execute_ErrorCode_ItemDoesNotExist()
        {
            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                default,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ERROR_CODE_ITEM_DOES_NOT_EXIST,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Execute_ErrorCode_InsufficientBalance()
        {
            ShardedShopState shopState = new ShardedShopState(_initialState.GetState(_shardedShopStateAddress));
            Assert.NotEmpty(shopState.Products);

            var (productId, shopItem) = shopState.Products.FirstOrDefault();
            Assert.NotNull(shopItem);

            var balance = _initialState.GetBalance(_buyerAgentAddress, _goldCurrencyState.Currency);
            _initialState = _initialState.BurnAsset(_buyerAgentAddress, balance);

            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ERROR_CODE_INSUFFICIENT_BALANCE,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Execute_ErrorCode_ItemDoesNotExist_By_SellerAvatar()
        {
            ShardedShopState shopState = new ShardedShopState(_initialState.GetState(_shardedShopStateAddress));
            Assert.NotNull(shopState.Products);
            var (productId, shopItem) = shopState.Products.First();
            Assert.True(shopItem.ExpiredBlockIndex > 0);
            Assert.True(shopItem.ItemUsable.RequiredBlockIndex > 0);

            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ERROR_CODE_ITEM_DOES_NOT_EXIST,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Execute_ErrorCode_ShopItemExpired()
        {
            IAccountStateDelta previousStates = _initialState;
            ShardedShopState shopState = new ShardedShopState(_initialState.GetState(_shardedShopStateAddress));
            Guid productId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            var itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            var shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                10,
                itemUsable);

            shopState.Register(shopItem);
            previousStates = previousStates.SetState(_shardedShopStateAddress, shopState.Serialize());

            Assert.True(shopState.Products.ContainsKey(productId));

            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 11,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Signer = _buyerAgentAddress,
            });

            Assert.Contains(
                Buy.ERROR_CODE_SHOPITEM_EXPIRED,
                action.buyerMultipleResult.purchaseResults.Select(r => r.errorCode)
            );
        }

        [Fact]
        public void Serialize_With_Dotnet_API()
        {
            ShardedShopState shopState = new ShardedShopState(_initialState.GetState(_shardedShopStateAddress));
            Guid productId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            ItemUsable itemUsable = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                10);
            ShopItem shopItem = new ShopItem(
                _sellerAgentAddress,
                _sellerAvatarAddress,
                productId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                10,
                itemUsable);
            AvatarState sellerAvatarState = _initialState.GetAvatarState(_sellerAvatarAddress);
            sellerAvatarState.inventory.AddItem(itemUsable);
            shopState.Register(shopItem);

            IAccountStateDelta prevState = _initialState
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_shardedShopStateAddress, shopState.Serialize());

            Buy.PurchaseInfo purchaseInfo = new Buy.PurchaseInfo(
                productId,
                _sellerAgentAddress,
                _sellerAvatarAddress,
                ItemSubType.Weapon
            );

            var action = new Buy
            {
                buyerAvatarAddress = _buyerAvatarAddress,
                purchaseInfos = new[] { purchaseInfo },
            };

            action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _buyerAgentAddress,
            });

            Assert.Empty(action.buyerMultipleResult.purchaseResults.Where(r => r.errorCode != 0));

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (Buy)formatter.Deserialize(ms);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }

        private (AvatarState avatarState, AgentState agentState) CreateAvatarState(
            Address agentAddress, Address avatarAddress)
        {
            var agentState = new AgentState(agentAddress);
            var rankingMapAddress = new PrivateKey().ToAddress();

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
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
            agentState.avatarAddresses[0] = avatarAddress;

            _initialState = _initialState
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
            return (avatarState, agentState);
        }

        private struct PriceData
        {
            public FungibleAssetValue TaxSum;
            public Dictionary<Address, FungibleAssetValue> TaxedPriceSum;
            public FungibleAssetValue PriceSum;

            public PriceData(Currency currency)
            {
                TaxSum = new FungibleAssetValue(currency, 0, 0);
                TaxedPriceSum = new Dictionary<Address, FungibleAssetValue>();
                PriceSum = new FungibleAssetValue(currency, 0, 0);
            }
        }

        public class ShopItemData
        {
            public ItemType ItemType { get; set; }

            public Guid ItemId { get; set; }

            public Address SellerAgentAddress { get; set; }

            public Address SellerAvatarAddress { get; set; }

            public BigInteger Price { get; set; }

            public long RequiredBlockIndex { get; set; }

            public bool ContainsInInventory { get; set; }
        }
    }
}
