namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;
    using Nekoyume.Model.State;
    using Nekoyume.VersionedStates;

    public static class TestStateFactory
    {
        // NOTE: Every create method should be handled all versions of the state.
        //       But this not means upward compatibility.
        public static TestState Create(IValue serialized)
        {
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestState(value.ToInteger())
                    : null;
            }

            switch ((uint)version)
            {
                case 1:
                {
                    return ITestStateV1.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value)
                        ? new TestState(value)
                        : null;
                }
            }

            return null;
        }

        // NOTE: Every create method should be handled all versions of the state.
        //       But this not means upward compatibility.
        public static TestStateV2 CreateV2(IValue serialized)
        {
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestStateV2(value.ToInteger())
                    : null;
            }

            switch ((uint)version)
            {
                case 1:
                {
                    return ITestStateV1.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value)
                        ? new TestStateV2(value)
                        : null;
                }

                // + Add a case for the version 2.
                case 2:
                {
                    return ITestStateV2.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testStateV1)
                        ? new TestStateV2(value, Create(testStateV1))
                        : null;
                }
            }

            return null;
        }

        // NOTE: Every create method should be handled all versions of the state.
        //       But this not means upward compatibility.
        public static TestStateV3 CreateV3(IValue serialized)
        {
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestStateV3(value.ToInteger())
                    : null;
            }

            switch ((uint)version)
            {
                case 1:
                {
                    return ITestStateV1.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value)
                        ? new TestStateV3(value)
                        : null;
                }

                // + Add a case for the version 2.
                case 2:
                {
                    return ITestStateV2.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV3(value, CreateV2(testState))
                        : null;
                }

                // + Add a case for the version 3.
                case 3:
                {
                    return ITestStateV3.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV3(value, CreateV2(testState))
                        : null;
                }
            }

            return null;
        }

        // NOTE: Every create method should be handled all versions of the state.
        //       But this not means upward compatibility.
        public static TestStateV4 CreateV4(IValue serialized)
        {
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestStateV4(new TestStateV2(value.ToInteger()))
                    : null;
            }

            switch ((uint)version)
            {
                case 1:
                {
                    return ITestStateV1.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value)
                        ? new TestStateV4(new TestStateV2(value))
                        : null;
                }

                case 2:
                {
                    return ITestStateV2.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV4(
                            new TestStateV2(value),
                            CreateV3(testState))
                        : null;
                }

                case 3:
                {
                    return ITestStateV3.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV4(
                            new TestStateV2(value),
                            CreateV3(testState))
                        : null;
                }

                // + Add a case for the version 4.
                case 4:
                {
                    return ITestStateV4.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var otherTestState,
                        out var anotherTestState)
                        ? new TestStateV4(
                            CreateV2(otherTestState),
                            CreateV3(anotherTestState))
                        : null;
                }
            }

            return null;
        }

        // NOTE: Every create method should be handled all versions of the state.
        //       But this not means upward compatibility.
        public static TestStateV5 CreateV5(IValue serialized)
        {
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestStateV5(new TestStateV2(value.ToInteger()))
                    : null;
            }

            switch ((uint)version)
            {
                case 1:
                {
                    return ITestStateV1.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value)
                        ? new TestStateV5(new TestStateV2(value))
                        : null;
                }

                case 2:
                {
                    return ITestStateV2.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV5(
                            new TestStateV2(value),
                            CreateV3(testState))
                        : null;
                }

                case 3:
                {
                    return ITestStateV3.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var value,
                        out var testState)
                        ? new TestStateV5(
                            new TestStateV2(value),
                            CreateV3(testState))
                        : null;
                }

                case 4:
                {
                    return ITestStateV4.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var otherTestState,
                        out var anotherTestState)
                        ? new TestStateV5(
                            CreateV2(otherTestState),
                            CreateV3(anotherTestState))
                        : null;
                }

                // + Add a case for the version 5.
                case 5:
                {
                    return ITestStateV5.TryDeconstruct(
                        serialized,
                        out _,
                        out _,
                        out var otherTestState,
                        out var anotherTestState,
                        out var yetAnotherTestState)
                        ? new TestStateV5(
                            CreateV2(otherTestState),
                            CreateV3(anotherTestState),
                            CreateV4(yetAnotherTestState))
                        : null;
                }
            }

            return null;
        }
    }
}
