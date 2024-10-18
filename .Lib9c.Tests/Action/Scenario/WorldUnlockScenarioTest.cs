namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldUnlockScenarioTest
    {
        private TableSheets _tableSheets;
        private IWorld _initialState;
        private Address _agentAddress;
        private Address _avatarAddress;
        private Address _rankingMapAddress;
        private WeeklyArenaState _weeklyArenaState;

        public WorldUnlockScenarioTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _rankingMapAddress
            );
            avatarState.level = 100;

            agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetLegacyState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetActionPoint(_avatarAddress, DailyReward.ActionPointMax)
                .SetLegacyState(_rankingMapAddress, new RankingMapState(_rankingMapAddress).Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(1, 1, 1, 2)]
        [InlineData(400, 3, 101, 4)]
        public void UnlockWorldByHackAndSlashAfterPatchTableWithAddRow(
            int avatarLevel,
            int worldIdToClear,
            int stageIdToClear,
            int worldIdToUnlock)
        {
            Assert.True(_tableSheets.CharacterLevelSheet.ContainsKey(avatarLevel));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToClear));
            Assert.True(_tableSheets.StageSheet.ContainsKey(stageIdToClear));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToUnlock));
            Assert.False(_tableSheets.WorldUnlockSheet.TryGetUnlockedInformation(
                worldIdToClear,
                stageIdToClear,
                out _));

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.level = avatarLevel;
            avatarState.worldInformation = new WorldInformation(0, _tableSheets.WorldSheet, stageIdToClear);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var doomfist = Doomfist.GetOne(_tableSheets, avatarState.level, ItemSubType.Weapon);
            avatarState.inventory.AddItem(doomfist);

            var nextState = _initialState
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(
                    _avatarAddress.Derive("world_ids"),
                    new Bencodex.Types.List(Enumerable.Range(1, worldIdToClear).Select(i => i.Serialize())));
            var hackAndSlash = new HackAndSlash
            {
                WorldId = worldIdToClear,
                StageId = stageIdToClear,
                AvatarAddress = _avatarAddress,
                Costumes = new List<Guid>(),
                Equipments = new List<Guid> { doomfist.NonFungibleId, },
                Foods = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };
            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = _agentAddress,
                RandomSeed = 0,
            });

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.worldInformation.IsStageCleared(stageIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var tableCsv = nextState.GetSheetCsv<WorldUnlockSheet>();
            var worldUnlockSheet = nextState.GetSheet<WorldUnlockSheet>();
            var newId = worldUnlockSheet.Last?.Id + 1 ?? 1;
            var newLine = $"{newId},{worldIdToClear},{stageIdToClear},{worldIdToUnlock}";
            tableCsv = $@"
id,world_id,stage_id,world_id_to_unlock,required_crystal
1,1,50,2,500
2,2,100,3,2500
3,3,150,4,50000
4,2,100,10001,0
5,4,200,5,100000
{newLine}
";

            var patchTableSheet = new PatchTableSheet
            {
                TableName = nameof(WorldUnlockSheet),
                TableCsv = tableCsv,
            };
            nextState = patchTableSheet.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = AdminState.Address,
                RandomSeed = 0,
            });

            var nextTableCsv = nextState.GetSheetCsv<WorldUnlockSheet>();
            Assert.Equal(nextTableCsv, tableCsv);

            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = _agentAddress,
                RandomSeed = 0,
            });

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));
        }
    }
}
