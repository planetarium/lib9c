namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Battle;
    using Lib9c.Helper;
    using Lib9c.Model;
    using Lib9c.Model.Item;
    using Lib9c.Model.Market;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData.Item;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Xunit;

    public class RegisterProductTest
    {
        private static readonly Address AvatarAddress = new ("47d082a115c63e7b58b1532d20e631538eafadde");

        private static readonly Currency Gold = Currency.Legacy("NCG", 2, null);

        private readonly Address _agentAddress;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private readonly GameConfigState _gameConfigState;
        private IWorld _initialState;

        public RegisterProductTest()
        {
            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);
            var rankingMapAddress = new PrivateKey().Address;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            _avatarState = AvatarState.Create(
                AvatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            _avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            agentState.avatarAddresses[0] = AvatarAddress;

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(Gold).Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<MaterialItemSheet>(), _tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(Addresses.GameConfig, _gameConfigState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(AvatarAddress, _avatarState)
                .SetActionPoint(AvatarAddress, DailyReward.ActionPointMax);
        }

        public static IEnumerable<object[]> Execute_Validate_MemberData()
        {
            yield return new object[]
            {
                new ValidateMember
                {
                    RegisterInfos = new List<IRegisterInfo>(),
                    Exc = typeof(ListEmptyException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                        },
                    },
                    Exc = typeof(InvalidAddressException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * Gold,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * CrystalCalculator.CRYSTAL,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = (10 * Gold).DivRem(3, out _),
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 0 * Gold,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * CrystalCalculator.CRYSTAL,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = (10 * Gold).DivRem(3, out _),
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Type = ProductType.FungibleAssetValue,
                            Price = 1 * Gold,
                            Asset = 0 * RuneHelper.StakeRune,
                        },
                    },
                    Exc = typeof(InvalidPriceException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.Fungible,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                        },
                    },
                    Exc = typeof(InvalidProductTypeException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.NonFungible,
                            ItemCount = 2,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.Fungible,
                            ItemCount = 0,
                        },
                    },
                    Exc = typeof(InvalidItemCountException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                            Asset = 1 * CrystalCalculator.CRYSTAL,
                        },
                    },
                    Exc = typeof(InvalidCurrencyException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                            Asset = 1 * Currencies.FreyaBlessingRune,
                        },
                    },
                    Exc = typeof(InvalidCurrencyException),
                },
                new ValidateMember
                {
                    RegisterInfos = new IRegisterInfo[]
                    {
                        new AssetInfo
                        {
                            AvatarAddress = AvatarAddress,
                            Price = 1 * Gold,
                            Type = ProductType.FungibleAssetValue,
                            Asset = 1 * Currencies.FreyaLiberationRune,
                        },
                    },
                    Exc = typeof(InvalidCurrencyException),
                },
            };
        }

        [Fact]
        public void Execute()
        {
            var materialRow = _tableSheets.MaterialItemSheet.Values.First();
            var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
            _avatarState.inventory.AddItem(tradableMaterial);
            var id = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, id, 0L);
            _avatarState.inventory.AddItem(equipment);
            Assert.Equal(2, _avatarState.inventory.Items.Count);
            var asset = 3 * RuneHelper.DailyRewardRune;
            var context = new ActionContext();
            _initialState = _initialState
                .SetAvatarState(AvatarAddress, _avatarState)
                .MintAsset(context, AvatarAddress, asset);
            var action = new RegisterProduct
            {
                AvatarAddress = AvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = 1,
                        Price = 1 * Gold,
                        TradableId = tradableMaterial.TradableId,
                        Type = ProductType.Fungible,
                    },
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = 1,
                        Price = 1 * Gold,
                        TradableId = equipment.ItemId,
                        Type = ProductType.NonFungible,
                    },
                    new AssetInfo
                    {
                        AvatarAddress = AvatarAddress,
                        Asset = asset,
                        Price = 1 * Gold,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            var nextState = action.Execute(
                new ActionContext
                {
                    BlockIndex = 1L,
                    PreviousState = _initialState,
                    RandomSeed = 0,
                    Signer = _agentAddress,
                });

            var nextAvatarState = nextState.GetAvatarState(AvatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
            Assert.Equal(DailyReward.ActionPointMax - RegisterProduct.CostAp, nextState.GetActionPoint(AvatarAddress));

            var marketState = new MarketState(nextState.GetLegacyState(Addresses.Market));
            Assert.Contains(AvatarAddress, marketState.AvatarAddresses);

            var productsState =
                new ProductsState((List)nextState.GetLegacyState(ProductsState.DeriveAddress(AvatarAddress)));
            var random = new TestRandom();
            for (var i = 0; i < 3; i++)
            {
                var guid = random.GenerateRandomGuid();
                Assert.Contains(guid, productsState.ProductIds);
                var productAddress = Product.DeriveAddress(guid);
                var product = ProductFactory.DeserializeProduct((List)nextState.GetLegacyState(productAddress));
                Assert.Equal(product.ProductId, guid);
                Assert.Equal(1 * Gold, product.Price);
                if (product is ItemProduct itemProduct)
                {
                    Assert.Equal(1, itemProduct.ItemCount);
                    Assert.NotNull(itemProduct.TradableItem);
                }

                if (product is FavProduct favProduct)
                {
                    Assert.Equal(asset, favProduct.Asset);
                }
            }

            Assert.Equal(0 * asset.Currency, nextState.GetBalance(AvatarAddress, asset.Currency));
        }

        [Theory]
        [MemberData(nameof(Execute_Validate_MemberData))]
        public void Execute_Validate_RegisterInfos(params ValidateMember[] validateMembers)
        {
            foreach (var validateMember in validateMembers)
            {
                foreach (var registerInfo in validateMember.RegisterInfos)
                {
                    var action = new RegisterProduct
                    {
                        AvatarAddress = AvatarAddress,
                        RegisterInfos = new[] { registerInfo, },
                    };
                    Assert.Throws(
                        validateMember.Exc,
                        () => action.Execute(
                            new ActionContext
                            {
                                PreviousState = _initialState,
                                RandomSeed = 0,
                                Signer = _agentAddress,
                            }));
                }
            }
        }

        [Theory]
        // not enough block index.
        [InlineData(ProductType.Fungible, 1, 2L, 1L, false)]
        [InlineData(ProductType.NonFungible, 1, 4L, 3L, false)]
        // not enough inventory items.
        [InlineData(ProductType.Fungible, 2, 3L, 3L, false)]
        // inventory has locked.
        [InlineData(ProductType.Fungible, 1, 3L, 3L, true)]
        [InlineData(ProductType.NonFungible, 1, 3L, 3L, true)]
        public void Execute_Throw_ItemDoesNotExistException(ProductType type, int itemCount, long requiredBlockIndex, long blockIndex, bool lockInventory)
        {
            ITradableItem tradableItem = null;
            switch (type)
            {
                case ProductType.Fungible:
                {
                    var materialRow = _tableSheets.MaterialItemSheet.Values.First();
                    var tradableMaterial = ItemFactory.CreateTradableMaterial(materialRow);
                    tradableMaterial.RequiredBlockIndex = requiredBlockIndex;
                    tradableItem = tradableMaterial;
                    break;
                }

                case ProductType.NonFungible:
                {
                    var equipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
                    var id = Guid.NewGuid();
                    tradableItem = (ITradableItem)ItemFactory.CreateItemUsable(equipmentRow, id, requiredBlockIndex);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (lockInventory)
            {
                _avatarState.inventory.AddItem((ItemBase)tradableItem, iLock: new OrderLock(Guid.NewGuid()));
            }
            else
            {
                _avatarState.inventory.AddItem((ItemBase)tradableItem);
            }

            _initialState = _initialState.SetAvatarState(AvatarAddress, _avatarState);
            var action = new RegisterProduct
            {
                AvatarAddress = AvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new RegisterInfo
                    {
                        AvatarAddress = AvatarAddress,
                        ItemCount = itemCount,
                        Price = 1 * Gold,
                        TradableId = tradableItem.TradableId,
                        Type = type,
                    },
                },
            };

            Assert.Throws<ItemDoesNotExistException>(
                () => action.Execute(
                    new ActionContext
                    {
                        Signer = _agentAddress,
                        BlockIndex = blockIndex,
                        RandomSeed = 0,
                        PreviousState = _initialState,
                    }));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var registerInfos = new List<RegisterInfo>();
            for (var i = 0; i < RegisterProduct.Capacity + 1; i++)
            {
                registerInfos.Add(new RegisterInfo());
            }

            var action = new RegisterProduct
            {
                AvatarAddress = _avatarState.address,
                RegisterInfos = registerInfos,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(new ActionContext()));
        }

        [Fact]
        public void Execute_Throw_DuplicateOrderIdException()
        {
            var asset = 3 * RuneHelper.DailyRewardRune;
            var context = new ActionContext();
            var random = new TestRandom();
            var productId = random.GenerateRandomGuid();
            _initialState = _initialState
                .SetAvatarState(AvatarAddress, _avatarState)
                .SetLegacyState(Product.DeriveAddress(productId), List.Empty)
                .MintAsset(context, AvatarAddress, asset);
            var action = new RegisterProduct
            {
                AvatarAddress = AvatarAddress,
                RegisterInfos = new List<IRegisterInfo>
                {
                    new AssetInfo
                    {
                        AvatarAddress = AvatarAddress,
                        Asset = asset,
                        Price = 1 * Gold,
                        Type = ProductType.FungibleAssetValue,
                    },
                },
            };
            Assert.Throws<DuplicateOrderIdException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = 1L,
                        PreviousState = _initialState,
                        RandomSeed = 0,
                        Signer = _agentAddress,
                    }));
        }

        public class ValidateMember
        {
            public IEnumerable<IRegisterInfo> RegisterInfos { get; set; }

            public Type Exc { get; set; }
        }
    }
}
