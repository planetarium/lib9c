using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;

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
            Currency goldCurrency = states.GetGoldCurrency();
            states = GenesisGoldDistribution(context, states, goldCurrency);
            states = WeeklyArenaRankingBoard(context, states);
            return MinerReward(context, states, goldCurrency);
        }

        public IAccountStateDelta GenesisGoldDistribution(IActionContext ctx, IAccountStateDelta states, Currency goldCurrency)
        {
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Address fund = GoldCurrencyState.Address;
            var testAddresses = new HashSet<Address>(
                new []
                {
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                }
            );

            foreach(GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                if (amount <= 0) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
                FungibleAssetValue fav = goldCurrency * amount;
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
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary) states.GetState(weeklyAddress);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }
            var resetIndex = rawWeekly["resetIndex"].ToLong();

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeeklyAddress = WeeklyArenaState.DeriveAddress(index - 1);
                var rawPrevWeekly = (Dictionary)states.GetState(prevWeeklyAddress);
                if (!rawPrevWeekly["ended"].ToBoolean())
                {
                    rawPrevWeekly = rawPrevWeekly.SetItem("ended", true.Serialize());
                    var weekly = new WeeklyArenaState(rawWeekly);
                    var prevWeekly = new WeeklyArenaState(rawPrevWeekly);
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    states = states.SetState(prevWeeklyAddress, rawPrevWeekly);
                    states = states.SetState(weeklyAddress, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - resetIndex >= gameConfigState.DailyArenaInterval)
            {
                var weekly = new WeeklyArenaState(rawWeekly);
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weeklyAddress, weekly.Serialize());
            }
            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states, Currency currency)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int)Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
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
    }
}
