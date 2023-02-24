namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;
    using Nekoyume.VersionedStates;

    public interface ITestStateV4 : ITestState
    {
        public static readonly Integer VersionCache = 4;

        Integer IVersionedState.Version => VersionCache;

        IValue IVersionedState.Data => new List(
            TestState?.Serialize() ?? Null.Value,
            OtherTestState?.Serialize() ?? Null.Value);

        public ITestStateV2 TestState { get; }

        // * Add a new property.
        public ITestStateV3 OtherTestState { get; }

        static bool TryDeconstruct(
            IValue serialized,
            out Text moniker,
            out Integer version,
            out IValue testState,
            out IValue otherTestState)
        {
            try
            {
                IValue data;
                (moniker, version, data) = Deconstruct(serialized);
                var list = (List)data;
                testState = list[0];
                otherTestState = list[1];
                return true;
            }
            catch
            {
                moniker = default;
                version = default;
                testState = default;
                otherTestState = default;
                return false;
            }
        }
    }
}
