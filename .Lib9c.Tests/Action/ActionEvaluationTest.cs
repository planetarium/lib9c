namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Bencodex.Types;
    using Lib9c.Formatters;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Collection;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ActionEvaluationTest
    {
        private readonly Currency _currency;
        private readonly Address _signer;
        private readonly Address _sender;
        private readonly IWorld _states;

        public ActionEvaluationTest()
        {
            var context = new ActionContext();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _signer = new PrivateKey().Address;
            _sender = new PrivateKey().Address;
            _states = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(_signer, (Text)"ANYTHING")
                .SetLegacyState(default, Dictionary.Empty.Add("key", "value"))
                .MintAsset(context, _signer, _currency * 10000);
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        [Theory]
        [InlineData(typeof(TransferAsset))]
        [InlineData(typeof(CreateAvatar))]
        [InlineData(typeof(HackAndSlash))]
        [InlineData(typeof(AddRedeemCode))]
        [InlineData(typeof(Buy))]
        [InlineData(typeof(ChargeActionPoint))]
        [InlineData(typeof(CombinationConsumable))]
        [InlineData(typeof(CombinationEquipment))]
        [InlineData(typeof(CreatePendingActivation))]
        [InlineData(typeof(DailyReward))]
        [InlineData(typeof(InitializeStates))]
        [InlineData(typeof(ItemEnhancement))]
        [InlineData(typeof(MigrationActivatedAccountsState))]
        [InlineData(typeof(PatchTableSheet))]
        [InlineData(typeof(RankingBattle))]
        [InlineData(typeof(RapidCombination))]
        [InlineData(typeof(RedeemCode))]
        [InlineData(typeof(RewardGold))]
        [InlineData(typeof(Sell))]
        [InlineData(typeof(SellCancellation))]
        [InlineData(typeof(UpdateSell))]
        [InlineData(typeof(CreatePendingActivations))]
        [InlineData(typeof(Grinding))]
        [InlineData(typeof(UnlockEquipmentRecipe))]
        [InlineData(typeof(UnlockWorld))]
        [InlineData(typeof(EventDungeonBattle))]
        [InlineData(typeof(EventConsumableItemCrafts))]
        [InlineData(typeof(Raid))]
        [InlineData(typeof(ClaimRaidReward))]
        [InlineData(typeof(ClaimWordBossKillReward))]
        [InlineData(typeof(PrepareRewardAssets))]
        [InlineData(typeof(RegisterProduct))]
        [InlineData(typeof(ReRegisterProduct))]
        [InlineData(typeof(CancelProductRegistration))]
        [InlineData(typeof(BuyProduct))]
        [InlineData(typeof(RequestPledge))]
        [InlineData(typeof(ApprovePledge))]
        [InlineData(typeof(EndPledge))]
        [InlineData(typeof(CreatePledge))]
        [InlineData(typeof(TransferAssets))]
        [InlineData(typeof(RuneSummon))]
        [InlineData(typeof(ActivateCollection))]
        [InlineData(typeof(RetrieveAvatarAssets))]
        [InlineData(typeof(MigrateFee))]
        public void Serialize_With_MessagePack(Type actionType)
        {
            var action = GetAction(actionType);
            var ncEval = new NCActionEvaluation(
                action,
                _signer,
                1234,
                _states.Trie.Hash,
                null,
                _states.Trie.Hash,
                0,
                new Dictionary<string, IValue>(),
                null
            );
            var evaluation = ncEval.ToActionEvaluation();
            var b = MessagePackSerializer.Serialize(ncEval);
            var deserialized = MessagePackSerializer.Deserialize<NCActionEvaluation>(b);
            Assert.Equal(evaluation.Signer, deserialized.Signer);
            Assert.Equal(evaluation.BlockIndex, deserialized.BlockIndex);
            Assert.Equal(_states.Trie.Hash, deserialized.OutputState);
            Assert.Equal(_states.Trie.Hash, deserialized.PreviousState);
            if (actionType == typeof(RewardGold))
            {
                Assert.Null(deserialized.Action);
            }
            else
            {
                Assert.NotNull(deserialized.Action);
                Assert.IsType(actionType, deserialized.Action);
            }

            if (action is GameAction gameAction)
            {
                Assert.Equal(gameAction.Id, ((GameAction)deserialized.Action).Id);
            }
        }

        private ActionBase GetAction(Type type)
        {
            var action = Activator.CreateInstance(type);
            return action switch
            {
                TransferAsset _ => new TransferAsset(_sender, _signer, _currency * 100),
                CreateAvatar _ => new CreateAvatar
                {
                    ear = 0,
                    hair = 0,
                    index = 0,
                    lens = 0,
                    name = "name",
                    tail = 0,
                },
                HackAndSlash _ => new HackAndSlash
                {
                    Costumes = new List<Guid>(),
                    Equipments = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    WorldId = 0,
                    StageId = 0,
                    AvatarAddress = new PrivateKey().Address,
                },
                AddRedeemCode _ => new AddRedeemCode
                {
                    redeemCsv = "csv",
                },
                Buy _ => new Buy
                {
                    buyerAvatarAddress = new PrivateKey().Address,
                    purchaseInfos = new[]
                    {
                        new PurchaseInfo(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            _signer,
                            new PrivateKey().Address,
                            ItemSubType.Armor,
#pragma warning disable CS0618
                            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                            Currency.Legacy("NCG", 2, null) * 10
#pragma warning restore CS0618
                        ),
                    },
                },
                ChargeActionPoint _ => new ChargeActionPoint(),
                CombinationConsumable _ => new CombinationConsumable(),
                CombinationEquipment _ => new CombinationEquipment(),
                CreatePendingActivation _ => new CreatePendingActivation(
                    new PendingActivationState(new byte[] { 0x0, }, new PrivateKey().PublicKey)
                ),
                DailyReward _ => new DailyReward(),
                InitializeStates _ => new InitializeStates
                {
                    TableSheets = new Dictionary<string, string>(),
                    AuthorizedMiners = Dictionary.Empty,
                    Credits = Dictionary.Empty,
                },
                ItemEnhancement _ => new ItemEnhancement
                {
                    materialIds = new List<Guid>(),
                },
                MigrationActivatedAccountsState _ => new MigrationActivatedAccountsState(),
                PatchTableSheet _ => new PatchTableSheet
                {
                    TableCsv = "table",
                    TableName = "name",
                },
                RankingBattle _ => new RankingBattle
                {
                    avatarAddress = new PrivateKey().Address,
                    enemyAddress = new PrivateKey().Address,
                    weeklyArenaAddress = new PrivateKey().Address,
                    costumeIds = new List<Guid>(),
                    equipmentIds = new List<Guid>(),
                },
                RapidCombination _ => new RapidCombination(),
                RedeemCode _ => new RedeemCode
                {
                    Code = "code",
                    AvatarAddress = new PrivateKey().Address,
                },
                RewardGold _ => null,
                Sell _ => new Sell
                {
                    price = _currency * 100,
                },
                SellCancellation _ => new SellCancellation(),
                UpdateSell _ => new UpdateSell
                {
                    sellerAvatarAddress = new PrivateKey().Address,
                    updateSellInfos = new[]
                    {
                        new UpdateSellInfo(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            ItemSubType.Armor,
                            _currency * 100,
                            1
                        ),
                    },
                },
                CreatePendingActivations _ => new CreatePendingActivations
                {
                    PendingActivations = new[]
                    {
                        (new byte[40], new byte[4], new byte[33]),
                    },
                },
                Grinding _ => new Grinding
                {
                    AvatarAddress = new PrivateKey().Address,
                    EquipmentIds = new List<Guid>(),
                },
                UnlockEquipmentRecipe _ => new UnlockEquipmentRecipe
                {
                    AvatarAddress = new PrivateKey().Address,
                    RecipeIds = new List<int>
                    {
                        2,
                        3,
                    },
                },
                UnlockWorld _ => new UnlockWorld
                {
                    AvatarAddress = new PrivateKey().Address,
                    WorldIds = new List<int>()
                    {
                        2,
                        3,
                    },
                },
                EventDungeonBattle _ => new EventDungeonBattle
                {
                    AvatarAddress = default,
                    EventScheduleId = 0,
                    EventDungeonId = 0,
                    EventDungeonStageId = 0,
                    Equipments = new List<Guid>(),
                    Costumes = new List<Guid>(),
                    Foods = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                },
                EventConsumableItemCrafts _ => new EventConsumableItemCrafts
                {
                    AvatarAddress = default,
                    EventScheduleId = 0,
                    EventConsumableItemRecipeId = 0,
                    SlotIndex = 0,
                },
                Raid _ => new Raid
                {
                    AvatarAddress = new PrivateKey().Address,
                    CostumeIds = new List<Guid>(),
                    EquipmentIds = new List<Guid>(),
                    FoodIds = new List<Guid>(),
                    RuneInfos = new List<RuneSlotInfo>(),
                    PayNcg = true,
                },
                ClaimRaidReward _ => new ClaimRaidReward(_sender),
                ClaimWordBossKillReward _ => new ClaimWordBossKillReward
                {
                    AvatarAddress = _sender,
                },
                PrepareRewardAssets _ => new PrepareRewardAssets
                {
                    RewardPoolAddress = _sender,
                    Assets = new List<FungibleAssetValue>
                    {
                        _currency * 100,
                    },
                },
                RegisterProduct _ => new RegisterProduct
                {
                    RegisterInfos = new List<IRegisterInfo>
                    {
                        new RegisterInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                            ItemCount = 1,
                            Price = 1 * _currency,
                            TradableId = Guid.NewGuid(),
                            Type = ProductType.Fungible,
                        },
                        new AssetInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                            Price = 1 * _currency,
                            Asset = 1 * RuneHelper.StakeRune,
                            Type = ProductType.FungibleAssetValue,
                        },
                    },
                    AvatarAddress = _sender,
                    ChargeAp = true,
                },
                ReRegisterProduct _ => new ReRegisterProduct
                {
                    AvatarAddress = new PrivateKey().Address,
                    ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                    {
                        (
                            new ItemProductInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                AgentAddress = new PrivateKey().Address,
                                Price = 1 * _currency,
                                ProductId = Guid.NewGuid(),
                                Type = ProductType.Fungible,
                            },
                            new RegisterInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                ItemCount = 1,
                                Price = 1 * _currency,
                                TradableId = Guid.NewGuid(),
                                Type = ProductType.Fungible,
                            }
                        ),
                        (
                            new ItemProductInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                AgentAddress = new PrivateKey().Address,
                                Price = 1 * _currency,
                                ProductId = Guid.NewGuid(),
                                Type = ProductType.NonFungible,
                            },
                            new RegisterInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                ItemCount = 1,
                                Price = 1 * _currency,
                                TradableId = Guid.NewGuid(),
                                Type = ProductType.NonFungible,
                            }
                        ),
                        (
                            new FavProductInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                AgentAddress = new PrivateKey().Address,
                                Price = 1 * _currency,
                                ProductId = Guid.NewGuid(),
                                Type = ProductType.FungibleAssetValue,
                            },
                            new AssetInfo
                            {
                                AvatarAddress = new PrivateKey().Address,
                                Price = 1 * _currency,
                                Asset = 1 * RuneHelper.StakeRune,
                                Type = ProductType.FungibleAssetValue,
                            }
                        ),
                    },
                    ChargeAp = true,
                },
                CancelProductRegistration _ => new CancelProductRegistration
                {
                    AvatarAddress = new PrivateKey().Address,
                    ProductInfos = new List<IProductInfo>
                    {
                        new FavProductInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                            AgentAddress = new PrivateKey().Address,
                            Price = 1 * _currency,
                            ProductId = Guid.NewGuid(),
                            Type = ProductType.FungibleAssetValue,
                        },
                    },
                    ChargeAp = true,
                },
                BuyProduct _ => new BuyProduct
                {
                    AvatarAddress = new PrivateKey().Address,
                    ProductInfos = new List<IProductInfo>
                    {
                        new ItemProductInfo
                        {
                            AvatarAddress = new PrivateKey().Address,
                            AgentAddress = new PrivateKey().Address,
                            Price = 1 * _currency,
                            ProductId = Guid.NewGuid(),
                            Type = ProductType.Fungible,
                            ItemSubType = ItemSubType.Armor,
                            TradableId = Guid.NewGuid(),
                        },
                    },
                },
                RequestPledge _ => new RequestPledge
                {
                    AgentAddress = new PrivateKey().Address,
                },
                ApprovePledge _ => new ApprovePledge
                {
                    PatronAddress = new PrivateKey().Address,
                },
                EndPledge _ => new EndPledge
                {
                    AgentAddress = new PrivateKey().Address,
                },
                CreatePledge _ => new CreatePledge
                {
                    PatronAddress = new PrivateKey().Address,
                    AgentAddresses = new[] { (new PrivateKey().Address, new PrivateKey().Address), },
                    Mead = 4,
                },
                TransferAssets _ => new TransferAssets(
                    _sender,
                    new List<(Address, FungibleAssetValue)>
                    {
                        (_signer, 1 * _currency),
                    }),
                RuneSummon _ => new RuneSummon
                {
                    AvatarAddress = _sender,
                    GroupId = 20001,
                    SummonCount = 10,
                },
                ActivateCollection _ => new ActivateCollection
                {
                    AvatarAddress = _sender,
                    CollectionData =
                    {
                        (
                            1,
                            new List<ICollectionMaterial>
                            {
                                new FungibleCollectionMaterial
                                {
                                    ItemId = 1,
                                    ItemCount = 2,
                                },
                                new NonFungibleCollectionMaterial
                                {
                                    ItemId = 2,
                                    ItemCount = 3,
                                    NonFungibleId = Guid.NewGuid(),
                                    Level = 1,
                                    SkillContains = true,
                                },
                            }
                        ),
                    },
                },
                RetrieveAvatarAssets _ => new RetrieveAvatarAssets(new PrivateKey().Address),
                MigrateFee _ => new MigrateFee
                {
                    TransferData = new List<(Address sender, Address recipient, BigInteger amount)>
                    {
                        (new PrivateKey().Address, new PrivateKey().Address, 1),
                        (new PrivateKey().Address, new PrivateKey().Address, 2),
                    },
                },
                _ => throw new InvalidCastException(),
            };
        }
    }
}
