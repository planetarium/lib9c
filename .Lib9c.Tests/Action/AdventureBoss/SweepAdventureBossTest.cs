namespace Lib9c.Tests.Action.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Extensions;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.AdventureBoss;
    using Xunit;

    public class SweepAdventureBossTest
    {
        // See UseBoostTestBoss.
        private const int BoostTestAdventureBossId = 2;

        private static readonly Dictionary<string, string> Sheets =
            TableSheetsImporter.ImportSheets();

        private static readonly TableSheets TableSheets = new (Sheets);
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

        // Wanted
        private static readonly Address WantedAddress = new PrivateKey().Address;

        private static readonly Address WantedAvatarAddress =
            Addresses.GetAvatarAddress(WantedAddress, 0);

        private static readonly AvatarState WantedAvatarState = AvatarState.Create(
            WantedAvatarAddress,
            WantedAddress,
            0L,
            TableSheets.GetAvatarSheets(),
            new PrivateKey().Address,
            "wanted"
        );

        private static readonly AgentState WantedState = new (WantedAddress)
        {
            avatarAddresses = { [0] = WantedAvatarAddress, },
        };

        // Test Account
        private static readonly Address TesterAddress =
            new ("2000000000000000000000000000000000000002");

        private static readonly Address TesterAvatarAddress =
            Addresses.GetAvatarAddress(TesterAddress, 0);

        private static readonly AvatarState TesterAvatarState = AvatarState.Create(
            TesterAvatarAddress,
            TesterAddress,
            0L,
            TableSheets.GetAvatarSheets(),
            new PrivateKey().Address,
            "Tester"
        );

        private static readonly AgentState TesterState = new (TesterAddress)
        {
            avatarAddresses =
            {
                [0] = TesterAvatarAddress,
            },
        };

        private readonly IWorld _initialState = new World(MockUtil.MockModernWorldState)
            .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(NCG).Serialize())
            .SetLegacyState(
                GameConfigState.Address,
                new GameConfigState(Sheets["GameConfigSheet"]).Serialize()
            )
            .SetAvatarState(WantedAvatarAddress, WantedAvatarState)
            .SetAgentState(WantedAddress, WantedState)
            .SetAvatarState(TesterAvatarAddress, TesterAvatarState)
            .SetAgentState(TesterAddress, TesterState)
            .MintAsset(new ActionContext(), WantedAddress, 1_000_000 * NCG);

        public static IEnumerable<object[]> GetExecuteMemberData()
        {
            yield return new object[]
            {
                0, 100, 100, typeof(InvalidOperationException), null,
            };
            yield return new object[]
            {
                1, 100, 99, null, new[] { (600302, 3), },
            };
            yield return new object[]
            {
                1, 0, 0, typeof(NotEnoughMaterialException), null,
            };
            yield return new object[]
            {
                10, 1, 1, typeof(NotEnoughMaterialException), null,
            };
            yield return new object[]
            {
                20, 20, 0, null, new[] { (600302, 27), (600303, 14), (600304, 14), },
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(
            int floor,
            int initialPotion,
            int expectedPotion,
            Type exc,
            (int, int)[] expectedRewards
        )
        {
            // Settings
            var state = _initialState;
            var gameConfigState = new GameConfigState(Sheets[nameof(GameConfigSheet)]);
            state = state.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var validatorKey = new PrivateKey().PublicKey;
            state = DelegationUtil.EnsureValidatorPromotionReady(state, validatorKey, 0L);
            state = DelegationUtil.MakeGuild(state, WantedAddress, validatorKey.Address, 0L);

            state = Stake(state, WantedAddress);
            var sheets = state.GetSheets(
                new[]
                {
                    typeof(MaterialItemSheet),
                });
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var materialRow =
                materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
            var apPotion = ItemFactory.CreateMaterial(materialRow);

            var inventory = state.GetInventoryV2(TesterAvatarAddress);
            if (initialPotion > 0)
            {
                inventory.AddItem(apPotion, initialPotion);
            }

            state = state.SetInventory(TesterAvatarAddress, inventory);

            // Open season
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = gameConfigState.AdventureBossMinBounty * NCG,
            }.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = WantedAddress,
                    BlockIndex = 0L,
                    RandomSeed = 1,
                });
            var exp = new Explorer(TesterAvatarAddress, TesterAvatarState.name)
            {
                MaxFloor = 5 * Math.Max(floor / 5, 1),
                Floor = floor,
            };
            state = state.SetExplorer(1, exp);

            // Sweep and Test
            var action = new SweepAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };

            if (exc is not null)
            {
                Assert.Throws(
                    exc,
                    () => action.Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Signer = TesterAddress,
                            BlockIndex = 1L,
                        }
                    ));
            }
            else
            {
                state = action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = TesterAddress,
                        BlockIndex = 1L,
                    });

                var potion = state.GetInventoryV2(TesterAvatarAddress).Items
                    .FirstOrDefault(i => i.item.ItemSubType == ItemSubType.ApStone);
                if (expectedPotion == 0)
                {
                    Assert.Null(potion);
                }
                else
                {
                    Assert.Equal(expectedPotion, potion!.count);
                }

                var unitSweepAp = state.GetSheet<AdventureBossSheet>().OrderedList
                    .First(row => row.BossId == state.GetLatestAdventureBossSeason().BossId)
                    .SweepAp;
                var exploreBoard = state.GetExploreBoard(1);
                var explorer = state.GetExplorer(1, TesterAvatarAddress);
                Assert.True(explorer.Score > 0);
                Assert.True(exploreBoard.TotalPoint > 0);
                Assert.Equal(explorer.Score, exploreBoard.TotalPoint);
                Assert.Equal(floor, explorer.Floor);
                Assert.Equal(floor * unitSweepAp, exploreBoard.UsedApPotion);
                Assert.Equal(explorer.UsedApPotion, exploreBoard.UsedApPotion);

                inventory = state.GetInventoryV2(TesterAvatarAddress);
                var circleRow = materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.Circle);
                foreach (var (id, amount) in expectedRewards)
                {
                    if (amount == 0)
                    {
                        Assert.Null(inventory.Items.FirstOrDefault(i => i.item.Id == id));
                    }
                    else if (id == circleRow.Id)
                    {
                        var itemCount = inventory.TryGetTradableFungibleItems(circleRow.ItemId, null, 1L, out var items)
                            ? items.Sum(item => item.count)
                            : 0;
                        Assert.Equal(amount, itemCount);
                    }
                    else
                    {
                        Assert.Equal(amount, inventory.Items.First(i => i.item.Id == id).count);
                    }
                }

                var nextCpAccount = state.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure));
                var nextCpState = new CpState(nextCpAccount.GetState(TesterAvatarAddress));

                Assert.True(nextCpState.Cp > 0);
            }
        }

        [Fact]
        public void Execute_AppliesScheduledFloorRewardBoost()
        {
            var (baseRewards, baseScore) = SweepWithBoostSchedule(null);
            var (boosted, boostedScore) = SweepWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, "*", 0, 10));

            // ×2 scales granted amounts only, so the points drawn between rewards stay the same.
            Assert.Contains(baseRewards, pair => pair.Value > 0);
            Assert.Equal(baseScore, boostedScore);
            Assert.Equal(
                baseRewards.ToDictionary(pair => pair.Key, pair => pair.Value * 2),
                boosted);
        }

        [Fact]
        public void Execute_MatchesFloorRewardBoostByFloorNumber()
        {
            var (baseRewards, baseScore) = SweepWithBoostSchedule(null);

            // ×2 on a single floor grants exactly that floor's one reward entry once more, and
            // the single-floor extras of all 20 floors add up to the whole sweep.
            var total = new Dictionary<string, BigInteger>();
            for (var floor = 1; floor <= 20; floor++)
            {
                var (rewards, score) = SweepWithBoostSchedule(
                    BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, $"{floor}", 0, 10));
                Assert.Equal(baseScore, score);
                var extra = Combine(rewards, baseRewards, -1);
                Assert.Single(extra);
                Assert.True(extra.Single().Value > 0, $"floor {floor}");
                total = Combine(total, extra, 1);
            }

            Assert.Equal(baseRewards, total);
        }

        [Fact]
        public void Execute_AppliesRangeBoostToThoseFloorsOnly()
        {
            var (baseRewards, _) = SweepWithBoostSchedule(null);
            var expected = baseRewards;
            for (var floor = 2; floor <= 4; floor++)
            {
                var (single, _) = SweepWithBoostSchedule(
                    BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, $"{floor}", 0, 10));
                expected = Combine(expected, Combine(single, baseRewards, -1), 1);
            }

            // An inner range catches an off-by-one at either end.
            var (inner, _) = SweepWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, "2~4", 0, 10));
            Assert.Equal(expected, inner);
        }

        [Theory]
        // Sweep grants no first-clear reward.
        [InlineData("ADVENTURE_BOSS_FIRST_CLEAR_REWARD", "*", 0L, 10L)]
        // No floor in range.
        [InlineData("ADVENTURE_BOSS_FLOOR_REWARD", "21~30", 0L, 10L)]
        // Before and after the scheduled blocks.
        [InlineData("ADVENTURE_BOSS_FLOOR_REWARD", "*", 2L, 10L)]
        [InlineData("ADVENTURE_BOSS_FLOOR_REWARD", "*", 0L, 1L)]
        public void Execute_IgnoresBoostThatDoesNotMatch(
            string target,
            string targetId,
            long start,
            long end)
        {
            var (baseRewards, baseScore) = SweepWithBoostSchedule(null);
            var (rewards, score) = SweepWithBoostSchedule(BoostCsv(target, targetId, start, end));

            Assert.Equal(baseScore, score);
            Assert.Equal(baseRewards, rewards);
        }

        [Fact]
        public void Execute_IsUnchangedWhenTheChainWasNeverPatchedWithBoostScheduleSheet()
        {
            var (neverPatched, neverPatchedScore) = SweepWithBoostSchedule(null);
            var (emptySheet, emptySheetScore) = SweepWithBoostSchedule(
                "id,target,target_id,op,value,start_block,end_block\n");

            Assert.Contains(neverPatched, pair => pair.Value > 0);
            Assert.Equal(neverPatchedScore, emptySheetScore);
            Assert.Equal(neverPatched, emptySheet);
        }

        private static string BoostCsv(string target, string targetId, long start, long end) =>
            "id,target,target_id,op,value,start_block,end_block\n" +
            $"1,{target},{targetId},MUL,2,{start},{end}\n";

        // Sums or subtracts per key, leaving out keys that end at zero.
        private static Dictionary<string, BigInteger> Combine(
            Dictionary<string, BigInteger> left,
            Dictionary<string, BigInteger> right,
            int sign) =>
            left.Keys.Union(right.Keys)
                .Select(key => (key, value: left.GetValueOrDefault(key) + (sign * right.GetValueOrDefault(key))))
                .Where(pair => pair.value != 0)
                .ToDictionary(pair => pair.key, pair => pair.value);

        // Makes season 1 fight AdventureBossSheet id 2, whose floor rows are numbered from 31, so
        // that a boost matched against the floor row id instead of the floor number misses.
        private static IWorld UseBoostTestBoss(IWorld state)
        {
            var floorOneRow = TableSheets.AdventureBossFloorSheet.Values
                .First(row => row.AdventureBossId == BoostTestAdventureBossId && row.Floor == 1);
            Assert.NotEqual(1, floorOneRow.Id);
            var season = state.GetSeasonInfo(1);
            season.BossId = TableSheets.AdventureBossSheet[BoostTestAdventureBossId].BossId;
            return state.SetSeasonInfo(season).SetLatestAdventureBossSeason(season);
        }

        // Sweeps 20 cleared floors at block 1 with a fixed seed, with the BoostScheduleSheet
        // given as CSV or, for null, a chain that was never patched with it. Returns what was
        // granted, by item id and currency ticker, and the explorer's score.
        private (Dictionary<string, BigInteger> Rewards, int Score) SweepWithBoostSchedule(
            string boostCsv)
        {
            var state = _initialState;
            var gameConfigState = new GameConfigState(Sheets[nameof(GameConfigSheet)]);
            state = state.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            foreach (var (key, value) in Sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            state = state.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(BoostScheduleSheet)),
                boostCsv is null ? Null.Value : boostCsv.Serialize());

            var validatorKey = new PrivateKey().PublicKey;
            state = DelegationUtil.EnsureValidatorPromotionReady(state, validatorKey, 0L);
            state = DelegationUtil.MakeGuild(state, WantedAddress, validatorKey.Address, 0L);
            state = Stake(state, WantedAddress);

            var apPotionRow = TableSheets.MaterialItemSheet.OrderedList
                .First(row => row.ItemSubType == ItemSubType.ApStone);
            var inventory = state.GetInventoryV2(TesterAvatarAddress);
            inventory.AddItem(ItemFactory.CreateMaterial(apPotionRow), 20);
            state = state.SetInventory(TesterAvatarAddress, inventory);

            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = gameConfigState.AdventureBossMinBounty * NCG,
            }.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = WantedAddress,
                    BlockIndex = 0L,
                    RandomSeed = 1,
                });
            state = UseBoostTestBoss(state);
            Assert.Equal(
                TableSheets.AdventureBossSheet[BoostTestAdventureBossId].BossId,
                state.GetLatestAdventureBossSeason().BossId);
            state = state.SetExplorer(
                1,
                new Explorer(TesterAvatarAddress, TesterAvatarState.name)
                {
                    MaxFloor = 20,
                    Floor = 20,
                });
            var before = Granted(state);

            state = new SweepAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            }.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = TesterAddress,
                    BlockIndex = 1L,
                    RandomSeed = 7,
                });

            return (
                Combine(Granted(state), before, -1),
                state.GetExplorer(1, TesterAvatarAddress).Score);
        }

        private Dictionary<string, BigInteger> Granted(IWorld state)
        {
            var granted = new Dictionary<string, BigInteger>();
            foreach (var item in state.GetInventoryV2(TesterAvatarAddress).Items
                         .Where(i => i.item.ItemSubType != ItemSubType.ApStone))
            {
                var key = $"item:{item.item.Id}";
                granted[key] = granted.GetValueOrDefault(key) + item.count;
            }

            var currencies = TableSheets.RuneSheet.Values
                .Select(row => Currencies.GetRune(row.Ticker))
                .Append(Currencies.Crystal);
            foreach (var currency in currencies)
            {
                var balance = state.GetBalance(TesterAvatarAddress, currency).MajorUnit;
                if (balance != 0)
                {
                    granted[$"fav:{currency.Ticker}"] = balance;
                }
            }

            return granted;
        }

        private IWorld Stake(IWorld world, Address agentAddress)
        {
            var action = new Stake(new BigInteger(500_000), TesterAvatarAddress);
            var state = action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = agentAddress,
                    BlockIndex = 0L,
                });
            return state;
        }
    }
}
