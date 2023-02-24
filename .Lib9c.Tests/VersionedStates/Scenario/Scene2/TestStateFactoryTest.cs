namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Nekoyume.VersionedStates;
    using Xunit;

    public class TestStateFactoryTest
    {
        [Fact]
        public void Create()
        {
            var current = new TestState(100);
            var serialized = ((ITestStateV1)current).Serialize();
            var backwards = new object[]
            {
                new Scene1.TestState(100),
                current,
            };
            foreach (var backward in backwards)
            {
                if (backward is IState state)
                {
                    var serializedBackward = state.Serialize();
                    Compare(current, serialized, serializedBackward);
                }

                if (backward is INonVersionedState nonVersionedState)
                {
                    var serializedBackward = nonVersionedState.Serialize();
                    Compare(current, serialized, serializedBackward);
                }

                // Note: not IVersionedState,
                //       only testing the implementation interface of IVersionedState.
                if (backward is ITestStateV1 testStateV1)
                {
                    var serializedBackward = testStateV1.Serialize();
                    Compare(current, serialized, serializedBackward);
                }
            }
        }

        private static void Compare(
            TestState current,
            IValue serialized,
            IValue serializedBackward)
        {
            var created = TestStateFactory.Create(serializedBackward);
            Assert.Equal(current, created);
            Assert.Equal(serialized, ((ITestStateV1)created).Serialize());
        }
    }
}
