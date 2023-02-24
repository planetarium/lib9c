namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;

    public interface ITestStateNonVersioned : INonVersionedState
    {
        // + Override `INonVersionedState.Data` property to return the serialized value of the state.
        IValue INonVersionedState.Data => Value;

        // + Declare the properties as a bencodex type only.
        Text Value { get; }

        // + Declare static deconstruct method.
        static bool TryDeconstruct(IValue serialized, out Text value)
        {
            try
            {
                value = (Text)serialized;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }
}
