namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RuneEnhancementTest
    {
        private readonly Currency _goldCurrency;

        public RuneEnhancementTest()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        [Theory]
        [InlineData(10000)]
        [InlineData(1)]
        public void Execute(int seed)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
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
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentStateV2(state, agentAddress, agentState);
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);
            state = LegacyModule.SetState(
                state,
                inventoryAddress,
                avatarState.inventory.Serialize());
            state = LegacyModule.SetState(
                state,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            state = LegacyModule.SetState(
                state,
                questListAddress,
                avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(
                    state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(state);
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = LegacyModule.SetState(state, runeStateAddress, runeState.Serialize());

            var costSheet = LegacyModule.GetSheet<RuneCostSheet>(state);
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level + 1, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = LegacyModule.GetSheet<RuneSheet>(state);
            if (!runeSheet.TryGetValue(runeId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = LegacyModule.GetGoldCurrency(state);
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            var ncgBal = cost.NcgQuantity * ncgCurrency * 10000;
            var crystalBal = cost.CrystalQuantity * crystalCurrency * 10000;
            var runeBal = cost.RuneStoneQuantity * runeCurrency * 10000;

            var rand = new TestRandom(seed);
            if (!RuneHelper.TryEnhancement(ncgBal, crystalBal, runeBal, ncgCurrency, crystalCurrency, runeCurrency, cost, rand, 99, out var tryCount))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            if (ncgBal.Sign > 0)
            {
                state = LegacyModule.MintAsset(state, context, agentAddress, ncgBal);
            }

            if (crystalBal.Sign > 0)
            {
                state = LegacyModule.MintAsset(state, context, agentAddress, crystalBal);
            }

            if (runeBal.Sign > 0)
            {
                state = LegacyModule.MintAsset(state, context, avatarState.address, runeBal);
            }

            var action = new RuneEnhancement()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
                TryCount = tryCount,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                Random = rand,
                Rehearsal = false,
                Signer = agentAddress,
            };

            var nextWorld = action.Execute(ctx);
            if (!LegacyModule.TryGetState(nextWorld, runeStateAddress, out List nextRuneRawState))
            {
                throw new Exception();
            }

            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

            var nextRunState = new RuneState(nextRuneRawState);
            var nextNcgBal = nextAccount.GetBalance(agentAddress, ncgCurrency);
            var nextCrystalBal = nextAccount.GetBalance(agentAddress, crystalCurrency);
            var nextRuneBal = nextAccount.GetBalance(agentAddress, runeCurrency);

            if (cost.NcgQuantity != 0)
            {
                Assert.NotEqual(ncgBal, nextNcgBal);
            }

            if (cost.CrystalQuantity != 0)
            {
                Assert.NotEqual(crystalBal, nextCrystalBal);
            }

            if (cost.RuneStoneQuantity != 0)
            {
                Assert.NotEqual(runeBal, nextRuneBal);
            }

            var costNcg = tryCount * cost.NcgQuantity * ncgCurrency;
            var costCrystal = tryCount * cost.CrystalQuantity * crystalCurrency;
            var costRune = tryCount * cost.RuneStoneQuantity * runeCurrency;

            if (costNcg.Sign > 0)
            {
                nextAccount = nextAccount.MintAsset(context, agentAddress, costNcg);
            }

            if (costCrystal.Sign > 0)
            {
                nextAccount = nextAccount.MintAsset(context, agentAddress, costCrystal);
            }

            if (costRune.Sign > 0)
            {
                nextAccount = nextAccount.MintAsset(context, avatarState.address, costRune);
            }

            var finalNcgBal = nextAccount.GetBalance(agentAddress, ncgCurrency);
            var finalCrystalBal = nextAccount.GetBalance(agentAddress, crystalCurrency);
            var finalRuneBal = nextAccount.GetBalance(avatarState.address, runeCurrency);
            Assert.Equal(ncgBal, finalNcgBal);
            Assert.Equal(crystalBal, finalCrystalBal);
            Assert.Equal(runeBal, finalRuneBal);
            Assert.Equal(runeState.Level + 1, nextRunState.Level);
        }

        [Fact]
        public void Execute_RuneCostNotFoundException()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
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
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentStateV2(state, agentAddress, agentState);
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);
            state = LegacyModule.SetState(
                state,
                inventoryAddress,
                avatarState.inventory.Serialize());
            state = LegacyModule.SetState(
                state,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            state = LegacyModule.SetState(
                state,
                questListAddress,
                avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(state, Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var world = new MockWorld(state);

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(world);
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(128381293);
            state = LegacyModule.SetState(state, runeStateAddress, runeState.Serialize());
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
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneCostDataNotFoundException()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
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
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentStateV2(state, agentAddress, agentState);
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);
            state = LegacyModule.SetState(
                state,
                inventoryAddress,
                avatarState.inventory.Serialize());
            state = LegacyModule.SetState(
                state,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            state = LegacyModule.SetState(
                state,
                questListAddress,
                avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(
                    state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var world = new MockWorld(state);

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(world);
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            var costSheet = LegacyModule.GetSheet<RuneCostSheet>(world);
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            for (var i = 0; i < costRow.Cost.Count + 1; i++)
            {
                runeState.LevelUp();
            }

            state = LegacyModule.SetState(state, runeStateAddress, runeState.Serialize());

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
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        public void Execute_NotEnoughFungibleAssetValueException(bool ncg, bool crystal, bool rune)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
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
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentStateV2(state, agentAddress, agentState);
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);
            state = LegacyModule.SetState(
                state,
                inventoryAddress,
                avatarState.inventory.Serialize());
            state = LegacyModule.SetState(
                state,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            state = LegacyModule.SetState(
                state,
                questListAddress,
                avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(
                    state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(state);
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = LegacyModule.SetState(state, runeStateAddress, runeState.Serialize());

            var costSheet = LegacyModule.GetSheet<RuneCostSheet>(state);
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level + 1, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = LegacyModule.GetSheet<RuneSheet>(state);
            if (!runeSheet.TryGetValue(runeId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = LegacyModule.GetGoldCurrency(state);
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            if (ncg && cost.NcgQuantity > 0)
            {
                state = LegacyModule.MintAsset(state, context, agentAddress, cost.NcgQuantity * ncgCurrency);
            }

            if (crystal && cost.CrystalQuantity > 0)
            {
                state = LegacyModule.MintAsset(state, context, agentAddress, cost.CrystalQuantity * crystalCurrency);
            }

            if (rune)
            {
                state = LegacyModule.MintAsset(state, context, avatarState.address, cost.RuneStoneQuantity * runeCurrency);
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
                Random = new TestRandom(0),
                Rehearsal = false,
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
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_TryCountIsZeroException()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
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
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentStateV2(state, agentAddress, agentState);
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);
            state = LegacyModule.SetState(
                state,
                inventoryAddress,
                avatarState.inventory.Serialize());
            state = LegacyModule.SetState(
                state,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            state = LegacyModule.SetState(
                state,
                questListAddress,
                avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(state, Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(state);
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = LegacyModule.SetState(state, runeStateAddress, runeState.Serialize());

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
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = LegacyModule.SetState(
                new MockWorld(),
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            state = AgentModule.SetAgentState(state, agentAddress, new AgentState(agentAddress));

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(
                    state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            state = AvatarModule.SetAvatarStateV2(state, avatarAddress, avatarState);

            var runeListSheet = LegacyModule.GetSheet<RuneListSheet>(state);
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
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }
    }
}
