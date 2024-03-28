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
            _avatarAddress = Addresses.GetAvatarAddress(_agentAddress, 0);

            _initialState = _initialState
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool stateExist)
        {
            IWorld previousStates = stateExist switch
            {
                true => _initialState.SetDailyRewardReceivedBlockIndex(_agentAddress, 0L)
                    .SetActionPoint(_avatarAddress, 0),
                false => _initialState
            };

            var nextState = ExecuteInternal(previousStates, _avatarAddress, 2448);
            var nextGameConfigState = nextState.GetGameConfigState();
            var receivedBlockIndex = nextState.GetDailyRewardReceivedBlockIndex(_avatarAddress);
            Assert.Equal(2448L, receivedBlockIndex);
            var actionPoint = nextState.GetActionPoint(_avatarAddress);
            Assert.Equal(nextGameConfigState.ActionPointMax, actionPoint);

            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            var expectedRune = RuneHelper.DailyRewardRune * nextGameConfigState.DailyRuneRewardAmount;
            Assert.Equal(expectedRune, avatarRuneAmount);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException() =>
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new World(MockUtil.MockModernWorldState), _avatarAddress));

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
            var previousStates =
                _initialState.SetDailyRewardReceivedBlockIndex(
                    _avatarAddress,
                    dailyRewardReceivedIndex);
            try
            {
                ExecuteInternal(previousStates, _avatarAddress, executeBlockIndex);
            }
            catch (RequiredBlockIndexException)
            {
                Assert.True(throwsException);
            }
        }

        [Fact]
        public void Execute_Throw_InvalidAddressException()
        {
            Assert.Throws<InvalidAddressException>(() =>
                ExecuteInternal(
                    new World(MockUtil.MockModernWorldState),
                    new PrivateKey().Address,
                    2448L));
        }

        [Fact]
        public void Execute_Without_Runereward()
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
            var nextState = ExecuteInternal(state, _avatarAddress, 1800);
            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            Assert.Equal(0, (int)avatarRuneAmount.MajorUnit);
        }

        private IWorld ExecuteInternal(IWorld previousStates, Address avatarAddress, long blockIndex = 0)
        {
            var dailyRewardAction = new DailyReward
            {
                avatarAddress = avatarAddress,
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
