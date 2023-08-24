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
using Nekoyume.Action.Extensions;
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
        public const long RankingBattle11UpdateTargetBlockIndex = 3_808_000L;
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            world = TransferMead(context, world);
            world = GenesisGoldDistribution(context, world);
            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold exec started", addressesHex);

            // Avoid InvalidBlockStateRootHashException before table patch.
            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            // Avoid InvalidBlockStateRootHashException in unit test genesis block evaluate.
            if (LegacyModule.GetState(world, arenaSheetAddress) is null || context.BlockIndex == 0)
            {
                world = WeeklyArenaRankingBoard2(context, world);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return MinerReward(context, world);
        }

        public IWorld GenesisGoldDistribution(IActionContext ctx, IWorld world)
        {
            IEnumerable<GoldDistribution> goldDistributions =
                LegacyModule.GetGoldDistribution(world);
            var index = ctx.BlockIndex;
            Currency goldCurrency = LegacyModule.GetGoldCurrency(world);
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
                world = LegacyModule.TransferAsset(
                    world,
                    ctx,
                    fund,
                    distribution.Address,
                    fav
                );
            }
            return world;
        }

        [Obsolete("Use WeeklyArenaRankingBoard2 for performance.")]
        public IWorld WeeklyArenaRankingBoard(IActionContext ctx, IWorld world)
        {
            var gameConfigState = LegacyModule.GetGameConfigState(world);
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = LegacyModule.GetWeeklyArenaState(world, index);
            var nextIndex = index + 1;
            var nextWeekly = LegacyModule.GetWeeklyArenaState(world, nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                world = LegacyModule.SetState(world, nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = LegacyModule.GetWeeklyArenaState(world, index - 1);
                if (!prevWeekly.Ended)
                {
                    prevWeekly.End();
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    world = LegacyModule.SetState(
                        world,
                        prevWeekly.address,
                        prevWeekly.Serialize());
                    world = LegacyModule.SetState(world, weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                world = LegacyModule.SetState(world, weekly.address, weekly.Serialize());
            }
            return world;
        }

        public IWorld WeeklyArenaRankingBoard2(IActionContext ctx, IWorld world)
        {
            world = PrepareNextArena(ctx, world);
            world = ResetChallengeCount(ctx, world);
            return world;
        }

        public IWorld PrepareNextArena(IActionContext ctx, IWorld world)
        {
            var gameConfigState = LegacyModule.GetGameConfigState(world);
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary)LegacyModule.GetState(world, weeklyAddress);
            var nextIndex = index + 1;
            var nextWeekly = LegacyModule.GetWeeklyArenaState(world, nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                world = LegacyModule.SetState(world, nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeeklyAddress = WeeklyArenaState.DeriveAddress(index - 1);
                var rawPrevWeekly = (Dictionary)LegacyModule.GetState(world, prevWeeklyAddress);
                if (!rawPrevWeekly["ended"].ToBoolean())
                {
                    rawPrevWeekly = rawPrevWeekly.SetItem("ended", true.Serialize());
                    var weekly = new WeeklyArenaState(rawWeekly);
                    var prevWeekly = new WeeklyArenaState(rawPrevWeekly);
                    var listAddress = weekly.address.Derive("address_list");
                    // Set ArenaInfo, address list for new RankingBattle.
                    var addressList = LegacyModule.TryGetState(world, listAddress, out List rawList)
                        ? rawList.ToList(StateExtensions.ToAddress)
                        : new List<Address>();
                    var nextAddresses = rawList ?? List.Empty;
                    if (ctx.BlockIndex >= RankingBattle11UpdateTargetBlockIndex)
                    {
                        weekly.ResetIndex = ctx.BlockIndex;

                        // Copy Map to address list.
                        if (ctx.BlockIndex == RankingBattle11UpdateTargetBlockIndex)
                        {
                            foreach (var kv in prevWeekly.Map)
                            {
                                var address = kv.Key;
                                var lazyInfo = kv.Value;
                                var info = new ArenaInfo(lazyInfo.State);
                                world = LegacyModule.SetState(
                                    world,
                                    weeklyAddress.Derive(address.ToByteArray()),
                                    info.Serialize());
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

                            if (LegacyModule.TryGetState(
                                    world,
                                    prevListAddress,
                                    out List prevRawList))
                            {
                                var prevList = prevRawList.ToList(StateExtensions.ToAddress);
                                foreach (var address in prevList.Where(
                                             address => !addressList.Contains(address)))
                                {
                                    addressList.Add(address);
                                }
                            }

                            // Copy activated ArenaInfo from prev ArenaInfo.
                            foreach (var address in addressList)
                            {
                                if (LegacyModule.TryGetState(
                                        world,
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
                                    world = LegacyModule.SetState(
                                        world,
                                        weeklyAddress.Derive(address.ToByteArray()),
                                        new ArenaInfo(prevInfo).Serialize());
                                }
                            }
                        }
                        // Set address list.
                        world = LegacyModule.SetState(world, listAddress, nextAddresses);
                    }
                    // Run legacy Update.
                    else
                    {
                        weekly.Update(prevWeekly, ctx.BlockIndex);
                    }

                    world = LegacyModule.SetState(world, prevWeeklyAddress, rawPrevWeekly);
                    world = LegacyModule.SetState(world, weeklyAddress, weekly.Serialize());
                }
            }
            return world;
        }

        public IWorld ResetChallengeCount(IActionContext ctx, IWorld world)
        {
            var gameConfigState = LegacyModule.GetGameConfigState(world);
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary)LegacyModule.GetState(world, weeklyAddress);
            var resetIndex = rawWeekly["resetIndex"].ToLong();

            if (ctx.BlockIndex - resetIndex >= gameConfigState.DailyArenaInterval)
            {
                var weekly = new WeeklyArenaState(rawWeekly);
                if (resetIndex >= RankingBattle11UpdateTargetBlockIndex)
                {
                    // Reset count each ArenaInfo.
                    weekly.ResetIndex = ctx.BlockIndex;
                    var listAddress = weeklyAddress.Derive("address_list");
                    if (LegacyModule.TryGetState(world, listAddress, out List rawList))
                    {
                        var addressList = rawList.ToList(StateExtensions.ToAddress);
                        foreach (var address in addressList)
                        {
                            var infoAddress = weeklyAddress.Derive(address.ToByteArray());
                            if (LegacyModule.TryGetState(world, infoAddress, out Dictionary rawInfo))
                            {
                                var info = new ArenaInfo(rawInfo);
                                info.ResetCount();
                                world = LegacyModule.SetState(world, infoAddress, info.Serialize());
                            }
                        }
                    }
                }
                else
                {
                    // Run legacy ResetCount.
                    weekly.ResetCount(ctx.BlockIndex);
                }
                world = LegacyModule.SetState(world, weeklyAddress, weekly.Serialize());
            }
            return world;
        }

        public IWorld MinerReward(IActionContext ctx, IWorld world)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            Currency currency = LegacyModule.GetGoldCurrency(world);
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int)Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
            FungibleAssetValue miningReward =
                defaultMiningReward.DivRem(countOfHalfLife, out FungibleAssetValue _);

            if (miningReward >= FungibleAssetValue.Parse(currency, "1.25"))
            {
                world = LegacyModule.TransferAsset(
                    world,
                    ctx,
                    GoldCurrencyState.Address,
                    ctx.Miner,
                    miningReward
                );
            }

            return world;
        }

        public static IWorld TransferMead(IActionContext context, IWorld world)
        {
#pragma warning disable LAA1002
            var targetAddresses = LegacyModule.TotalUpdatedFungibleAssets(world)
#pragma warning restore LAA1002
                .Where(pair => pair.Item2.Equals(Currencies.Mead))
                .Select(pair => pair.Item1)
                .Distinct();
            foreach (var address in targetAddresses)
            {
                var contractAddress = address.GetPledgeAddress();
                if (LegacyModule.TryGetState(world, contractAddress, out List contract) &&
                    contract[1].ToBoolean())
                {
                    try
                    {
                        world = LegacyModule.Mead(world, context, address, contract[2].ToInteger());
                    }
                    catch (InsufficientBalanceException)
                    {
                    }
                }
            }

            return world;
        }

    }
}
