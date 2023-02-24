namespace Lib9c.Tests.VersionedStates.Scenario.Scene3
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
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
                if (backward is ITestStateV1 versionedState)
                {
                    var serializedBackward = versionedState.Serialize();
                    Compare(current, serialized, serializedBackward);
                }
            }
        }

        [Fact]
        public void CreateV2()
        {
            var current = new TestStateV2(100);
            var serialized = ((ITestStateV2)current).Serialize();
            var backwards = new object[]
            {
                new Scene1.TestState(100),
                new TestState(100),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case IState state:
                    {
                        var serializedBackward = state.Serialize();
                        CompareV2(current, serialized, serializedBackward);
                        break;
                    }

                    case INonVersionedState nonVersionedState:
                    {
                        var serializedBackward = nonVersionedState.Serialize();
                        CompareV2(current, serialized, serializedBackward);
                        break;
                    }
                }

                switch (backward)
                {
                    case ITestStateV1 testStateV1:
                    {
                        var serializedBackward = testStateV1.Serialize();
                        CompareV2(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV2(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV2(100, new TestState(200));
            serialized = ((ITestStateV2)current).Serialize();
            CompareV2(current, serialized, serialized);
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

        private static void CompareV2(
            TestStateV2 current,
            IValue serialized,
            IValue serializedBackward)
        {
            var created = TestStateFactory.CreateV2(serializedBackward);
            Assert.Equal(current, created);
            Assert.Equal(serialized, ((ITestStateV2)created).Serialize());
        }
    }
}
