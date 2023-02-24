namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;
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

        [Fact]
        public void CreateV3()
        {
            var current = new TestStateV3(100);
            var serialized = ((ITestStateV3)current).Serialize();
            var backwards = new object[]
            {
                new Scene1.TestState(100),
                new TestState(100),
                new TestStateV2(100),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case IState state:
                    {
                        var serializedBackward = state.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }

                    case INonVersionedState nonVersionedState:
                    {
                        var serializedBackward = nonVersionedState.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }
                }

                switch (backward)
                {
                    case ITestStateV1 testStateV1:
                    {
                        var serializedBackward = testStateV1.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV3(100, new TestStateV2(200));
            serialized = ((ITestStateV3)current).Serialize();
            backwards = new object[]
            {
                new TestStateV2(100, new TestState(200)),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV3(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV3(100, new TestStateV2(200, new TestState(201)));
            serialized = ((ITestStateV3)current).Serialize();
            CompareV3(current, serialized, serialized);
        }

        [Fact]
        public void CreateV4()
        {
            var current = new TestStateV4(new TestStateV2(100));
            var serialized = ((ITestStateV4)current).Serialize();
            var backwards = new object[]
            {
                new Scene1.TestState(100),
                new TestState(100),
                new TestStateV2(100),
                new TestStateV3(100),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case IState state:
                    {
                        var serializedBackward = state.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case INonVersionedState nonVersionedState:
                    {
                        var serializedBackward = nonVersionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }
                }

                switch (backward)
                {
                    case ITestStateV1 testStateV1:
                    {
                        var serializedBackward = testStateV1.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV4(new TestStateV2(100), new TestStateV3(200));
            serialized = ((ITestStateV4)current).Serialize();
            backwards = new object[]
            {
                new TestStateV2(100, new TestState(200)),
                new TestStateV3(100, new TestStateV2(200)),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV4(
                new TestStateV2(100),
                new TestStateV3(200, new TestStateV2(201)));
            serialized = ((ITestStateV4)current).Serialize();
            backwards = new object[]
            {
                new TestStateV3(100, new TestStateV2(200, new TestState(201))),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV4(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV4(
                new TestStateV2(100, new TestState(101)),
                new TestStateV3(
                    200,
                    new TestStateV2(201, new TestState(203))));
            serialized = ((ITestStateV4)current).Serialize();
            CompareV4(current, serialized, serialized);
        }

        [Fact]
        public void CreateV5()
        {
            var current = new TestStateV5(new TestStateV2(100));
            var serialized = ((ITestStateV5)current).Serialize();
            var backwards = new object[]
            {
                new Scene1.TestState(100),
                new TestState(100),
                new TestStateV2(100),
                new TestStateV3(100),
                new TestStateV4(new TestStateV2(100)),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case IState state:
                    {
                        var serializedBackward = state.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case INonVersionedState nonVersionedState:
                    {
                        var serializedBackward = nonVersionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }
                }

                switch (backward)
                {
                    case ITestStateV1 testStateV1:
                    {
                        var serializedBackward = testStateV1.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV5 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV5(new TestStateV2(100), new TestStateV3(200));
            serialized = ((ITestStateV5)current).Serialize();
            backwards = new object[]
            {
                new TestStateV2(100, new TestState(200)),
                new TestStateV3(100, new TestStateV2(200)),
                new TestStateV4(new TestStateV2(100), new TestStateV3(200)),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV2 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV5(
                new TestStateV2(100),
                new TestStateV3(200, new TestStateV2(201)));
            serialized = ((ITestStateV5)current).Serialize();
            backwards = new object[]
            {
                new TestStateV3(100, new TestStateV2(200, new TestState(201))),
                new TestStateV4(
                    new TestStateV2(100),
                    new TestStateV3(200, new TestStateV2(201))),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV3 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }

                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV5(
                new TestStateV2(100, new TestState(101)),
                new TestStateV3(200));
            serialized = ((ITestStateV5)current).Serialize();
            backwards = new object[]
            {
                new TestStateV4(
                    new TestStateV2(100, new TestState(101)),
                    new TestStateV3(200)),
                current,
            };
            foreach (var backward in backwards)
            {
                switch (backward)
                {
                    case ITestStateV4 versionedState:
                    {
                        var serializedBackward = versionedState.Serialize();
                        CompareV5(current, serialized, serializedBackward);
                        break;
                    }
                }
            }

            current = new TestStateV5(
                new TestStateV2(100, new TestState(101)),
                new TestStateV3(200, new TestStateV2(201)),
                new TestStateV4(
                    new TestStateV2(3010, new TestState(3011)),
                    new TestStateV3(
                        3020,
                        new TestStateV2(3021, new TestState(3022)))));
            serialized = ((ITestStateV5)current).Serialize();
            CompareV5(current, serialized, serialized);
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

        private static void CompareV3(
            TestStateV3 current,
            IValue serialized,
            IValue serializedBackward)
        {
            var created = TestStateFactory.CreateV3(serializedBackward);
            Assert.Equal(current, created);
            Assert.Equal(serialized, ((ITestStateV3)created).Serialize());
        }

        private static void CompareV4(
            TestStateV4 current,
            IValue serialized,
            IValue serializedBackward)
        {
            var created = TestStateFactory.CreateV4(serializedBackward);
            Assert.Equal(current, created);
            Assert.Equal(serialized, ((ITestStateV4)created).Serialize());
        }

        private static void CompareV5(
            TestStateV5 current,
            IValue serialized,
            IValue serializedBackward)
        {
            var created = TestStateFactory.CreateV5(serializedBackward);
            Assert.Equal(current, created);
            Assert.Equal(serialized, ((ITestStateV5)created).Serialize());
        }
    }
}
