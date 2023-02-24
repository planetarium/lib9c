namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;

    // + Declare a versioned state interface.
    // + Inherits `ITestState` interface to share the same moniker.
    public interface ITestStateV1 : ITestState
    {
        // + Cache version.
        public static readonly Integer VersionCache = 1;

        // + Override `IVersionedState.Version` property.
        Integer IVersionedState.Version => VersionCache;

        IValue IVersionedState.Data => Value;

        // + Changed the type of the property from `Text` to `Integer`.
        public Integer Value { get; }

        // + Declare a static method to deconstruct serialized data.
        static bool TryDeconstruct(
            IValue serialized,
            out Text moniker,
            out Integer version,
            out Integer value)
        {
            try
            {
                IValue data;
                (moniker, version, data) = Deconstruct(serialized);
                value = (Integer)data;
                return true;
            }
            catch
            {
                moniker = default;
                version = default;
                value = default;
                return false;
            }
        }
    }
}
