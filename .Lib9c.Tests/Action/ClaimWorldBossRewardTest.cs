namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Action.Exceptions.AdventureBoss;
    using Lib9c.Helper;
    using Lib9c.Model.Mail;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class ClaimWorldBossRewardTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IWorld _state;

        public ClaimWorldBossRewardTest()
        {
            var tableCsv = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(tableCsv);
            _state = new World(MockUtil.MockModernWorldState);
            foreach (var kv in tableCsv)
            {
                _state = _state.SetLegacyState(Addresses.GetSheetAddress(kv.Key), kv.Value.Serialize());
            }
        }

        [Theory]
        [InlineData(0.0001)]
        [InlineData(1)]
        [InlineData(100)]
        public void Execute(decimal contribution)
        {
            Address agentAddress = new PrivateKey().Address;
            var avatarAddress = agentAddress.Derive("avatar-state-0");
            var bossRow = _tableSheets.WorldBossListSheet.OrderedList.First();
            var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, bossRow.Id);
            var worldBossAddress = Addresses.GetWorldBossAddress(bossRow.Id);
            var worldBossState = new WorldBossState(bossRow, _tableSheets.WorldBossGlobalHpSheet[1]);
            const int totalScore = 1000000;
            worldBossState.TotalDamage = totalScore;
            var raiderState = new RaiderState
            {
                HighScore = 0,
                TotalScore = (long)(totalScore * contribution / 100),
            };
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            var state = _state
                .SetLegacyState(raiderAddress, raiderState.Serialize())
                .SetLegacyState(worldBossAddress, worldBossState.Serialize())
                .SetAvatarState(avatarAddress, avatarState);
            var action = new ClaimWorldBossReward(avatarAddress);
            var nextState = action.Execute(new ActionContext
            {
                RandomSeed = 0,
                Signer = agentAddress,
                BlockIndex = bossRow.EndedBlockIndex + 1L,
                PreviousState = state,
            });
            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            Assert.Empty(nextAvatarState.mailBox.OfType<WorldBossRewardMail>());

            var (items, fav) = WorldBossHelper.CalculateContributionReward(_tableSheets.WorldBossContributionRewardSheet[bossRow.BossId], contribution);

            // Check reward
            Assert.All(items, tuple => avatarState.inventory.HasItem(tuple.id, tuple.count));

            foreach (var asset in fav)
            {
                Assert.Equal(
                    asset.Currency.Equals(Currencies.Crystal)
                        ? nextState.GetBalance(agentAddress, asset.Currency)
                        : nextState.GetBalance(avatarAddress, asset.Currency), asset);
            }

            var nextRaiderState = nextState.GetRaiderState(raiderAddress);
            Assert.True(nextRaiderState.HasClaimedReward);

            Assert.Throws<AlreadyClaimedException>(() => action.Execute(new ActionContext
            {
                RandomSeed = 0,
                Signer = agentAddress,
                BlockIndex = bossRow.EndedBlockIndex + 2L,
                PreviousState = nextState,
            }));
        }
    }
}
