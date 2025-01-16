using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at many pull requests
    /// Updated at https://github.com/planetarium/lib9c/pull/1135
    /// </summary>
    [Serializable]
    public class RewardGold : ActionBase
    {
        // Start filtering inactivate ArenaInfo
        // https://github.com/planetarium/lib9c/issues/946
        public const long FilterInactiveArenaInfoBlockIndex = 3_976_000L;
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var states = context.PreviousState;
            states = TransferMead(context, states);
            states = GenesisGoldDistribution(context, states);
            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold exec started", addressesHex);

            // Avoid InvalidBlockStateRootHashException before table patch.
            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            // Avoid InvalidBlockStateRootHashException in unit test genesis block evaluate.
            if (states.GetLegacyState(arenaSheetAddress) is null || context.BlockIndex == 0)
            {
                states = WeeklyArenaRankingBoard2(context, states);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return MinerReward(context, states);
        }

        public IWorld GenesisGoldDistribution(IActionContext ctx, IWorld states)
        {
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;
            FungibleAssetValue fundValue = states.GetBalance(fund, goldCurrency);
            foreach(GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                FungibleAssetValue fav = goldCurrency * amount;
                if (amount <= 0 || fundValue < fav) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
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
                states = states.TransferAsset(
                    ctx,
                    fund,
                    distribution.Address,
                    fav
                );
            }
            return states;
        }

        [Obsolete("Use WeeklyArenaRankingBoard2 for performance.")]
        public IWorld WeeklyArenaRankingBoard(IActionContext ctx, IWorld states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetLegacyState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = states.GetWeeklyArenaState(index - 1);
                if (!prevWeekly.Ended)
                {
                    prevWeekly.End();
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    states = states.SetLegacyState(prevWeekly.address, prevWeekly.Serialize());
                    states = states.SetLegacyState(weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetLegacyState(weekly.address, weekly.Serialize());
            }
            return states;
        }

        public IWorld WeeklyArenaRankingBoard2(IActionContext ctx, IWorld states)
        {
            states = PrepareNextArena(ctx, states);
            states = ResetChallengeCount(ctx, states);
            return states;
        }

        public IWorld PrepareNextArena(IActionContext ctx, IWorld states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary) states.GetLegacyState(weeklyAddress);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetLegacyState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeeklyAddress = WeeklyArenaState.DeriveAddress(index - 1);
                var rawPrevWeekly = (Dictionary) states.GetLegacyState(prevWeeklyAddress);
                if (!rawPrevWeekly["ended"].ToBoolean())
                {
                    rawPrevWeekly = rawPrevWeekly.SetItem("ended", true.Serialize());
                    var weekly = new WeeklyArenaState(rawWeekly);
                    var prevWeekly = new WeeklyArenaState(rawPrevWeekly);
                    var listAddress = weekly.address.Derive("address_list");
                    // Set ArenaInfo, address list for new RankingBattle.
                    var addressList = states.TryGetLegacyState(listAddress, out List rawList)
                        ? rawList.ToList(StateExtensions.ToAddress)
                        : new List<Address>();
                    var nextAddresses = rawList ?? List.Empty;
                    if (ctx.BlockIndex >= RankingBattle11.UpdateTargetBlockIndex)
                    {
                        weekly.ResetIndex = ctx.BlockIndex;

                        // Copy Map to address list.
                        if (ctx.BlockIndex == RankingBattle11.UpdateTargetBlockIndex)
                        {
                            foreach (var kv in prevWeekly.Map)
                            {
                                var address = kv.Key;
                                var lazyInfo = kv.Value;
                                var info = new ArenaInfo(lazyInfo.State);
                                states = states.SetLegacyState(
                                    weeklyAddress.Derive(address.ToByteArray()), info.Serialize());
                                if (!addressList.Contains(address))
                                {
                                    nextAddresses = nextAddresses.Add(address.Serialize());
                                }
                            }
                        }
                        else
                        {
                            bool filterInactive =
                                ctx.BlockIndex >= FilterInactiveArenaInfoBlockIndex;
                            // Copy addresses from prev weekly address list.
                            var prevListAddress = prevWeekly.address.Derive("address_list");

                            if (states.TryGetLegacyState(prevListAddress, out List prevRawList))
                            {
                                var prevList = prevRawList.ToList(StateExtensions.ToAddress);
                                foreach (var address in prevList.Where(address => !addressList.Contains(address)))
                                {
                                    addressList.Add(address);
                                }
                            }

                            // Copy activated ArenaInfo from prev ArenaInfo.
                            foreach (var address in addressList)
                            {
                                if (states.TryGetLegacyState(
                                        prevWeekly.address.Derive(address.ToByteArray()),
                                        out Dictionary rawInfo))
                                {
                                    var prevInfo = new ArenaInfo(rawInfo);
                                    var record = prevInfo.ArenaRecord;
                                    // Filter ArenaInfo
                                    if (filterInactive && record.Win == 0 && record.Draw == 0 &&
                                        record.Lose == 0)
                                    {
                                        continue;
                                    }

                                    nextAddresses = nextAddresses.Add(address.Serialize());
                                    states = states.SetLegacyState(
                                        weeklyAddress.Derive(address.ToByteArray()),
                                        new ArenaInfo(prevInfo).Serialize());
                                }
                            }
                        }
                        // Set address list.
                        states = states.SetLegacyState(listAddress, nextAddresses);
                    }
                    // Run legacy Update.
                    else
                    {
                        weekly.Update(prevWeekly, ctx.BlockIndex);
                    }

                    states = states.SetLegacyState(prevWeeklyAddress, rawPrevWeekly);
                    states = states.SetLegacyState(weeklyAddress, weekly.Serialize());
                }
            }
            return states;
        }

        public IWorld ResetChallengeCount(IActionContext ctx, IWorld states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary) states.GetLegacyState(weeklyAddress);
            var resetIndex = rawWeekly["resetIndex"].ToLong();

            if (ctx.BlockIndex - resetIndex >= gameConfigState.DailyArenaInterval)
            {
                var weekly = new WeeklyArenaState(rawWeekly);
                if (resetIndex >= RankingBattle11.UpdateTargetBlockIndex)
                {
                    // Reset count each ArenaInfo.
                    weekly.ResetIndex = ctx.BlockIndex;
                    var listAddress = weeklyAddress.Derive("address_list");
                    if (states.TryGetLegacyState(listAddress, out List rawList))
                    {
                        var addressList = rawList.ToList(StateExtensions.ToAddress);
                        foreach (var address in addressList)
                        {
                            var infoAddress = weeklyAddress.Derive(address.ToByteArray());
                            if (states.TryGetLegacyState(infoAddress, out Dictionary rawInfo))
                            {
                                var info = new ArenaInfo(rawInfo);
                                info.ResetCount();
                                states = states.SetLegacyState(infoAddress, info.Serialize());
                            }
                        }
                    }
                }
                else
                {
                    // Run legacy ResetCount.
                    weekly.ResetCount(ctx.BlockIndex);
                }
                states = states.SetLegacyState(weeklyAddress, weekly.Serialize());
            }
            return states;
        }

        public IWorld MinerReward(IActionContext ctx, IWorld states)
        {
            Currency currency = Currencies.Mead;
            var usedGas = states.GetBalance(Addresses.GasPool, currency);
            var defaultReward = currency * 5;
            var halfOfUsedGas = usedGas.DivRem(2).Quotient;
            var gasToBurn = usedGas - halfOfUsedGas;
            var miningReward = halfOfUsedGas + defaultReward;
            states = states.MintAsset(ctx, Addresses.GasPool, defaultReward);
            if (gasToBurn.Sign > 0)
            {
                states = states.BurnAsset(ctx, Addresses.GasPool, gasToBurn);
            }
            states = states.TransferAsset(
                ctx, Addresses.GasPool, Addresses.RewardPool, miningReward);

            var goldCurrency = states.GetGoldCurrency();
            if (states.GetBalance(GoldCurrencyState.Address, goldCurrency).Sign > 0)
            {
                states = states.TransferAsset(
                    ctx,
                    GoldCurrencyState.Address,
                    Addresses.RewardPool,
                    states.GetGoldCurrency() * 1000);
            }

            return states;
        }

        public static IWorld TransferMead(IActionContext context, IWorld states)
        {
            var targetAddresses = context.Txs
                .Where(tx => tx.MaxGasPrice is { } price && price.Currency.Equals(Currencies.Mead))
                .Select(tx => tx.Signer)
                .Distinct();
            foreach (var address in targetAddresses)
            {
                var contractAddress = address.GetPledgeAddress();
                if (states.TryGetLegacyState(contractAddress, out List contract) &&
                    contract[1].ToBoolean())
                {
                    try
                    {
                        states = states.Mead(context, address, contract[2].ToInteger());
                    }
                    catch (InsufficientBalanceException)
                    {
                    }
                }
            }

            return states;
        }

    }
}
