namespace Lib9c.Tests.Action.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV;
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
    using Xunit;

    public class ExploreAdventureBossTest
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

        static ExploreAdventureBossTest()
        {
            TesterAvatarState.level = 500;
        }

        public ExploreAdventureBossTest()
        {
            var collectionSheet = new CollectionSheet();
            collectionSheet.Set(CollectionSheetFixture.Default);
            var collectionState = new CollectionState();
            foreach (var row in collectionSheet.Values)
            {
                collectionState.Ids.Add(row.Id);
            }

            _initialState = _initialState.SetCollectionState(TesterAvatarAddress, collectionState);
        }

        // Member Data
        public static IEnumerable<object[]> GetExecuteMemberData()
        {
            // No AP potion at all
            yield return new object[]
            {
                0, 5, 0, 0, 0, null,
            };
            // Start from bottom, goes to 5
            yield return new object[]
            {
                0, 5, 5, 20, 10, null, // 2 potions per floor
            };
            // Start from bottom, goes to 3 because of potion
            yield return new object[]
            {
                0, 5, 3, 6, 0, null,
            };
            // Start from 3, goes to 5 because of locked floor
            yield return new object[]
            {
                2, 5, 5, 10, 4, null,
            };
            // Start from 6, goes to 9
            yield return new object[]
            {
                5, 10, 9, 20, 10, null,
            };
            // Start from 20, cannot enter
            yield return new object[]
            {
                20, 20, 20, 10, 10, typeof(InvalidOperationException),
            };
        }

        [Theory]
        [MemberData(nameof(GetExecuteMemberData))]
        public void Execute(
            int floor,
            int maxFloor,
            int expectedFloor,
            int initialPotion,
            int expectedPotion,
            Type exc
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

            // override sheet
            state = state.SetLegacyState(
                Addresses.GetSheetAddress<CollectionSheet>(),
                CollectionSheetFixture.Default.Serialize()
            );

            state = Stake(state, WantedAddress);
            var sheets = state.GetSheets(
                new[]
                {
                    typeof(MaterialItemSheet),
                    typeof(RuneSheet),
                });
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var materialRow =
                materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.ApStone);
            var apPotion = ItemFactory.CreateMaterial(materialRow);

            if (initialPotion > 0)
            {
                var inventory = state.GetInventoryV2(TesterAvatarAddress);
                inventory.AddItem(apPotion, initialPotion);
                state = state.SetInventory(TesterAvatarAddress, inventory);
            }

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
                MaxFloor = maxFloor,
                Floor = floor,
            };
            state = state.SetExplorer(1, exp);

            // Explore and Test
            var itemSlotStateAddress =
                ItemSlotState.DeriveAddress(TesterAvatarAddress, BattleType.Adventure);
            var itemSlotState =
                state.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                    ? new ItemSlotState(rawItemSlotState)
                    : new ItemSlotState(BattleType.Adventure);
            Assert.True(itemSlotState.Equipments.Count == 0);

            // Two floors in the same range can grant the same item, and the assertions below
            // compare against the inventory's aggregate count, so accumulate per item id
            // instead of keeping one entry per floor.
            var expectedItemRewards = new Dictionary<int, int>();
            var expectedFavRewards = new Dictionary<int, int>();
            var firstRewardSheet = TableSheets.AdventureBossFloorFirstRewardSheet;
            foreach (var row in firstRewardSheet.Values.Where(
                r =>
                    r.FloorId > floor && r.FloorId <= expectedFloor))
            {
                foreach (var reward in row.Rewards)
                {
                    switch (reward.ItemType)
                    {
                        // The loop below resolves every key through RuneSheet, so a
                        // Crystal row in the sheet would not work here yet.
                        case "Rune":
                        case "Crystal":
                            expectedFavRewards[reward.ItemId] =
                                expectedFavRewards.GetValueOrDefault(reward.ItemId) +
                                reward.Amount;
                            break;
                        case "Material":
                            expectedItemRewards[reward.ItemId] =
                                expectedItemRewards.GetValueOrDefault(reward.ItemId) +
                                reward.Amount;
                            break;
                    }
                }
            }

            var action = new ExploreAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
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

                var exploreBoard = state.GetExploreBoard(1);
                var explorer = state.GetExplorer(1, TesterAvatarAddress);

                Assert.Equal(initialPotion - expectedPotion, exploreBoard.UsedApPotion);
                Assert.Equal(expectedFloor, explorer.Floor);

                var inventory = state.GetInventoryV2(TesterAvatarAddress);
                var circleRow =
                    materialSheet.OrderedList.First(row => row.ItemSubType == ItemSubType.Circle);
                foreach (var (id, amount) in expectedItemRewards)
                {
                    if (amount == 0)
                    {
                        Assert.Null(inventory.Items.FirstOrDefault(i => i.item.Id == id));
                    }
                    else if (id == circleRow.Id)
                    {
                        var itemCount =
                            inventory.TryGetTradableFungibleItems(
                                circleRow.ItemId,
                                null,
                                1L,
                                out var items
                            )
                                ? items.Sum(item => item.count)
                                : 0;
                        Assert.Equal(amount, itemCount);
                    }
                    else
                    {
                        Assert.True(amount <= inventory.Items.First(i => i.item.Id == id).count);
                    }
                }

                var runeSheet = sheets.GetSheet<RuneSheet>();
                foreach (var (id, amount) in expectedFavRewards)
                {
                    var ticker = runeSheet.Values.First(rune => rune.Id == id).Ticker;
                    var currency = Currencies.GetRune(ticker);
                    Assert.True(
                        amount * currency <= state.GetBalance(TesterAvatarAddress, currency));
                }

                var nextCpAccount = state.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure));
                var nextCpState = new CpState(nextCpAccount.GetState(TesterAvatarAddress));

                Assert.True(nextCpState.Cp > 0);
            }
        }

        [Fact]
        public void Execute_AppliesScheduledRewardBoost()
        {
            var (baseRewards, baseScore, adventureBossId) = ExploreWithBoostSchedule(null);
            var firstClear = FirstClearRewards(adventureBossId, 1, 5);
            var floorRewards = Combine(baseRewards, firstClear, -1);
            Assert.NotEmpty(firstClear);
            Assert.Contains(floorRewards, pair => pair.Value > 0);

            var (floorBoosted, floorBoostedScore, _) = ExploreWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, "1~5", 0, 10));
            var (firstBoosted, firstBoostedScore, _) = ExploreWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFirstClearReward, "1~5", 0, 10));

            // ×2 scales granted amounts only, so every draw — including the score drawn after
            // each floor reward — stays where it was, and each target doubles only its own part.
            Assert.Equal(baseScore, floorBoostedScore);
            Assert.Equal(baseScore, firstBoostedScore);
            Assert.Equal(Combine(baseRewards, floorRewards, 1), floorBoosted);
            Assert.Equal(Combine(baseRewards, firstClear, 1), firstBoosted);
        }

        [Fact]
        public void Execute_MatchesFloorRewardBoostByFloorNumber()
        {
            var (baseRewards, baseScore, adventureBossId) = ExploreWithBoostSchedule(null);
            var floorRewards = Combine(baseRewards, FirstClearRewards(adventureBossId, 1, 5), -1);

            // ×2 on a single floor grants exactly that floor's one reward entry once more.
            var perFloor = new List<Dictionary<string, BigInteger>>();
            for (var floor = 1; floor <= 5; floor++)
            {
                var (rewards, score, _) = ExploreWithBoostSchedule(
                    BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, $"{floor}", 0, 10));
                Assert.Equal(baseScore, score);
                var extra = Combine(rewards, baseRewards, -1);
                Assert.Single(extra);
                Assert.True(extra.Single().Value > 0, $"floor {floor}");
                perFloor.Add(extra);
            }

            Assert.Equal(floorRewards, perFloor.Aggregate((a, b) => Combine(a, b, 1)));

            // An inner range catches an off-by-one at either end.
            var (inner, innerScore, _) = ExploreWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, "2~4", 0, 10));
            Assert.Equal(baseScore, innerScore);
            var expected = perFloor.Skip(1).Take(3).Aggregate(baseRewards, (a, b) => Combine(a, b, 1));
            Assert.Equal(expected, inner);
        }

        [Fact]
        public void Execute_MatchesFirstClearRewardBoostByFloorNumber()
        {
            var (baseRewards, baseScore, adventureBossId) = ExploreWithBoostSchedule(null);
            var (inner, innerScore, _) = ExploreWithBoostSchedule(
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFirstClearReward, "2~4", 0, 10));

            Assert.Equal(baseScore, innerScore);
            Assert.Equal(
                Combine(baseRewards, FirstClearRewards(adventureBossId, 2, 4), 1),
                inner);
        }

        [Theory]
        [InlineData(2L, 10L)]
        [InlineData(0L, 1L)]
        public void Execute_IgnoresRewardBoostOutsideItsBlockRange(long start, long end)
        {
            var (baseRewards, baseScore, _) = ExploreWithBoostSchedule(null);
            var csv =
                BoostCsv(BoostScheduleSheet.Targets.AdventureBossFloorReward, "*", start, end) +
                $"2,{BoostScheduleSheet.Targets.AdventureBossFirstClearReward},*,MUL,2,{start},{end}\n";
            var (rewards, score, _) = ExploreWithBoostSchedule(csv);

            Assert.Equal(baseScore, score);
            Assert.Equal(baseRewards, rewards);
        }

        [Fact]
        public void Execute_IsUnchangedWhenTheChainWasNeverPatchedWithBoostScheduleSheet()
        {
            var (neverPatched, neverPatchedScore, _) = ExploreWithBoostSchedule(null);
            var (emptySheet, emptySheetScore, _) = ExploreWithBoostSchedule(
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

        private static string RewardKey(string itemType, int itemId) =>
            itemType switch
            {
                "Material" => $"item:{itemId}",
                "Rune" => $"fav:{TableSheets.RuneSheet[itemId].Ticker}",
                "Crystal" => "fav:CRYSTAL",
                _ => throw new ArgumentException(itemType),
            };

        // First-clear rewards of floors fromFloor~toFloor (floor numbers) of the given boss.
        private static Dictionary<string, BigInteger> FirstClearRewards(
            int adventureBossId,
            int fromFloor,
            int toFloor)
        {
            var rewards = new Dictionary<string, BigInteger>();
            foreach (var floorRow in TableSheets.AdventureBossFloorSheet.Values.Where(
                         r => r.AdventureBossId == adventureBossId &&
                              fromFloor <= r.Floor && r.Floor <= toFloor))
            {
                var firstReward = TableSheets.AdventureBossFloorFirstRewardSheet[floorRow.Id];
                foreach (var reward in firstReward.Rewards)
                {
                    var key = RewardKey(reward.ItemType, reward.ItemId);
                    rewards[key] = rewards.GetValueOrDefault(key) + reward.Amount;
                }
            }

            return rewards;
        }

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

        // Explores floors 1~5 at block 1 with a fixed seed, with the BoostScheduleSheet given as
        // CSV or, for null, a chain that was never patched with it. Returns what was granted, by
        // item id and currency ticker, the explorer's score and the season's AdventureBossSheet id.
        private (Dictionary<string, BigInteger> Rewards, int Score, int AdventureBossId)
            ExploreWithBoostSchedule(
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
                Addresses.GetSheetAddress<CollectionSheet>(),
                CollectionSheetFixture.Default.Serialize());
            state = state.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(BoostScheduleSheet)),
                boostCsv is null ? Null.Value : boostCsv.Serialize());

            state = Stake(state, WantedAddress);
            var apPotionRow = TableSheets.MaterialItemSheet.OrderedList
                .First(row => row.ItemSubType == ItemSubType.ApStone);
            var inventory = state.GetInventoryV2(TesterAvatarAddress);
            inventory.AddItem(ItemFactory.CreateMaterial(apPotionRow), 20);
            state = state.SetInventory(TesterAvatarAddress, inventory);

            var gameConfig = new GameConfigState(Sheets[nameof(GameConfigSheet)]);
            state = new Wanted
            {
                Season = 1,
                AvatarAddress = WantedAvatarAddress,
                Bounty = gameConfig.AdventureBossMinBounty * NCG,
            }.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = WantedAddress,
                    BlockIndex = 0L,
                    RandomSeed = 1,
                });
            state = UseBoostTestBoss(state);
            state = state.SetExplorer(
                1,
                new Explorer(TesterAvatarAddress, TesterAvatarState.name) { MaxFloor = 5, });
            var before = Granted(state);

            state = new ExploreAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid>(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            }.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = TesterAddress,
                    BlockIndex = 1L,
                    RandomSeed = 7,
                });

            var explorer = state.GetExplorer(1, TesterAvatarAddress);
            Assert.Equal(5, explorer.Floor);
            var bossId = state.GetLatestAdventureBossSeason().BossId;
            var adventureBossId = TableSheets.AdventureBossSheet.Values
                .First(row => row.BossId == bossId).Id;
            Assert.Equal(BoostTestAdventureBossId, adventureBossId);
            return (Combine(Granted(state), before, -1), explorer.Score, adventureBossId);
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
