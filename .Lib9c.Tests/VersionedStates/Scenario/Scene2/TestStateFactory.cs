namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Nekoyume.VersionedStates;

    // + Declare a factory class for TestState.
    public static class TestStateFactory
    {
        // + Declare a method to create a TestState instance from serialized value.
        public static TestState Create(IValue serialized)
        {
            // + Use `IVersionedState.TryDeconstruct()` method to check if the serialized value is
            //   a versioned state.
            if (!IVersionedState.TryDeconstruct(
                    serialized,
                    out var moniker,
                    out var version,
                    out _) ||
                moniker != ITestState.MonikerCache)
            {
                // + If the serialized value is not a versioned state, try to deconstruct it as
                //   a non-versioned state.
                return ITestStateNonVersioned.TryDeconstruct(
                    serialized,
                    out var value)
                    ? new TestState(value.ToInteger())
                    : null;
            }

            // + If the serialized value is a versioned state, try to deconstruct it as a
            //   versioned state.
            // + Switch the version of the state and create a new instance.
            switch ((uint)version)
            {
                // + Add a case for the version 1.
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
    }
}
