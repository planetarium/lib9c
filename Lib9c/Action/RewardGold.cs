using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
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
            Log.Debug("RewardGold Execute Beginning.");
            var states = context.PreviousStates;
            try
            {
                states = GenesisGoldDistribution(context, states);
                states = WeeklyArenaRankingBoard(context, states);
                states = MinerReward(context, states);
            }
            catch (Exception e)
            {
                Log.Debug($"RewardGold throw exception.\n{e.Message}");
                throw;
            }
            
            Log.Debug("RewardGold End.");
            return states;
        }

        public IAccountStateDelta GenesisGoldDistribution(IActionContext ctx, IAccountStateDelta states)
        {
            Log.Debug("RewardGold GenesisGoldDistribution() invoked.");
            
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;
            foreach(GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                if (amount <= 0) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
                FungibleAssetValue fav = goldCurrency * amount;
                var testAddresses = new HashSet<Address>(
                    new []
                    {
                        new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                        new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                    }
                );
                if (!testAddresses.Contains(distribution.Address))
                {
                    fav = fav.DivRem(100, out FungibleAssetValue _);
                }
                Log.Debug($"RewardGold TransferAsset(). sender({fund}), recipient({distribution.Address}), fav({fav.ToString()}), allowNegativeBalance(false)");
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
            Log.Debug("RewardGold WeeklyArenaRankingBoard() invoked.");
            
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                Log.Debug($"RewardGold SetState(). nextWeekly, address({nextWeekly.address}), index({nextIndex})");
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
                    Log.Debug($"RewardGold SetState(). prevWeekly, address({prevWeekly.address})");
                    states = states.SetState(prevWeekly.address, prevWeekly.Serialize());
                    Log.Debug($"RewardGold SetState(). weekly, address({weekly.address})");
                    states = states.SetState(weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                Log.Debug($"RewardGold SetState(). weekly, address({weekly.address})");
                states = states.SetState(weekly.address, weekly.Serialize());
            }

            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states)
        {
            Log.Debug("RewardGold MinerReward() invoked.");
            
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int)Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
            FungibleAssetValue miningReward =
                defaultMiningReward.DivRem(countOfHalfLife, out FungibleAssetValue _);

            if (miningReward >= FungibleAssetValue.Parse(currency, "1.25"))
            {
                Log.Debug($"RewardGold TransferAsset(). sender({GoldCurrencyState.Address}), recipient({ctx.Miner}), fav({miningReward.ToString()}), allowNegativeBalance(false)");
                states = states.TransferAsset(
                    GoldCurrencyState.Address,
                    ctx.Miner,
                    miningReward
                );
            }

            return states;
        }
    }
}
