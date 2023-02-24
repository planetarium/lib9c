namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.VersionedStates.Scenario.Scene2;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;

    // + Implement `ITestStateV3` interface.
    public class TestStateV3 : ITestStateV3
    {
        Integer ITestStateV3.Value => Value;

        ITestStateV2 ITestStateV3.TestState => TestState;

        public int Value { get; }

        public TestStateV2 TestState { get; }

        public TestStateV3() : this(0)
        {
        }

        public TestStateV3(int value = 0, TestStateV2 testState = null)
        {
            Value = value;
            TestState = testState;
        }

        protected bool Equals(TestStateV3 other)
        {
            return Value == other.Value &&
                   Equals(TestState, other.TestState);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestStateV3)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, TestState);
        }
    }
}
