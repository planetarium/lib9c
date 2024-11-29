using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_product3")]
    public class RegisterProduct : GameAction
    {
        public static readonly IReadOnlyCollection<Currency> NonTradableTickerCurrencies = new List<Currency>
        {
            Currencies.FreyaBlessingRune,
            Currencies.FreyaLiberationRune,
            Currencies.Crystal,
            Currencies.OdinWeaknessRune,
            Currencies.OdinWisdomRune,
        };

        public const int CostAp = 5;
        public const int Capacity = 100;
        public Address AvatarAddress;
        public IEnumerable<IRegisterInfo> RegisterInfos;
        public bool ChargeAp;

        public override IWorld Execute(IActionContext context)
        {
            var sw = new Stopwatch();

            GasTracer.UseGas(1);
            var states = context.PreviousState;

            sw.Start();
            if (!RegisterInfos.Any())
            {
                throw new ListEmptyException("RegisterInfos was empty");
            }

            if (RegisterInfos.Count() > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(RegisterInfos)} must be less than or equal {Capacity}.");
            }

            var ncg = states.GetGoldCurrency();
            foreach (var registerInfo in RegisterInfos)
            {
                registerInfo.ValidateAddress(AvatarAddress);
                registerInfo.ValidatePrice(ncg);
                registerInfo.Validate();
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException("failed to load avatar state.");
            }

            sw.Stop();
            Log.Debug("{Source} {Process} from #{BlockIndex}: {Elapsed}",
                nameof(RegisterProduct), "Get States And Validate", context.BlockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            if (!states.TryGetActionPoint(AvatarAddress, out var actionPoint))
            {
                actionPoint = avatarState.actionPoint;
            }

            var resultActionPoint = avatarState.inventory.UseActionPoint(
                actionPoint,
                CostAp,
                ChargeAp,
                states.GetSheet<MaterialItemSheet>(),
                context.BlockIndex);
            sw.Stop();
            Log.Debug("{Source} {Process} from #{BlockIndex}: {Elapsed}",
                nameof(RegisterProduct), "UseAp", context.BlockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var productsStateAddress = ProductsState.DeriveAddress(AvatarAddress);
            ProductsState productsState;
            if (states.TryGetLegacyState(productsStateAddress, out List rawProducts))
            {
                productsState = new ProductsState(rawProducts);
            }
            else
            {
                productsState = new ProductsState();
                var marketState = states.TryGetLegacyState(Addresses.Market, out List rawMarketList)
                    ? rawMarketList
                    : List.Empty;
                marketState = marketState.Add(AvatarAddress.Serialize());
                states = states.SetLegacyState(Addresses.Market, marketState);
            }
            sw.Stop();
            Log.Debug("{Source} {Process} from #{BlockIndex}: {Elapsed}",
                nameof(RegisterProduct), "Get productsState", context.BlockIndex, sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var random = context.GetRandom();
            foreach (var info in RegisterInfos.OrderBy(r => r.Type).ThenBy(r => r.Price))
            {
                states = Register(context, info, avatarState, productsState, states, random);
            }
            sw.Stop();
            Log.Debug("{Source} {Process} from #{BlockIndex}: {Elapsed}, ProductCount: {ProductCount}",
                nameof(RegisterProduct), "Register Infos", context.BlockIndex, sw.Elapsed.TotalMilliseconds, RegisterInfos.Count());

            sw.Restart();
            states = states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetActionPoint(AvatarAddress, resultActionPoint)
                .SetLegacyState(productsStateAddress, productsState.Serialize());

            sw.Stop();
            Log.Debug("{Source} {Process} from #{BlockIndex}: {Elapsed}",
                nameof(RegisterProduct), "Set States", context.BlockIndex, sw.Elapsed.TotalMilliseconds);

            return states;
        }

        public static IWorld Register(IActionContext context, IRegisterInfo info, AvatarState avatarState,
            ProductsState productsState, IWorld states, IRandom random)
        {
            switch (info)
            {
                case RegisterInfo registerInfo:
                    switch (info.Type)
                    {
                        case ProductType.Fungible:
                        case ProductType.NonFungible:
                        {
                            var tradableId = registerInfo.TradableId;
                            var itemCount = registerInfo.ItemCount;
                            var type = registerInfo.Type;
                            ITradableItem tradableItem = null;
                            switch (type)
                            {
                                case ProductType.Fungible:
                                {
                                    if (avatarState.inventory.TryGetTradableItems(tradableId,
                                            context.BlockIndex, itemCount, out var items))
                                    {
                                        int totalCount = itemCount;
                                        tradableItem = (ITradableItem) items.First().item;
                                        foreach (var inventoryItem in items)
                                        {
                                            int removeCount = Math.Min(totalCount,
                                                inventoryItem.count);
                                            ITradableFungibleItem tradableFungibleItem =
                                                (ITradableFungibleItem) inventoryItem.item;
                                            if (!avatarState.inventory.RemoveTradableItem(
                                                    tradableId,
                                                    tradableFungibleItem.RequiredBlockIndex,
                                                    removeCount))
                                            {
                                                throw new ItemDoesNotExistException(
                                                    $"failed to remove tradable material {tradableId}/{itemCount}");
                                            }

                                            totalCount -= removeCount;
                                            if (totalCount < 1)
                                            {
                                                break;
                                            }
                                        }

                                        if (totalCount != 0)
                                        {
                                            throw new InvalidItemCountException();
                                        }
                                    }

                                    break;
                                }
                                case ProductType.NonFungible:
                                {
                                    if (avatarState.inventory.TryGetNonFungibleItem(tradableId,
                                            out var item) &&
                                        avatarState.inventory.RemoveNonFungibleItem(tradableId))
                                    {
                                        tradableItem = item.item as ITradableItem;
                                    }

                                    break;
                                }
                            }

                            if (tradableItem is null || tradableItem.RequiredBlockIndex > context.BlockIndex)
                            {
                                throw new ItemDoesNotExistException($"can't find item: {tradableId}");
                            }

                            Guid productId = random.GenerateRandomGuid();
                            var productAddress = Product.DeriveAddress(productId);
                            // 중복된 ProductId가 발급되면 상태를 덮어씌우는 현상을 방지하기위해 예외발생
                            if (states.TryGetLegacyState(productAddress, out IValue v) && v is not Null)
                            {
                                // FIXME 클라이언트 배포를 회피하기위해 기존 오류를 사용했으나 정규배포때 별도 예외로 구분ㅐ
                                throw new DuplicateOrderIdException("already registered id.");
                            }
                            var product = new ItemProduct
                            {
                                ProductId = productId,
                                Price = registerInfo.Price,
                                TradableItem = tradableItem,
                                ItemCount = itemCount,
                                RegisteredBlockIndex = context.BlockIndex,
                                Type = registerInfo.Type,
                                SellerAgentAddress = context.Signer,
                                SellerAvatarAddress = registerInfo.AvatarAddress,
                            };
                            productsState.ProductIds.Add(productId);
                            states = states.SetLegacyState(productAddress, product.Serialize());
                            break;
                        }
                    }

                    break;
                case AssetInfo assetInfo:
                {
                    Guid productId = random.GenerateRandomGuid();
                    Address productAddress = Product.DeriveAddress(productId);
                    // 중복된 ProductId가 발급되면 상태를 덮어씌우는 현상을 방지하기위해 예외발생
                    if (states.TryGetLegacyState(productAddress, out IValue v) && v is not Null)
                    {
                        // FIXME 클라이언트 배포를 회피하기위해 기존 오류를 사용했으나 정규배포때 별도 예외로 구분ㅐ
                        throw new DuplicateOrderIdException("already registered id.");
                    }

                    FungibleAssetValue asset = assetInfo.Asset;
                    var product = new FavProduct
                    {
                        ProductId = productId,
                        Price = assetInfo.Price,
                        Asset = asset,
                        RegisteredBlockIndex = context.BlockIndex,
                        Type = assetInfo.Type,
                        SellerAgentAddress = context.Signer,
                        SellerAvatarAddress = assetInfo.AvatarAddress,
                    };
                    states = states
                        .TransferAsset(context, avatarState.address, productAddress, asset)
                        .SetLegacyState(productAddress, product.Serialize());
                    productsState.ProductIds.Add(productId);
                    break;
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RegisterInfos.Select(r => r.Serialize())),
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = ChargeAp.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = (List) plainValue["r"];
            RegisterInfos = serialized.Cast<List>()
                .Select(ProductFactory.DeserializeRegisterInfo).ToList();
            AvatarAddress = plainValue["a"].ToAddress();
            ChargeAp = plainValue["c"].ToBoolean();
        }
    }
}
