namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;
    using Nekoyume.VersionedStates;

    public interface ITestStateV3 : ITestState
    {
        public static readonly Integer VersionCache = 3;

        Integer IVersionedState.Version => VersionCache;

        IValue IVersionedState.Data => new List(
            Value,
            TestState?.Serialize() ?? Null.Value);

        public Integer Value { get; }

        // * Change type of `TestStateV1` to `ITestStateV2`.
        public ITestStateV2 TestState { get; }

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
