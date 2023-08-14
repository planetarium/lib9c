namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class RankingBattleTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _weeklyArenaAddress;
        private readonly IWorld _initialWorld;

        public RankingBattleTest(ITestOutputHelper outputHelper)
        {
            _initialWorld = new MockWorld();

            var keys = new List<string>
            {
                nameof(SkillActionBuffSheet),
                nameof(ActionBuffSheet),
                nameof(StatBuffSheet),
            };
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                if (!keys.Contains(key))
                {
                    _initialWorld = LegacyModule.SetState(
                        _initialWorld,
                        Addresses.TableSheet.Derive(key),
                        value.Serialize());
                }
            }

            _tableSheets = new TableSheets(sheets);

            var rankingMapAddress = new PrivateKey().ToAddress();

            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            _agent1Address = agent1State.address;
            _avatar1Address = avatar1State.address;

            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            var agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;

            var weeklyArenaState = new WeeklyArenaState(0);
            var weeklyAddressListAddress = weeklyArenaState.address.Derive("address_list");
            var weeklyAddressList = new List<Address>
            {
                _avatar1Address,
                _avatar2Address,
            };
            var arenaInfo1Address = weeklyArenaState.address.Derive(_avatar1Address.ToByteArray());
            var arenaInfo1 = new ArenaInfo(
                avatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet,
                true);
            var arenaInfo2Address = weeklyArenaState.address.Derive(_avatar2Address.ToByteArray());
            var arenaInfo2 = new ArenaInfo(
                avatar2State,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet,
                true);
            _weeklyArenaAddress = weeklyArenaState.address;

            _initialWorld = AgentModule.SetAgentState(_initialWorld, _agent1Address, agent1State);
            _initialWorld = AvatarModule.SetAvatarState(
                _initialWorld,
                _avatar1Address,
                avatar1State);
            _initialWorld = AgentModule.SetAgentState(_initialWorld, agent2Address, agent2State);
            _initialWorld = AvatarModule.SetAvatarStateV2(
                _initialWorld,
                _avatar2Address,
                avatar2State);
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                Addresses.GameConfig,
                new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize());
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                _weeklyArenaAddress,
                weeklyArenaState.Serialize());
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                weeklyAddressListAddress,
                weeklyAddressList.Aggregate(
                    List.Empty,
                    (list, address) => list.Add(address.Serialize())));
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                arenaInfo1Address,
                arenaInfo1.Serialize());
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                arenaInfo2Address,
                arenaInfo2.Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        public static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    Math.Max(
                        tableSheets.StageSheet.First?.Id ?? 1,
                        GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);

            return (agentState, avatarState);
        }

        [Fact]
        public void ExecuteActionObsoletedException()
        {
            var previousArenaInfoAddress = _weeklyArenaAddress.Derive(_avatar1Address.ToByteArray());
            var previousArenaInfo = new ArenaInfo(
                (Dictionary)LegacyModule.GetState(_initialWorld, previousArenaInfoAddress));
            var previousAvatarState = AvatarModule.GetAvatarState(_initialWorld, _avatar1Address);
            while (true)
            {
                previousArenaInfo.UpdateV3(previousAvatarState, previousArenaInfo, BattleLog.Result.Lose);
                if (previousArenaInfo.DailyChallengeCount == 0)
                {
                    break;
                }
            }

            var previousState = LegacyModule.SetState(
                _initialWorld,
                previousArenaInfoAddress,
                previousArenaInfo.Serialize());

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<ActionObsoletedException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousState = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }
    }
}
