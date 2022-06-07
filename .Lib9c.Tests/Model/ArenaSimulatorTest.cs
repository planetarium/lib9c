namespace Lib9c.Tests.Model
{
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public ArenaSimulatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new ArenaSimulator(_random);
            // var log = simulator.Simulate();

            // var player = simulator.Player;
            //
            // while (player.Level == 1)
            // {
            //     simulator.Simulate(1);
            // }
            //
            // var player2 = simulator.Player;
            // Assert.Equal(2, player2.Level);
            // Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            // Assert.True(simulator.Log.OfType<GetExp>().Any());
        }
    }
}
