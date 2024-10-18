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

            _agentAddress = new PrivateKey().Address;
            _avatarAddress = Addresses.GetAvatarAddress(_agentAddress, 0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool stateExist)
        {
            var previousStates = stateExist switch
            {
                true => _initialState.SetDailyRewardReceivedBlockIndex(_agentAddress, 0L)
                    .SetActionPoint(_avatarAddress, 0),
                false => _initialState,
            };

            var nextState = ExecuteInternal(previousStates, _avatarAddress, DailyReward.DailyRewardInterval);
            var receivedBlockIndex = nextState.GetDailyRewardReceivedBlockIndex(_avatarAddress);
            Assert.Equal(DailyReward.DailyRewardInterval, receivedBlockIndex);
            var actionPoint = nextState.GetActionPoint(_avatarAddress);
            Assert.Equal(DailyReward.ActionPointMax, actionPoint);

            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            var expectedRune = RuneHelper.DailyRewardRune * DailyReward.DailyRuneRewardAmount;
            Assert.Equal(expectedRune, avatarRuneAmount);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, DailyReward.DailyRewardInterval - 1, true)]
        [InlineData(0, DailyReward.DailyRewardInterval, false)]
        [InlineData(DailyReward.DailyRewardInterval, DailyReward.DailyRewardInterval, true)]
        [InlineData(DailyReward.DailyRewardInterval, DailyReward.DailyRewardInterval * 2 - 1, true)]
        [InlineData(DailyReward.DailyRewardInterval, DailyReward.DailyRewardInterval * 2, false)]
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
                    new PrivateKey().Address));
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
