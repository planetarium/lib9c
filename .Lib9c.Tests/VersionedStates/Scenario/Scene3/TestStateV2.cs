namespace Lib9c.Tests.VersionedStates.Scenario.Scene3
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;

    // + Implement `ITestStateV2` interface.
    public class TestStateV2 : ITestStateV2
    {
        Integer ITestStateV2.Value => Value;

        ITestStateV1 ITestStateV2.TestState => TestState;

        public int Value { get; }

        public TestState TestState { get; }

        public TestStateV2() : this(0)
        {
        }

        public TestStateV2(int value = 0, TestState testState = null)
        {
            Value = value;
            TestState = testState;
        }

        protected bool Equals(TestStateV2 other)
        {
            return Value == other.Value && Equals(TestState, other.TestState);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestStateV2)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, TestState);
        }
    }
}
