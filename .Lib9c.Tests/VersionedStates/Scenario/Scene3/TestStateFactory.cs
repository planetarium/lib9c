namespace Lib9c.Tests.VersionedStates.Scenario.Scene3
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
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

        // + Declare a method to create a `TestStateV2` instance from serialized value.
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
                        out var testState)
                        ? new TestStateV2(value, Create(testState))
                        : null;
                }
            }

            return null;
        }
    }
}
