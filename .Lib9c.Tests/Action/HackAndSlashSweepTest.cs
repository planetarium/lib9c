namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class HackAndSlashSweepTest
    {
        private const int ExtendedWorldId = 10;
        private const int ExtendedStageIdOffset = 450;
        private const int ExtendedEntryMaterialId = 900001;

        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IWorld _initialState;
        private readonly IRandom _random;

        public HackAndSlashSweepTest()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _rankingMapAddress
            );
            _avatarState.level = 100;

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _weeklyArenaState = new WeeklyArenaState(0);
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax);

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            foreach (var address in _avatarState.combinationSlotAddresses)
            {
                var slotState = new CombinationSlotState(
                    address,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                _initialState = _initialState.SetLegacyState(address, slotState.Serialize());
            }
        }

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level).ToList();
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment, iLock: null);
            }

            var equipmentGuids = equipments.Select(e => e.NonFungibleId).ToList();
            var random = new TestRandom();
            var costumes = new List<Guid>();
            if (avatarState.level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId],
                    random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipmentGuids, costumes);
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 1,
            };

            IWorld state = new World(MockUtil.MockModernWorldState);

            Assert.Throws<FailedLoadStateException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(100, 1)]
        public void Execute_SheetRowNotFoundException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            var state = _initialState.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowNotFoundException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(1, 999)]
        [InlineData(2, 50)]
        public void Execute_SheetRowColumnException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            var state = _initialState.SetLegacyState(
                _avatarAddress.Derive("world_ids"),
                List.Empty.Add(worldId.Serialize())
            );

            Assert.Throws<SheetRowColumnException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(1, 48, 1, 50)]
        [InlineData(1, 49, 2, 51)]
        public void Execute_InvalidStageException(int clearedWorldId, int clearedStageId, int worldId, int stageId)
        {
            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };
            var worldSheet = _initialState.GetSheet<WorldSheet>();
            var worldUnlockSheet = _initialState.GetSheet<WorldUnlockSheet>();

            _avatarState.worldInformation.ClearStage(clearedWorldId, clearedStageId, 1, worldSheet, worldUnlockSheet);

            var state = _initialState
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                )
                .SetAvatarState(_avatarAddress, _avatarState);

            Assert.Throws<InvalidStageException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_ExtendedWorld_CanPlayWithoutEntryCost()
        {
            const int normalFinalWorldId = 9;
            const int normalFinalStageId = 450;
            const int extendedStageId = ExtendedStageIdOffset + 1; // 451 (base stage 1)
            const int actionPointToSpend = 10; // base stage 1 costAP=5 => 2 plays

            var state = _initialState;
            var avatarState = state.GetAvatarState(_avatarAddress);

            // Prepare WorldInformation: normal cleared up to final stage and trigger hard world unlock.
            var worldSheet = state.GetSheet<WorldSheet>();
            var worldUnlockSheet = state.GetSheet<WorldUnlockSheet>();
            avatarState.worldInformation = new WorldInformation(0, worldSheet, normalFinalStageId);
            avatarState.worldInformation.ClearStage(
                normalFinalWorldId,
                normalFinalStageId,
                1,
                worldSheet,
                worldUnlockSheet);

            var (equipments, costumes) = GetDummyItems(avatarState);

            state = state
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(normalFinalWorldId.Serialize()));
            state = AvatarCpBooster.Apply(state, _sheets, _avatarAddress);

            // Stage 451 (extended) can have an entry material cost cloned from base stage 1.
            // HackAndSlashSweep validates that the declared entry cost matches the expected cost (per play count).
            var stageRow = state.GetSheet<StageSheet>()[extendedStageId];
            var expectedPlayCount = stageRow.CostAP > 0 ? actionPointToSpend / stageRow.CostAP : 0;
            var expectedEntryCostItemCount = stageRow.EntryCostItemId > 0
                ? checked(stageRow.EntryCostItemCount * expectedPlayCount)
                : 0;

            if (stageRow.EntryCostItemId > 0 && expectedEntryCostItemCount > 0)
            {
                var materialItemSheet = state.GetSheet<MaterialItemSheet>();
                var entryCostRow = materialItemSheet[stageRow.EntryCostItemId];
                var entryCostItem = ItemFactory.CreateTradableMaterial(entryCostRow);
                avatarState.inventory.AddItem(entryCostItem, expectedEntryCostItemCount);
                state = state.SetAvatarState(_avatarAddress, avatarState);
            }

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 0,
                actionPoint = actionPointToSpend,
                avatarAddress = _avatarAddress,
                worldId = ExtendedWorldId,
                stageId = extendedStageId,
                equipments = equipments,
                costumes = costumes,
                entryCostItemId = stageRow.EntryCostItemId,
                entryCostItemCount = expectedEntryCostItemCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = 1,
                });

            // Lazy migration: world 10 should be synced to legacy `world_ids`.
            Assert.True(nextState.TryGetLegacyState(_avatarAddress.Derive("world_ids"), out List rawIds));
            var unlockedWorldIds = rawIds.ToList(StateExtensions.ToInteger);
            Assert.Contains(ExtendedWorldId, unlockedWorldIds);

            // WorldInformation should also contain world 10 as unlocked.
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.TryGetWorld(ExtendedWorldId, out var extendedWorld));
            Assert.True(extendedWorld.IsUnlocked);
        }

        [Fact]
        public void Execute_StageFavReward_MintsFavByPlayCount()
        {
            const int normalFinalWorldId = 9;
            const int normalFinalStageId = 450;
            const int extendedStageId = ExtendedStageIdOffset + 1; // 451 (base stage 1)
            const int actionPointToSpend = 30; // enough AP for at least one full sweep play of stage 451
            const string favTicker = "CRYSTAL";
            const int favAmountPerPlay = 1000;

            // Build a modified StageSheet where base stage 1 has a FAV reward.
            // Stage 451 is used for the extended world stage in this test, so patch that row directly.
            var lines = _sheets[nameof(StageSheet)].Split('\n').ToList();
            var header = lines[0].TrimEnd('\r').Split(',').ToList();
            var ticker1Index = header.IndexOf("fungible_asset_reward_ticker_1");
            var ratio1Index = header.IndexOf("fungible_asset_reward_ratio_1");
            var min1Index = header.IndexOf("fungible_asset_reward_min_1");
            var max1Index = header.IndexOf("fungible_asset_reward_max_1");
            var favDropMinIndex = header.IndexOf("fav_drop_min");
            var favDropMaxIndex = header.IndexOf("fav_drop_max");
            for (var i = 1; i < lines.Count; i++)
            {
                var trimmed = lines[i].TrimEnd('\r');
                var comma = trimmed.IndexOf(',');
                if (comma >= 0 && trimmed.Substring(0, comma) == extendedStageId.ToString())
                {
                    var cols = trimmed.Split(',').ToList();
                    while (cols.Count < header.Count)
                    {
                        cols.Add(string.Empty);
                    }

                    // Force a single deterministic FAV reward per play.
                    if (ticker1Index >= 0)
                    {
                        cols[ticker1Index] = favTicker;
                    }

                    if (ratio1Index >= 0)
                    {
                        cols[ratio1Index] = "100";
                    }

                    if (min1Index >= 0)
                    {
                        cols[min1Index] = favAmountPerPlay.ToString();
                    }

                    if (max1Index >= 0)
                    {
                        cols[max1Index] = favAmountPerPlay.ToString();
                    }

                    // Clear other FAV reward entries so selection is deterministic.
                    for (var favIndex = 2; favIndex <= 5; favIndex++)
                    {
                        var t = header.IndexOf($"fungible_asset_reward_ticker_{favIndex}");
                        var r = header.IndexOf($"fungible_asset_reward_ratio_{favIndex}");
                        var mn = header.IndexOf($"fungible_asset_reward_min_{favIndex}");
                        var mx = header.IndexOf($"fungible_asset_reward_max_{favIndex}");
                        if (t >= 0)
                        {
                            cols[t] = string.Empty;
                        }

                        if (r >= 0)
                        {
                            cols[r] = string.Empty;
                        }

                        if (mn >= 0)
                        {
                            cols[mn] = string.Empty;
                        }

                        if (mx >= 0)
                        {
                            cols[mx] = string.Empty;
                        }
                    }

                    // Exactly one draw from the FAV pool per play.
                    if (favDropMinIndex >= 0)
                    {
                        cols[favDropMinIndex] = "1";
                    }

                    if (favDropMaxIndex >= 0)
                    {
                        cols[favDropMaxIndex] = "1";
                    }

                    lines[i] = string.Join(",", cols);
                    break;
                }
            }

            var state = _initialState.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(StageSheet)),
                string.Join('\n', lines).Serialize());

            var avatarState = state.GetAvatarState(_avatarAddress);

            var worldSheet = state.GetSheet<WorldSheet>();
            var worldUnlockSheet = state.GetSheet<WorldUnlockSheet>();
            avatarState.worldInformation = new WorldInformation(0, worldSheet, normalFinalStageId);
            avatarState.worldInformation.ClearStage(
                normalFinalWorldId,
                normalFinalStageId,
                1,
                worldSheet,
                worldUnlockSheet);

            var (equipments, costumes) = GetDummyItems(avatarState);

            state = state
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(normalFinalWorldId.Serialize()));
            state = AvatarCpBooster.Apply(state, _sheets, _avatarAddress);

            var stageRow = state.GetSheet<StageSheet>()[extendedStageId];
            var expectedPlayCount = stageRow.CostAP > 0 ? actionPointToSpend / stageRow.CostAP : 0;
            var expectedEntryCostItemCount = stageRow.EntryCostItemId > 0
                ? checked(stageRow.EntryCostItemCount * expectedPlayCount)
                : 0;

            if (stageRow.EntryCostItemId > 0 && expectedEntryCostItemCount > 0)
            {
                var materialItemSheet = state.GetSheet<MaterialItemSheet>();
                var entryCostRow = materialItemSheet[stageRow.EntryCostItemId];
                var entryCostItem = ItemFactory.CreateTradableMaterial(entryCostRow);
                avatarState.inventory.AddItem(entryCostItem, expectedEntryCostItemCount);
                state = state.SetAvatarState(_avatarAddress, avatarState);
            }

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 0,
                actionPoint = actionPointToSpend,
                avatarAddress = _avatarAddress,
                worldId = ExtendedWorldId,
                stageId = extendedStageId,
                equipments = equipments,
                costumes = costumes,
                entryCostItemId = stageRow.EntryCostItemId,
                entryCostItemCount = expectedEntryCostItemCount,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = 1,
                });

            var favCurrency = Currencies.GetCurrencyByTicker(favTicker);
            var recipient = Currencies.PickAddress(favCurrency, _agentAddress, _avatarAddress);
            var expectedBalance = favCurrency * checked(favAmountPerPlay * expectedPlayCount);
            Assert.Equal(expectedBalance, nextState.GetBalance(recipient, favCurrency));
        }

        [Theory]
        [InlineData(0, 2)] // entryCostItemId not set (0)
        [InlineData(900001, 0)] // entryCostItemCount not set (0)
        [InlineData(900001, 1)] // entryCostItemCount off by one (expect 2, got 1)
        [InlineData(1, 2)] // wrong entryCostItemId
        public void Execute_ExtendedWorld_ThrowsInvalidActionField_WhenHardMaterialMismatch(
            int entryCostItemId, int entryCostItemCount)
        {
            const int normalFinalWorldId = 9;
            const int normalFinalStageId = 450;
            const int extendedStageId = ExtendedStageIdOffset + 1;
            const int actionPointToSpend = 10; // base stage 1 costAP=5 => 2 plays

            var state = _initialState;
            var avatarState = state.GetAvatarState(_avatarAddress);

            var worldSheet = state.GetSheet<WorldSheet>();
            var worldUnlockSheet = state.GetSheet<WorldUnlockSheet>();
            avatarState.worldInformation = new WorldInformation(0, worldSheet, normalFinalStageId);
            avatarState.worldInformation.ClearStage(
                normalFinalWorldId, normalFinalStageId, 1, worldSheet, worldUnlockSheet);

            var (equipments, costumes) = GetDummyItems(avatarState);

            state = state
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(normalFinalWorldId.Serialize()));
            state = AvatarCpBooster.Apply(state, _sheets, _avatarAddress);

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 0,
                actionPoint = actionPointToSpend,
                avatarAddress = _avatarAddress,
                worldId = ExtendedWorldId,
                stageId = extendedStageId,
                equipments = equipments,
                costumes = costumes,
                entryCostItemId = entryCostItemId,
                entryCostItemCount = entryCostItemCount,
            };

            Assert.Throws<InvalidActionFieldException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                        BlockIndex = 1,
                    }));
        }

        [Theory]
        [InlineData(GameConfig.MimisbrunnrWorldId, 10000001, false)]
        [InlineData(GameConfig.MimisbrunnrWorldId, 10000001, true)]
        // Unlock CRYSTAL first.
        [InlineData(2, 51, false)]
        public void Execute_InvalidWorldException(int worldId, int stageId, bool unlockedIdsExist)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 10000001);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            if (unlockedIdsExist)
            {
                state = state.SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    List.Empty.Add(worldId.Serialize())
                );
            }

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            Assert.Throws<InvalidWorldException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_UsageLimitExceedException()
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = HackAndSlashSweep.UsableApStoneCount + 10,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 2,
            };

            Assert.Throws<UsageLimitExceedException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(3, 2)]
        [InlineData(7, 5)]
        public void Execute_NotEnoughMaterialException(int useApStoneCount, int holdingApStoneCount)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, holdingApStoneCount);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * useApStoneCount;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                state = state.SetAvatarState(_avatarAddress, avatarState);

                var action = new HackAndSlashSweep
                {
                    equipments = equipments,
                    costumes = costumes,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = useApStoneCount,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughMaterialException>(
                    () => action.Execute(
                        new ActionContext()
                        {
                            PreviousState = state,
                            Signer = _agentAddress,
                            RandomSeed = 0,
                        }));
            }
        }

        [Fact]
        public void Execute_NotEnoughActionPointException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                state = state.SetAvatarState(_avatarAddress, avatarState);

                var action = new HackAndSlashSweep
                {
                    runeInfos = new List<RuneSlotInfo>(),
                    costumes = costumes,
                    equipments = equipments,
                    avatarAddress = _avatarAddress,
                    actionPoint = 999999,
                    apStoneCount = 0,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughActionPointException>(
                    () =>
                        action.Execute(
                            new ActionContext()
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                RandomSeed = 0,
                            }));
            }
        }

        [Fact]
        public void Execute_PlayCountIsZeroException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = state.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                state = state.SetAvatarState(_avatarAddress, avatarState);
                var action = new HackAndSlashSweep
                {
                    costumes = costumes,
                    equipments = equipments,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = 0,
                    apStoneCount = 0,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<PlayCountIsZeroException>(
                    () =>
                        action.Execute(
                            new ActionContext()
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                RandomSeed = 0,
                            }));
            }
        }

        [Fact]
        public void Execute_NotEnoughCombatPointException()
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 1;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = state.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            var stageId = 24;
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    (int)playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = 1,
                    stageId = stageId,
                };

                Assert.Throws<NotEnoughCombatPointException>(
                    () =>
                        action.Execute(
                            new ActionContext()
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                RandomSeed = 0,
                            }));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void ExecuteWithStake(int stakingLevel)
        {
            const int worldId = 1;
            const int stageId = 1;
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 3;

            var itemRow = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = LegacyStakeState.DeriveAddress(_agentAddress);
            var stakeState = new LegacyStakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == stakingLevel)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var actionPoint = _initialState.GetActionPoint(_avatarAddress);
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    (int)playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                var nextState = action.Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    });
                var nextAvatar = nextState.GetAvatarState(_avatarAddress);

                var nextCpAccount = nextState.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Adventure));
                var nextCpState = new CpState(nextCpAccount.GetState(_avatarAddress));

                Assert.True(nextCpState.Cp > 0);
                Assert.Equal(expectedLevel, nextAvatar.level);
                Assert.Equal(expectedExp, nextAvatar.exp);
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }
        }

        [Theory]
        [InlineData(1, 15)]
        [InlineData(2, 55)]
        [InlineData(3, 111)]
        [InlineData(4, 189)]
        [InlineData(4, 200)]
        [InlineData(5, 250)]
        [InlineData(6, 300)]
        public void CheckRewardItems(int worldId, int stageId)
        {
            const int apStoneCount = 10;
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.level = 400;
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), stageId);

            var materialSheet = _initialState.GetSheet<MaterialItemSheet>();
            var itemRow = materialSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone, apStoneCount);

            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    Enumerable.Range(1, worldId).ToList().Select(i => i.Serialize()).Serialize())
                .SetActionPoint(_avatarAddress, 120);
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (!stageSheet.TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }

            var actionPoint = _initialState.GetActionPoint(_avatarAddress);
            var action = new HackAndSlashSweep
            {
                costumes = new List<Guid>(),
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
                actionPoint = (int)actionPoint,
                apStoneCount = apStoneCount,
                worldId = worldId,
                stageId = stageId,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });
            var nextAvatar = nextState.GetAvatarState(_avatarAddress);

            var circleRow = materialSheet.Values.First(i => i.ItemSubType == ItemSubType.Circle);
            var circleRewardData = stageRow.Rewards.FirstOrDefault(reward => reward.ItemId == circleRow.Id);
            if (circleRewardData != null)
            {
                var circles = nextAvatar.inventory.Items.Where(x => x.item.Id == circleRow.Id);
                Assert.All(circles, x => Assert.True(x.item is TradableMaterial));
            }
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            var stakingLevel = 1;
            const int worldId = 1;
            const int stageId = 1;
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 3;

            var itemRow = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = LegacyStakeState.DeriveAddress(_agentAddress);
            var stakeState = new LegacyStakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == stakingLevel)?.RequiredGold ?? 0;
            var context = new ActionContext();
            var state = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(stakeStateAddress, stakeState.Serialize())
                .MintAsset(context, stakeStateAddress, requiredGold * _initialState.GetGoldCurrency());
            var stageSheet = _initialState.GetSheet<StageSheet>();
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var apSheet = _initialState.GetSheet<StakeActionPointCoefficientSheet>();
                var costAp = apSheet.GetActionPointByStaking(stageRow.CostAP, 1, stakingLevel);
                var actionPoint = _initialState.GetActionPoint(_avatarAddress);
                var itemPlayCount =
                    DailyReward.ActionPointMax / costAp * 1;
                var apPlayCount = actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    (int)playCount);

                var ncgCurrency = state.GetGoldCurrency();
                state = state.MintAsset(context, _agentAddress, 99999 * ncgCurrency);

                var unlockRuneSlot = new UnlockRuneSlot()
                {
                    AvatarAddress = _avatarAddress,
                    SlotIndex = 1,
                };

                state = unlockRuneSlot.Execute(
                    new ActionContext
                    {
                        BlockIndex = 1,
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    });

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>()
                    {
                        new (slotIndex, runeId),
                        new (slotIndex2, runeId2),
                    },
                    avatarAddress = _avatarAddress,
                    actionPoint = (int)actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws(
                    exception,
                    () => action.Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Signer = _agentAddress,
                            RandomSeed = 0,
                        }));
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, int.MinValue + 1)]
        [InlineData(-1, 0)]
        [InlineData(int.MinValue + 1, 0)]
        public void Execute_ArgumentOutOfRangeException(int ap, int apPotion)
        {
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.worldInformation =
                new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25);
            avatarState.level = 400;

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, 0);
            var actionPoint = _initialState.GetActionPoint(_avatarAddress);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    DailyReward.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    (int)playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
                var action = new HackAndSlashSweep
                {
                    runeInfos = new List<RuneSlotInfo>(),
                    costumes = costumes,
                    equipments = equipments,
                    avatarAddress = _avatarAddress,
                    actionPoint = ap,
                    apStoneCount = apPotion,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<ArgumentOutOfRangeException>(
                    () =>
                        action.Execute(
                            new ActionContext()
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                RandomSeed = 0,
                            }));
            }
        }

        [Theory]
        [InlineData(150, true)] // inside [100, 200)
        [InlineData(250, false)] // after the schedule ends
        public void Execute_AppliesScheduledStageRewardBoost(long blockIndex, bool scheduled)
        {
            const int stageId = 1;
            var (state, prevMaterials) = PrepareStageRewardBoost(stageId);

            var plain = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithoutBoostSchedule(state),
                stageId,
                blockIndex);
            var patched = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithBoostSchedule(state, StageRewardBoostFixture.BoostCsv(stageId)),
                stageId,
                blockIndex);

            Assert.Equal(plain.Exp, patched.Exp);
            Assert.True(plain.Fav.MajorUnit >= 2000, "the sweep should cover several clears");
            if (!scheduled)
            {
                Assert.Equal(plain.Inventory, patched.Inventory);
                Assert.Equal(plain.Fav, patched.Fav);
                return;
            }

            // Every clear of the sweep is doubled, exactly as HackAndSlash doubles each clear.
            Assert.Equal(plain.Fav * 2, patched.Fav);
            var gained = 0;
            foreach (var id in plain.Materials.Keys.Union(patched.Materials.Keys))
            {
                prevMaterials.TryGetValue(id, out var prev);
                plain.Materials.TryGetValue(id, out var plainCount);
                patched.Materials.TryGetValue(id, out var patchedCount);
                Assert.Equal(2 * (plainCount - prev), patchedCount - prev);
                gained += plainCount - prev;
            }

            Assert.True(gained > 0, "the stage should reward items");
        }

        [Fact]
        public void Execute_IsUnchangedWhenTheChainWasNeverPatchedWithBoostScheduleSheet()
        {
            const int stageId = 1;
            const long blockIndex = 150;
            var (state, _) = PrepareStageRewardBoost(stageId);

            var unpatched = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithoutBoostSchedule(state),
                stageId,
                blockIndex);
            var empty = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithBoostSchedule(state, StageRewardBoostFixture.EmptyCsv),
                stageId,
                blockIndex);
            var unmatched = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithBoostSchedule(state, StageRewardBoostFixture.UnmatchedCsv(stageId)),
                stageId,
                blockIndex);

            foreach (var other in new[] { empty, unmatched })
            {
                Assert.Equal(unpatched.Inventory, other.Inventory);
                Assert.Equal(unpatched.Fav, other.Fav);
                Assert.Equal(unpatched.Exp, other.Exp);
            }
        }

        [Theory]
        [InlineData("ADD", "1")]
        [InlineData("MUL", "1.5")]
        public void Execute_AppliesStageRewardBoostPerClear(string op, string value)
        {
            // Unlike MUL 2, these differ between applying per clear and applying once to the
            // sweep's total, so they pin down that each clear is boosted on its own.
            const int stageId = 1;
            const long blockIndex = 150;
            var (state, prevMaterials) = PrepareStageRewardBoost(stageId);
            var csv = StageRewardBoostFixture.BoostCsv(stageId, op, value);
            var boost = new BoostScheduleSheet();
            boost.Set(csv);
            var itemBoost = boost[1];
            var favBoost = boost[2];

            var plain = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithoutBoostSchedule(state),
                stageId,
                blockIndex);
            var patched = ExecuteStageRewardBoost(
                StageRewardBoostFixture.WithBoostSchedule(state, csv),
                stageId,
                blockIndex);

            // Replay the sweep's draws (ActionContext.GetRandom is a fresh TestRandom(seed)) and
            // boost every clear independently of the code under test.
            var stageRow = state.GetSheet<StageSheet>()[stageId];
            var materialItemSheet = state.GetSheet<MaterialItemSheet>();
            var playCount = (int)(plain.Fav.MajorUnit / 1000);
            var random = new TestRandom(0);
            var maxCount = random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);
            var perClear = new Dictionary<int, int>();
            var drawnTotal = new Dictionary<int, int>();
            for (var i = 0; i < playCount; i++)
            {
                var selector = StageSimulatorV1.SetItemSelector(stageRow, random);
                var drawn = Simulator.SetRewardV2(selector, maxCount, random, materialItemSheet);
                foreach (var group in drawn.GroupBy(item => item.Id))
                {
                    perClear.TryGetValue(group.Key, out var boosted);
                    perClear[group.Key] = boosted + itemBoost.Apply(group.Count());
                    drawnTotal.TryGetValue(group.Key, out var total);
                    drawnTotal[group.Key] = total + group.Count();
                }
            }

            Assert.True(playCount > 1, "the sweep should cover several clears");
            Assert.Contains(perClear, pair => pair.Value != itemBoost.Apply(drawnTotal[pair.Key]));

            foreach (var id in perClear.Keys.Union(patched.Materials.Keys))
            {
                prevMaterials.TryGetValue(id, out var prev);
                patched.Materials.TryGetValue(id, out var patchedCount);
                perClear.TryGetValue(id, out var expected);
                Assert.Equal(expected, patchedCount - prev);
            }

            var favCurrency = Currencies.GetCurrencyByTicker(StageRewardBoostFixture.FavTicker);
            Assert.Equal(favCurrency * (favBoost.Apply(1000) * playCount), patched.Fav);

            // The client replays the sweep with GetRewardItems, which must add up to the grant.
            var replayed = HackAndSlashSweep.GetRewardItems(
                new TestRandom(0),
                playCount,
                stageRow,
                materialItemSheet,
                itemBoost);
            Assert.Equal(
                perClear.OrderBy(pair => pair.Key),
                replayed.GroupBy(item => item.Id)
                    .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()))
                    .OrderBy(pair => pair.Key));
        }

        [Fact]
        public void Execute_AddsBoostedExtrasLikeTheExpandedRewards()
        {
            // Stage 11 is rewritten to drop several ids per clear, a circle among them, each up to
            // twice per draw, so that MUL 2 counts extras for several ids across the sweep.
            const int stageId = 11;
            const int circleId = 600402;
            const int otherDropId = 306024;
            const long blockIndex = 150;
            var collectQuestCsv = _sheets[nameof(CollectQuestSheet)].TrimEnd('\n', '\r') +
                                  $"\n299998,1000000,22,{circleId}\n299999,1000000,22,{otherDropId}\n";
            var state = StageRewardBoostFixture.WithBoostSchedule(
                    _initialState,
                    StageRewardBoostFixture.BoostCsv(stageId, "MUL", "2"))
                .SetLegacyState(
                    Addresses.TableSheet.Derive(nameof(StageSheet)),
                    StageRewardBoostFixture.WithItemReward(_sheets[nameof(StageSheet)], stageId, circleId, 4, 6)
                        .Serialize())
                .SetLegacyState(
                    Addresses.TableSheet.Derive(nameof(CollectQuestSheet)),
                    collectQuestCsv.Serialize());
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                state.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.level = 3;
            avatarState.worldInformation = new WorldInformation(0, state.GetSheet<WorldSheet>(), 25);
            state = AvatarCpBooster.Apply(
                state.SetAvatarState(_avatarAddress, avatarState),
                _sheets,
                _avatarAddress);
            Assert.Contains(avatarState.questList.OfType<CollectQuest>(), q => q.ItemId == circleId);

            var actionPoint = (int)state.GetActionPoint(_avatarAddress);
            var nextState = new HackAndSlashSweep
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
                actionPoint = actionPoint,
                apStoneCount = 0,
                worldId = 1,
                stageId = stageId,
            }.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
                BlockIndex = blockIndex,
            });
            var actual = nextState.GetAvatarState(_avatarAddress);

            // Expected: the same sweep granted one object per unit, the way the client replays it,
            // through the plain UpdateInventory, on a copy of the avatar.
            var stageRow = state.GetSheet<StageSheet>()[stageId];
            var materialItemSheet = state.GetSheet<MaterialItemSheet>();
            var levelSheet = state.GetSheet<CharacterLevelSheet>();
            var boost = new BoostScheduleSheet();
            boost.Set(StageRewardBoostFixture.BoostCsv(stageId, "MUL", "2"));
            var playCount = actionPoint / stageRow.CostAP;
            var expanded = HackAndSlashSweep.GetRewardItems(
                new TestRandom(0),
                playCount,
                stageRow,
                materialItemSheet,
                boost[1]);
            var expected = state.GetAvatarState(_avatarAddress);
            expected.UpdateMonsterMap(state.GetSheet<StageWaveSheet>(), stageId);
            expected.UpdateInventory(expanded);
            for (var i = 0; i < playCount; i++)
            {
                var (level, exp) = expected.GetLevelAndExp(levelSheet, stageId, 1);
                expected.UpdateExp(level, exp);
            }

            Assert.True(expanded.Select(item => item.Id).Distinct().Count() > 1, "several ids");
            Assert.Contains(expanded, item => item.Id == circleId);
            Assert.Contains(expanded, item => item.Id == otherDropId);
            var redraw = new TestRandom(0);
            var maxCount = redraw.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);
            var repeatedInAClear = false;
            for (var i = 0; i < playCount; i++)
            {
                var selector = StageSimulatorV1.SetItemSelector(stageRow, redraw);
                var drawn = Simulator.SetRewardV2(selector, maxCount, redraw, materialItemSheet);
                repeatedInAClear |= drawn.GroupBy(item => item.Id).Any(group => group.Count() > 1);
            }

            Assert.True(repeatedInAClear, "an id should be drawn several times in one clear");

            // Per id and tradability rather than raw bytes: the action calls AddItem (and so
            // List.Sort, which is unstable) fewer times, so equal entries may sit in another order.
            Assert.Equal(Stacks(expected), Stacks(actual));
            Assert.All(
                actual.inventory.Items.Where(i => i.item.Id == circleId),
                i => Assert.IsType<TradableMaterial>(i.item));
            Assert.Equal(expected.questList.Serialize(), actual.questList.Serialize());
            var circleQuest = (Dictionary)actual.questList.OfType<CollectQuest>()
                .Single(q => q.ItemId == circleId)
                .Serialize();
            Assert.Equal(expanded.Count(item => item.Id == circleId), (int)(Integer)circleQuest["current"]);
        }

        [Fact]
        public void GetRewardItems_IsUnchangedWithoutARow()
        {
            var stageRow = _tableSheets.StageSheet[1];
            var before = HackAndSlashSweep.GetRewardItems(
                new TestRandom(11),
                10,
                stageRow,
                _tableSheets.MaterialItemSheet);
            var after = HackAndSlashSweep.GetRewardItems(
                new TestRandom(11),
                10,
                stageRow,
                _tableSheets.MaterialItemSheet,
                null);
            var boosted = HackAndSlashSweep.GetRewardItems(
                new TestRandom(11),
                10,
                stageRow,
                _tableSheets.MaterialItemSheet,
                StageRewardBoostFixture.Row(BoostScheduleSheet.Targets.StageItemReward, "MUL", 2));

            Assert.Equal(before.Select(item => item.Id), after.Select(item => item.Id));
            Assert.Equal(
                before.SelectMany(item => new[] { item.Id, item.Id }),
                boosted.Select(item => item.Id));
        }

        private static List<(int Id, bool Tradable, int Count)> Stacks(AvatarState avatarState) =>
            avatarState.inventory.Items
                .GroupBy(i => (i.item.Id, Tradable: i.item is ITradableItem))
                .Select(g => (g.Key.Id, g.Key.Tradable, g.Sum(i => i.count)))
                .OrderBy(t => t.Id)
                .ThenBy(t => t.Tradable)
                .ToList();

        private static Dictionary<int, int> CountMaterials(AvatarState avatarState) =>
            avatarState.inventory.Items
                .Where(i => i.item is Material)
                .GroupBy(i => i.item.Id)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.count));

        /// <summary>
        /// An avatar that can sweep <paramref name="stageId"/>, which grants 1000 crystal per
        /// clear.
        /// </summary>
        private (IWorld State, Dictionary<int, int> Materials) PrepareStageRewardBoost(int stageId)
        {
            var state = _initialState.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(StageSheet)),
                StageRewardBoostFixture.WithFavReward(_sheets[nameof(StageSheet)], stageId, 1000).Serialize());
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                state.GetAvatarSheets(),
                _rankingMapAddress);
            avatarState.level = 3;
            avatarState.worldInformation =
                new WorldInformation(0, state.GetSheet<WorldSheet>(), 25);
            state = state.SetAvatarState(_avatarAddress, avatarState);
            return (state, CountMaterials(avatarState));
        }

        private (IValue Inventory, Dictionary<int, int> Materials, FungibleAssetValue Fav, long Exp)
            ExecuteStageRewardBoost(IWorld state, int stageId, long blockIndex)
        {
            var action = new HackAndSlashSweep
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
                actionPoint = (int)state.GetActionPoint(_avatarAddress),
                apStoneCount = 0,
                worldId = 1,
                stageId = stageId,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var favCurrency = Currencies.GetCurrencyByTicker(StageRewardBoostFixture.FavTicker);
            var recipient = Currencies.PickAddress(favCurrency, _agentAddress, _avatarAddress);
            return (
                nextAvatarState.inventory.Serialize(),
                CountMaterials(nextAvatarState),
                nextState.GetBalance(recipient, favCurrency),
                nextAvatarState.exp);
        }
    }
}
