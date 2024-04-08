namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneEnhancementTest
    {
        private readonly Currency _goldCurrency;

        public RuneEnhancementTest()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        [Theory]
        // All success
        [InlineData(1, 1, 2, 0, 0, 1, null, 0)]
        [InlineData(1, 2, 3, 0, 0, 2, null, 0)]
        // Success 1 of 2
        [InlineData(202, 2, 203, 0, 1000, 40, null, 2)]
        // All fail
        [InlineData(202, 2, 202, 0, 1000, 40, null, 0)]
        // Reaching max level
        [InlineData(299, 1, 300, 0, 500, 20, null, 1)]
        // Cannot exceed max level
        [InlineData(299, 2, 299, 0, 0, 0, typeof(RuneCostDataNotFoundException), 0)]
        [InlineData(300, 1, 300, 0, 0, 0, typeof(RuneCostDataNotFoundException), 0)]
        public void Execute_LegacyState(
            int startLevel,
            int tryCount,
            int expectedLevel,
            int expectedNcgCost,
            int expectedCrystalCost,
            int expectedRuneCost,
            Type expectedException,
            int seed
        )
        {
            const int initialNcg = 10_000;
            const int initialCrystal = 1_000_000;
            const int initialRune = 1_000;
            var s = seed;
            // Set states
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var allRuneState = new AllRuneState(runeId);
            var runeState = allRuneState.GetRuneState(runeId);
            runeState.LevelUp(startLevel);
            // Set Legacy Rune state
            state = state.SetLegacyState(
                RuneState.DeriveAddress(avatarAddress, runeId),
                runeState.Serialize()
            );

            // Prepare materials
            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeTicker = tableSheets.RuneSheet.Values.First(r => r.Id == runeId).Ticker;
            var runeCurrency = Currency.Legacy(runeTicker, 0, minters: null);
            var r = new TestRandom(seed: 1);

            state = state.MintAsset(context, agentAddress, ncgCurrency * initialNcg);
            state = state.MintAsset(context, agentAddress, crystalCurrency * initialCrystal);
            state = state.MintAsset(context, avatarAddress, runeCurrency * initialRune);

            // Action
            var action = new RuneEnhancement
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = tryCount,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = seed,
                Signer = agentAddress,
            };

            if (expectedException is not null)
            {
                Assert.Throws(expectedException, () => { action.Execute(ctx); });
            }
            else
            {
                var nextState = action.Execute(ctx);
                // RuneState must be migrated to AllRuneState
                var nextAllRuneState = nextState.GetRuneState(avatarAddress);
                var nextRuneState = nextAllRuneState.GetRuneState(runeId);
                if (nextRuneState is null)
                {
                    throw new Exception();
                }

                var nextNcgBal = nextState.GetBalance(agentAddress, ncgCurrency);
                var nextCrystalBal = nextState.GetBalance(agentAddress, crystalCurrency);
                var nextRuneBal = nextState.GetBalance(avatarAddress, runeCurrency);

                Assert.Equal((initialNcg - expectedNcgCost) * ncgCurrency, nextNcgBal);
                Assert.Equal(
                    (initialCrystal - expectedCrystalCost) * crystalCurrency,
                    nextCrystalBal
                );
                Assert.Equal((initialRune - expectedRuneCost) * runeCurrency, nextRuneBal);
                Assert.Equal(expectedLevel, nextRuneState.Level);
            }
        }

        [Theory]
        // All success
        [InlineData(1, 1, 2, 0, 0, 1, null, 0)]
        [InlineData(1, 2, 3, 0, 0, 2, null, 0)]
        // Success 1 of 2
        [InlineData(202, 2, 203, 0, 1000, 40, null, 2)]
        // All fail
        [InlineData(202, 2, 202, 0, 1000, 40, null, 0)]
        // Reaching max level
        [InlineData(299, 1, 300, 0, 500, 20, null, 1)]
        // Cannot exceed max level
        [InlineData(299, 2, 299, 0, 0, 0, typeof(RuneCostDataNotFoundException), 0)]
        [InlineData(300, 1, 300, 0, 0, 0, typeof(RuneCostDataNotFoundException), 0)]
        public void Execute(
            int startLevel,
            int tryCount,
            int expectedLevel,
            int expectedNcgCost,
            int expectedCrystalCost,
            int expectedRuneCost,
            Type expectedException,
            int seed
        )
        {
            const int initialNcg = 10_000;
            const int initialCrystal = 1_000_000;
            const int initialRune = 1_000;
            var s = seed;
            // Set states
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var allRuneState = new AllRuneState(runeId);
            var runeState = allRuneState.GetRuneState(runeId);
            runeState.LevelUp(startLevel);
            state = state.SetRuneState(avatarAddress, allRuneState);

            // Prepare materials
            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeTicker = tableSheets.RuneSheet.Values.First(r => r.Id == runeId).Ticker;
            var runeCurrency = Currency.Legacy(runeTicker, 0, minters: null);
            var r = new TestRandom(seed: 1);

            state = state.MintAsset(context, agentAddress, ncgCurrency * initialNcg);
            state = state.MintAsset(context, agentAddress, crystalCurrency * initialCrystal);
            state = state.MintAsset(context, avatarAddress, runeCurrency * initialRune);

            // Action
            var action = new RuneEnhancement
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = tryCount,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = seed,
                Signer = agentAddress,
            };

            if (expectedException is not null)
            {
                Assert.Throws(expectedException, () => { action.Execute(ctx); });
            }
            else
            {
                var nextState = action.Execute(ctx);
                var nextAllRuneState = nextState.GetRuneState(avatarAddress);
                var nextRuneState = nextAllRuneState.GetRuneState(runeId);
                if (nextRuneState is null)
                {
                    throw new Exception();
                }

                var nextNcgBal = nextState.GetBalance(agentAddress, ncgCurrency);
                var nextCrystalBal = nextState.GetBalance(agentAddress, crystalCurrency);
                var nextRuneBal = nextState.GetBalance(avatarAddress, runeCurrency);

                Assert.Equal((initialNcg - expectedNcgCost) * ncgCurrency, nextNcgBal);
                Assert.Equal(
                    (initialCrystal - expectedCrystalCost) * crystalCurrency,
                    nextCrystalBal
                );
                Assert.Equal((initialRune - expectedRuneCost) * runeCurrency, nextRuneBal);
                Assert.Equal(expectedLevel, nextRuneState.Level);
            }
        }

        [Fact]
        public void Execute_RuneCostNotFoundException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            const int runeId = 128381293;
            var runeState = new AllRuneState(runeId);
            state = state.SetRuneState(avatarState.address, runeState);

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = 1,
            };

            Assert.Throws<RuneCostNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneCostDataNotFoundException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var allRuneState = new AllRuneState(runeId);
            var runeState = allRuneState.GetRuneState(runeId);
            Assert.NotNull(runeState);

            var costSheet = state.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            runeState.LevelUp(costRow.Cost.Count);
            allRuneState.SetRuneState(runeState);
            state = state.SetRuneState(avatarAddress, allRuneState);

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = 1,
            };

            Assert.Throws<RuneCostDataNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        public void Execute_NotEnoughFungibleAssetValueException(bool ncg, bool crystal, bool rune)
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var allRuneState = new AllRuneState(runeId);
            var runeState = allRuneState.GetRuneState(runeId);
            state = state.SetRuneState(runeStateAddress, allRuneState);

            var costSheet = state.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level + 1, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = state.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(runeId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            if (ncg && cost.NcgQuantity > 0)
            {
                state = state.MintAsset(context, agentAddress, cost.NcgQuantity * ncgCurrency);
            }

            if (crystal && cost.CrystalQuantity > 0)
            {
                state = state.MintAsset(context, agentAddress, cost.CrystalQuantity * crystalCurrency);
            }

            if (rune)
            {
                state = state.MintAsset(context, avatarState.address, cost.RuneStoneQuantity * runeCurrency);
            }

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = 1,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            if (!ncg && cost.NcgQuantity == 0)
            {
                return;
            }

            if (!crystal && cost.CrystalQuantity == 0)
            {
                return;
            }

            if (!rune && cost.RuneStoneQuantity == 0)
            {
                return;
            }

            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_TryCountIsZeroException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new AllRuneState(runeId);
            state = state.SetRuneState(runeStateAddress, runeState);

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = 0,
            };

            Assert.Throws<TryCountIsZeroException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetAgentState(agentAddress, new AgentState(agentAddress));

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            state = state.SetAvatarState(avatarAddress, avatarState, true, false, false, false);

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = 0,
            };

            Assert.Throws<FailedLoadStateException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }
    }
}
