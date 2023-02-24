namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;

    // + Add this interface for non-versioned state.
    // + Does not inherits `ITestState` interface because it is not a versioned state and
    //   it does not have a moniker.

    // + Declare a base interface for interfaces that share the same moniker.
    public interface ITestState : IVersionedState
    {
        // + Cache moniker.
        public static readonly Text MonikerCache = "test";

        // + Override moniker property.
        Text IVersionedState.Moniker => MonikerCache;
    }
}
