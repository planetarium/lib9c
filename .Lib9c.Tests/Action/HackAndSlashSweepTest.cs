namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class HackAndSlashSweepTest
    {
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
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress
            )
            {
                level = 100,
            };
            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _weeklyArenaState = new WeeklyArenaState(0);
            _initialState = new World(new MockWorldState())
                .SetLegacyState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

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
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level)
                .Select(e => e.NonFungibleId).ToList();
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
                    _tableSheets.ItemSheet[costumeId], random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipments, costumes);
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

            IWorld state = new World(new MockWorldState());

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
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

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
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

            Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
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

            Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                RandomSeed = 0,
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 10000001),
            };

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

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

            Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
            };

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var action = new HackAndSlashSweep
            {
                runeInfos = new List<RuneSlotInfo>(),
                apStoneCount = 99,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 2,
            };

            Assert.Throws<UsageLimitExceedException>(() => action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
            };

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, holdingApStoneCount);

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * useApStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);

                var action = new HackAndSlashSweep
                {
                    equipments = equipments,
                    costumes = costumes,
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = useApStoneCount,
                    worldId = 1,
                    stageId = 2,
                };

                Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
                actionPoint = 0,
            };

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
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

                Assert.Throws<NotEnoughActionPointException>(() =>
                    action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                level = 400,
                actionPoint = 0,
            };

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(2, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    2,
                    playCount);

                var (equipments, costumes) = GetDummyItems(avatarState);
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

                Assert.Throws<PlayCountIsZeroException>(() =>
                    action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 0,
                level = 1,
            };

            IWorld state = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            int stageId = 24;
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = 1,
                    stageId = stageId,
                };

                Assert.Throws<NotEnoughCombatPointException>(() =>
                    action.Execute(new ActionContext()
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 120,
                level = 3,
            };
            var itemRow = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
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
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = avatarState.actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    playCount);

                var action = new HackAndSlashSweep
                {
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                var nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });
                var nextAvatar = nextState.GetAvatarState(_avatarAddress);
                Assert.Equal(expectedLevel, nextAvatar.level);
                Assert.Equal(expectedExp, nextAvatar.exp);
            }
            else
            {
                throw new SheetRowNotFoundException(nameof(StageSheet), stageId);
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
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 120,
                level = 3,
            };
            var itemRow = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(itemRow);
            avatarState.inventory.AddItem(apStone);

            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
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
                var itemPlayCount =
                    gameConfigState.ActionPointMax / costAp * 1;
                var apPlayCount = avatarState.actionPoint / costAp;
                var playCount = apPlayCount + itemPlayCount;
                var (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _initialState.GetSheet<CharacterLevelSheet>(),
                    stageId,
                    playCount);

                var ncgCurrency = state.GetGoldCurrency();
                state = state.MintAsset(context, _agentAddress, 99999 * ncgCurrency);

                var unlockRuneSlot = new UnlockRuneSlot()
                {
                    AvatarAddress = _avatarAddress,
                    SlotIndex = 1,
                };

                state = unlockRuneSlot.Execute(new ActionContext
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
                        new RuneSlotInfo(slotIndex, runeId),
                        new RuneSlotInfo(slotIndex2, runeId2),
                    },
                    avatarAddress = _avatarAddress,
                    actionPoint = avatarState.actionPoint,
                    apStoneCount = 1,
                    worldId = worldId,
                    stageId = stageId,
                };

                Assert.Throws(exception, () => action.Execute(new ActionContext
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
    }
}
