namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
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
        private readonly IAccount _initialAccount;
        private readonly IWorld _initialWorld;

        public DailyRewardTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialAccount = new MockAccount();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialAccount = _initialAccount
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(tableSheets.GameConfigSheet);
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
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

            _initialAccount = _initialAccount
                .SetState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
            _initialWorld = new MockWorld(_initialAccount);
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new DailyReward
            {
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousState = new MockWorld(),
                Random = new TestRandom(),
                Rehearsal = true,
                Signer = _agentAddress,
            });

            var updatedAddress = Assert.Single(nextState.Delta.Accounts.Values.SelectMany(a => a.Delta.UpdatedAddresses));
            Assert.Equal(_avatarAddress, updatedAddress);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool legacy)
        {
            IAccount previousStates = null;
            switch (legacy)
            {
                case true:
                    previousStates = _initialAccount;
                    break;
                case false:
                    var avatarState = AvatarModule.GetAvatarState(_initialWorld, _avatarAddress);
                    previousStates = SetAvatarStateAsV2To(_initialAccount, avatarState);
                    break;
            }

            var nextState = ExecuteInternal(previousStates, 1800);
            var nextWorld = _initialWorld.SetAccount(nextState);
            var nextGameConfigState = LegacyModule.GetGameConfigState(nextWorld);
            AvatarModule.TryGetAvatarStateV2(nextWorld, _agentAddress, _avatarAddress, out var nextAvatarState, out var migrationRequired);
            Assert.Equal(legacy, migrationRequired);
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
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new MockAccount()));

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 1799, true)]
        [InlineData(0, 1800, false)]
        [InlineData(1800, 1800, true)]
        [InlineData(1800, 1800 + 1799, true)]
        [InlineData(1800, 1800 + 1800, false)]
        public void Execute_Throw_RequiredBlockIndexException(
            long dailyRewardReceivedIndex,
            long executeBlockIndex,
            bool throwsException)
        {
            var avatarState = AvatarModule.GetAvatarState(new MockWorld(_initialAccount), _avatarAddress);
            avatarState.dailyRewardReceivedIndex = dailyRewardReceivedIndex;
            var previousStates = SetAvatarStateAsV2To(_initialAccount, avatarState);
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

            var state = _initialAccount
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
            var nextState = ExecuteInternal(state, 1800);
            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            Assert.Equal(0, (int)avatarRuneAmount.MajorUnit);
        }

        private IAccount SetAvatarStateAsV2To(IAccount state, AvatarState avatarState) =>
            state
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

        private IAccount ExecuteInternal(IAccount previousStates, long blockIndex = 0)
        {
            var dailyRewardAction = new DailyReward
            {
                avatarAddress = _avatarAddress,
            };

            return dailyRewardAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = new MockWorld(previousStates),
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            }).GetAccount(ReservedAddresses.LegacyAccount);
        }
    }
}
