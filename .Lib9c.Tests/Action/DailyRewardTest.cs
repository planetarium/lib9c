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
        private readonly IWorld _initialWorld;

        public DailyRewardTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialWorld = new MockWorld();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialWorld = LegacyModule.SetState(
                    _initialWorld,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
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

            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                Addresses.GameConfig,
                gameConfigState.Serialize());
            _initialWorld = AgentModule.SetAgentState(_initialWorld, _agentAddress, agentState);
            _initialWorld = AvatarModule.SetAvatarState(_initialWorld, _avatarAddress, avatarState);
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
                RandomSeed = 0,
                Rehearsal = true,
                Signer = _agentAddress,
            });

            var updatedAddressAvatar = Assert.Single(nextState.GetAccount(Addresses.Avatar).Delta.UpdatedAddresses);
            Assert.Equal(_avatarAddress, updatedAddressAvatar);
            var updatedAddressLegacy = Assert.Single(nextState.GetAccount(ReservedAddresses.LegacyAccount).Delta.UpdatedAddresses);
            Assert.Equal(_avatarAddress, updatedAddressLegacy);
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
                    previousStates = _initialWorld;
                    break;
                case false:
                    var avatarState = AvatarModule.GetAvatarState(_initialWorld, _avatarAddress);
                    previousStates = AvatarModule.SetAvatarStateV2(_initialWorld, _avatarAddress, avatarState);
                    break;
            }

            var nextWorld = ExecuteInternal(previousStates, 1800);
            var nextGameConfigState = LegacyModule.GetGameConfigState(nextWorld);
            AvatarModule.TryGetAvatarStateV2(nextWorld, _agentAddress, _avatarAddress, out var nextAvatarState, out var migrationRequired);
            Assert.NotNull(nextAvatarState);
            Assert.NotNull(nextAvatarState.inventory);
            Assert.NotNull(nextAvatarState.questList);
            Assert.NotNull(nextAvatarState.worldInformation);
            Assert.Equal(nextGameConfigState.ActionPointMax, nextAvatarState.actionPoint);

            var avatarRuneAmount = LegacyModule.GetBalance(
                nextWorld,
                _avatarAddress,
                RuneHelper.DailyRewardRune);
            var expectedRune = RuneHelper.DailyRewardRune * nextGameConfigState.DailyRuneRewardAmount;
            Assert.Equal(expectedRune, avatarRuneAmount);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException() =>
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new MockWorld()));

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
            var avatarState = AvatarModule.GetAvatarState(_initialWorld, _avatarAddress);
            avatarState.dailyRewardReceivedIndex = dailyRewardReceivedIndex;
            var previousStates = AvatarModule.SetAvatarStateV2(_initialWorld, _avatarAddress, avatarState);
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

            var state = LegacyModule.SetState(
                _initialWorld,
                Addresses.GameConfig,
                gameConfigState.Serialize());
            var nextState = ExecuteInternal(state, 1800);
            var avatarRuneAmount = LegacyModule.GetBalance(
                nextState,
                _avatarAddress,
                RuneHelper.DailyRewardRune);
            Assert.Equal(0, (int)avatarRuneAmount.MajorUnit);
        }

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
                Rehearsal = false,
                Signer = _agentAddress,
            });
        }
    }
}
