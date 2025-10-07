namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex;
    using Lib9c.Action;
    using Lib9c.Action.Arena;
    using Lib9c.Model;
    using Lib9c.Model.Arena;
    using Lib9c.Model.EnumType;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Tx;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using CpState = Lib9c.Model.State.CpState;
    using GameConfigState = Lib9c.Model.State.GameConfigState;

    public class BattleTest
    {
        private static readonly Codec _codec = new Codec();

        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly PrivateKey _preset1;
        private readonly Address _preset1Agent;
        private readonly Address _preset1Avatar;

        private readonly PrivateKey _preset2;
        private readonly Address _preset2Agent;
        private readonly Address _preset2Avatar;

        private IWorld _initialStates;

        public BattleTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialStates = new World(MockUtil.MockModernWorldState);

            _sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in _sheets)
            {
                _initialStates = _initialStates.SetLegacyState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }

            _tableSheets = new TableSheets(_sheets);

            var rankingMapAddress = new PrivateKey().Address;
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard
            );

            var preset1 = GetAgentStateWithAvatarState(
                _tableSheets,
                rankingMapAddress,
                clearStageId
            );
            _preset1 = preset1.PrivateKey;
            _preset1Agent = preset1.AgentState.address;
            _preset1Avatar = preset1.AvatarState.address;

            var preset2 = GetAgentStateWithAvatarState(
                _tableSheets,
                rankingMapAddress,
                clearStageId
            );
            _preset2 = preset2.PrivateKey;
            _preset2Agent = preset2.AgentState.address;
            _preset2Avatar = preset2.AvatarState.address;

            _initialStates = _initialStates
                .SetAgentState(_preset1Agent, preset1.AgentState)
                .SetAvatarState(_preset1Avatar, preset1.AvatarState)
                .SetAgentState(_preset2Agent, preset2.AgentState)
                .SetAvatarState(_preset2Avatar, preset2.AvatarState)
                .SetActionPoint(_preset1Avatar, 120)
                .SetLegacyState(
                    Addresses.GameConfig,
                    new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize()
                );

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Execute_Success()
        {
            var previousStates = _initialStates;
            var random = new TestRandom();

            var action = new Lib9c.Action.Arena.Battle
            {
                myAvatarAddress = _preset1Avatar,
                enemyAvatarAddress = _preset2Avatar,
                memo = "my memo",
                arenaProvider = ArenaProvider.PLANETARIUM,
                chargeAp = false,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var buffer = new byte[TxId.Size];
            random.NextBytes(buffer);
            var txid = new TxId(buffer);

            var nextStates = action.Execute(
                new ActionContext
                {
                    TxId = txid,
                    PreviousState = previousStates,
                    Signer = _preset1Agent,
                    RandomSeed = random.Seed,
                    BlockIndex = 1,
                }
            );

            var accountAddress = Addresses.Battle.Derive(ArenaProvider.PLANETARIUM.ToString());
            var account = nextStates.GetAccountState(accountAddress);
            var resultState = account.GetState(ArenaResult.DeriveAddress(_preset1Avatar, txid));
            var arenaResult = new ArenaResult(resultState);
            var resultActionPoint = nextStates.GetActionPoint(_preset1Avatar);
            var cpAccount = nextStates.GetAccountState(Addresses.GetCpAccountAddress(BattleType.Arena));
            var resultCpState = cpAccount.GetState(_preset1Avatar);
            var cpState = new CpState(resultCpState);

            Assert.IsType<bool>(arenaResult.IsVictory);
            Assert.True(arenaResult.Cp > 0);
            Assert.True(cpState.Cp > 0);
            Assert.Equal(115, resultActionPoint);
        }

        private static (
            PrivateKey PrivateKey,
            Lib9c.Model.State.AgentState AgentState,
            Lib9c.Model.State.AvatarState AvatarState
        ) GetAgentStateWithAvatarState(
            TableSheets tableSheets,
            Address rankingMapAddress,
            int clearStageId
        )
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.Address;
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                rankingMapAddress
            );
            avatarState.worldInformation = new WorldInformation(
                0,
                tableSheets.WorldSheet,
                clearStageId
            );

            agentState.avatarAddresses.Add(0, avatarAddress);

            return (privateKey, agentState, avatarState);
        }
    }
}
