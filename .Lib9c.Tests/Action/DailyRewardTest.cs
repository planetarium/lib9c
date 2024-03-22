namespace Lib9c.Tests.Action
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class DailyRewardTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IWorld _initialState;

        public DailyRewardTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new World(MockUtil.MockModernWorldState);
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(tableSheets.GameConfigSheet);
            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().Address;
            var rankingMapAddress = new PrivateKey().Address;
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress)
            {
                actionPoint = 0,
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool legacy)
        {
            IWorld previousStates = null;
            switch (legacy)
            {
                case true:
                    previousStates = _initialState;
                    break;
                case false:
                    var avatarState = _initialState.GetAvatarState(_avatarAddress);
                    previousStates = SetAvatarStateAsV2To(_initialState, avatarState);
                    break;
            }

            var nextState = ExecuteInternal(previousStates, 2448);
            var nextGameConfigState = nextState.GetGameConfigState();
            nextState.TryGetAvatarState(_agentAddress, _avatarAddress, out var nextAvatarState);
            Assert.NotNull(nextAvatarState);
            Assert.NotNull(nextAvatarState.inventory);
            Assert.NotNull(nextAvatarState.questList);
            Assert.NotNull(nextAvatarState.worldInformation);
            Assert.Equal(nextGameConfigState.ActionPointMax, nextAvatarState.actionPoint);

            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            var expectedRune = RuneHelper.DailyRewardRune * nextGameConfigState.DailyRuneRewardAmount;
            Assert.Equal(expectedRune, avatarRuneAmount);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException() =>
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new World(MockUtil.MockModernWorldState)));

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 2447, true)]
        [InlineData(0, 2448, false)]
        [InlineData(2448, 2448, true)]
        [InlineData(2448, 2448 + 2447, true)]
        [InlineData(2448, 2448 + 2448, false)]
        public void Execute_Throw_RequiredBlockIndexException(
            long dailyRewardReceivedIndex,
            long executeBlockIndex,
            bool throwsException)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.dailyRewardReceivedIndex = dailyRewardReceivedIndex;
            var previousStates = SetAvatarStateAsV2To(_initialState, avatarState);
            try
            {
                ExecuteInternal(previousStates, executeBlockIndex);
            }
            catch (RequiredBlockIndexException)
            {
                Assert.True(throwsException);
            }
        }

        [Fact]
        private void Execute_Without_Runereward()
        {
            var gameConfigSheet = new GameConfigSheet();
            var csv = @"key,value
hourglass_per_block,3
action_point_max,120
daily_reward_interval,1
daily_arena_interval,5040
weekly_arena_interval,56000
required_appraise_block,10
battle_arena_interval,4
rune_stat_slot_unlock_cost,50
rune_skill_slot_unlock_cost,500";
            gameConfigSheet.Set(csv);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(gameConfigSheet);

            var state = _initialState
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize());
            var nextState = ExecuteInternal(state, 1800);
            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            Assert.Equal(0, (int)avatarRuneAmount.MajorUnit);
        }

        private IWorld SetAvatarStateAsV2To(IWorld state, AvatarState avatarState) =>
            state.SetAvatarState(_avatarAddress, avatarState);

        private IWorld ExecuteInternal(IWorld previousStates, long blockIndex = 0)
        {
            var dailyRewardAction = new DailyReward
            {
                avatarAddress = _avatarAddress,
            };

            return dailyRewardAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousStates,
                RandomSeed = 0,
                Signer = _agentAddress,
            });
        }
    }
}
