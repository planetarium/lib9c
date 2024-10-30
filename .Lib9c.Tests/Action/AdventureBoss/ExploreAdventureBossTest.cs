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
    using Xunit;

    public class ExploreAdventureBossTest
    {
        private static readonly Dictionary<string, string> Sheets =
            TableSheetsImporter.ImportSheets();

        private static readonly TableSheets TableSheets = new TableSheets(Sheets);
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
        private static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

        // Wanted
        private static readonly Address WantedAddress = new PrivateKey().Address;

        private static readonly Address WantedAvatarAddress =
            Addresses.GetAvatarAddress(WantedAddress, 0);

        private static readonly AvatarState WantedAvatarState = new (
            WantedAvatarAddress, WantedAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "wanted"
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

        private static readonly AvatarState TesterAvatarState = new (
            TesterAvatarAddress, TesterAddress, 0L, TableSheets.GetAvatarSheets(),
            new PrivateKey().Address, name: "Tester"
        ) { level = 500 };

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

        public ExploreAdventureBossTest()
        {
            var collectionSheet = TableSheets.CollectionSheet;
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
                0, 5, 0, 0, 0, null, new (int, int)[] { }, new (int, int)[] { },
            };
            // Start from bottom, goes to 5
            yield return new object[]
            {
                0, 5, 5, 20, 10, null, // 2 potions per floor
                new[]
                {
                    (600301, 10), // 10 floor reward
                    (600302, 30), // 10+5+5+5+5 first reward
                    (600303, 4), // 2+2 first reward
                    (600304, 0),
                },
                new[]
                {
                    (10033, 5), // 5 first reward
                    (10034, 8), // 5 first reward + 3 floor reward
                },
            };
            // Start from bottom, goes to 3 because of potion
            yield return new object[]
            {
                0, 5, 3, 6, 0, null, new[]
                {
                    (600301, 7), // 7 floor reward
                    (600302, 20), // 10+5+5 first reward
                    (600303, 4), // 2+2 first reward
                    (600304, 0),
                },
                new[]
                {
                    (10033, 0),
                    (10034, 0),
                },
            };
            // Start from 3, goes to 5 because of locked floor
            yield return new object[]
            {
                2, 5, 5, 10, 4, null, new[]
                {
                    (600301, 4), // 4 floor reward
                    (600302, 15), // 5+5+5 first reward
                    (600303, 2), // 2 first reward
                    (600304, 0),
                },
                new[]
                {
                    (10033, 8), // 5 first reward + 3 floor reward
                    (10034, 5), // 5 first reward
                },
            };
            // Start from 6, goes to 9
            yield return new object[]
            {
                5, 10, 9, 20, 10, null,
                new[]
                {
                    (600203, 5), // 5 first reward
                    (600301, 0),
                    (600302, 5), // 5 floor reward
                    (600303, 15), // 5+5+5 first reward
                    (600304, 0),
                },
                new[]
                {
                    (10033, 8), // 5 first reward + 3 floor reward
                    (10034, 3), // 3 floor reward
                },
            };
            // Start from 20, cannot enter
            yield return new object[]
            {
                20, 20, 20, 10, 10, typeof(InvalidOperationException), null, null,
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
            Type exc,
            (int, int)[] expectedItemRewards,
            (int, int)[] expectedFavRewards
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

            var validatorKey = new PrivateKey();
            state = DelegationUtil.EnsureValidatorPromotionReady(state, validatorKey, 0L);
            state = DelegationUtil.MakeGuild(state, WantedAddress, validatorKey, 0L);

            state = Stake(state, WantedAddress);
            var sheets = state.GetSheets(sheetTypes: new[]
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
            }.Execute(new ActionContext
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

            var previousAvatarState = _initialState.GetAvatarState(TesterAvatarAddress);
            var equipments = Doomfist.GetAllParts(TableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var action = new ExploreAdventureBoss
            {
                Season = 1,
                AvatarAddress = TesterAvatarAddress,
                Costumes = new List<Guid>(),
                Equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };

            if (exc is not null)
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = TesterAddress,
                        BlockIndex = 1L,
                    }
                ));
            }
            else
            {
                state = action.Execute(new ActionContext
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
                foreach (var (id, amount) in expectedItemRewards)
                {
                    if (amount == 0)
                    {
                        Assert.Null(inventory.Items.FirstOrDefault(i => i.item.Id == id));
                    }
                    else
                    {
                        Assert.Equal(amount, inventory.Items.First(i => i.item.Id == id).count);
                    }
                }

                var runeSheet = sheets.GetSheet<RuneSheet>();
                foreach (var (id, amount) in expectedFavRewards)
                {
                    var ticker = runeSheet.Values.First(rune => rune.Id == id).Ticker;
                    var currency = Currencies.GetRune(ticker);
                    Assert.Equal(amount, state.GetBalance(TesterAvatarAddress, currency).RawValue);
                }

                itemSlotState =
                    state.TryGetLegacyState(itemSlotStateAddress, out rawItemSlotState)
                        ? new ItemSlotState(rawItemSlotState)
                        : new ItemSlotState(BattleType.Adventure);
                Assert.True(itemSlotState.Equipments.Count > 0);
            }
        }

        private IWorld Stake(IWorld world, Address agentAddress)
        {
            var action = new Stake(new BigInteger(500_000));
            var state = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                BlockIndex = 0L,
            });
            return state;
        }
    }
}
