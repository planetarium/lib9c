namespace Lib9c.Tests.VersionedStates.Scenario.Scene2
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Nekoyume.VersionedStates;

    // - Remove `IState` interface.
    // + Implement `ITestState_NonVersioned` interface.
    // + Implement `ITestStateV1` interface.
    public class TestState : /*IState,*/ITestStateNonVersioned, ITestStateV1
    {
        // + Implement `ITestStateNonVersioned` property.
        Text ITestStateNonVersioned.Value => (Text)Value.Serialize();

        // + Implement `ITestStateV1` property.
        Integer ITestStateV1.Value => Value;

        public int Value { get; }

        // + Add default constructor for deserialization.
        public TestState() : this(0)
        {
        }

        public TestState(int value = 0)
        {
            Value = value;
        }

        // - Remove the constructor that takes `IValue` as a parameter.
        // public TestState(IValue serialized)
        // {
        //     Value = serialized.ToInteger();
        // }

        // - Remove the `Serialize()` method.
        // public IValue Serialize()
        // {
        //     return Value.Serialize();
        // }

        protected bool Equals(TestState other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestState)obj);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}
