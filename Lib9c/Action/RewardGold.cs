using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    public class RewardGold : ActionBase
    {
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            states = GenesisGoldDistribution(context, states);
            states = WeeklyArenaRankingBoard(context, states);
            states = ShardShopStateByCategoryWithGuId(context, states);
            return MinerReward(context, states);
        }

        public IAccountStateDelta GenesisGoldDistribution(IActionContext ctx, IAccountStateDelta states)
        {
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;
            foreach (GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                if (amount <= 0) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
                FungibleAssetValue fav = goldCurrency * amount;
                var testAddresses = new HashSet<Address>(
                    new[]
                    {
                        new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                        new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                    }
                );
                if (!testAddresses.Contains(distribution.Address))
                {
                    fav = fav.DivRem(100, out FungibleAssetValue _);
                }

                states = states.TransferAsset(
                    fund,
                    distribution.Address,
                    fav
                );
            }

            return states;
        }

        public IAccountStateDelta WeeklyArenaRankingBoard(IActionContext ctx, IAccountStateDelta states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = states.GetWeeklyArenaState(index - 1);
                if (!prevWeekly.Ended)
                {
                    prevWeekly.End();
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    states = states.SetState(prevWeekly.address, prevWeekly.Serialize());
                    states = states.SetState(weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weekly.address, weekly.Serialize());
            }

            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int) Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
            FungibleAssetValue miningReward =
                defaultMiningReward.DivRem(countOfHalfLife, out FungibleAssetValue _);

            if (miningReward >= FungibleAssetValue.Parse(currency, "1.25"))
            {
                states = states.TransferAsset(
                    GoldCurrencyState.Address,
                    ctx.Miner,
                    miningReward
                );
            }

            return states;
        }

        public IAccountStateDelta ShardShopStateByCategoryWithGuId(IActionContext ctx, IAccountStateDelta states)
        {
            // Change BlockIndex on main net.
            if (ctx.BlockIndex == 1039424)
            {
                Log.Information("Start {Method} on BlockIndex: #{BlockIndex}",
                    nameof(ShardShopStateByCategoryWithGuId), ctx.BlockIndex);
                ShopState shopState = states.GetShopState();
                IReadOnlyDictionary<Guid, ShopItem> products = shopState.Products;
                int count = products.Count;
                var shardedShopStates = new Dictionary<Address, ShardedShopState>();
                var addressKeys = new List<string>
                {
                    "0",
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "a",
                    "b",
                    "c",
                    "d",
                    "e",
                    "f",
                };
                int shardedCount = 0;
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

                Log.Information("Initialize ShardedShopStates");
                foreach (var itemSubType in itemTypeKeys)
                {
                    foreach (var addressKey in addressKeys)
                    {
                        Address address = ShardedShopState.DeriveAddress(itemSubType, addressKey);
                        shardedShopStates[address] = new ShardedShopState(address);
                    }
                }
                Log.Information("Initialize Finish");

                Log.Information("Start Shard ShopState.Products");
                foreach (var kv in products)
                {
                    ItemSubType itemSubType =  kv.Value.ItemUsable?.ItemSubType ?? kv.Value.Costume.ItemSubType;
                    Address address = ShardedShopState.DeriveAddress(itemSubType, kv.Key);
                    if (shardedShopStates.ContainsKey(address))
                    {
                        ShardedShopState state = shardedShopStates[address];
                        state.Register(kv.Value);
                    }
                    else
                    {
                        var state = new ShardedShopState(address);
                        state.Register(kv.Value);
                        shardedShopStates[address] = state;
                    }
                }
                Log.Information("Shard ShopState.Products Finish");

                Log.Information("Start Set ShardedShopStates");
#pragma warning disable LAA1002
                foreach (var kv in shardedShopStates)
                {
                    var state = kv.Value;
                    shardedCount += state.Products.Count;
                    states = states.SetState(kv.Key, state.Serialize());
                }
#pragma warning restore LAA1002
                Log.Information("Set ShopStates Finish. Shop.Products: {ShopCount} ShardedCount: {ShardedCount}",
                    count, shardedCount);
                Log.Information("End {Method}", nameof(ShardShopStateByCategoryWithGuId));
            }
            return states;
        }
    }
}
