namespace Lib9c.Tests.VersionedStates.Scenario.Scene3
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Nekoyume.VersionedStates;

    public interface ITestStateV2 : ITestState
    {
        public static readonly Integer VersionCache = 2;

        Integer IVersionedState.Version => VersionCache;

        // + Change the type from `Integer` to `List`.
        IValue IVersionedState.Data => new List(
            Value,
            // + Serialize `TestStateV1` property. If it is `null`, use `Null.Value`.
            TestState?.Serialize() ?? Null.Value);

        public Integer Value { get; }

        // + Add a new property.
        public ITestStateV1 TestState { get; }

        // + Declare a static method to deconstruct serialized data.
        static bool TryDeconstruct(
            IValue serialized,
            out Text moniker,
            out Integer version,
            out Integer value,
            out IValue testState)
        {
            try
            {
                IValue data;
                (moniker, version, data) = Deconstruct(serialized);
                var list = (List)data;
                value = (Integer)list[0];
                testState = list[1];
                return true;
            }
            catch
            {
                moniker = default;
                version = default;
                value = default;
                testState = default;
                return false;
            }
        }
    }
}
