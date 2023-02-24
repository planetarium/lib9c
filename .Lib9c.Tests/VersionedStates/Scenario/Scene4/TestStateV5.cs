namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using System;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;

    // + Implement `ITestStateV5` interface.
    public class TestStateV5 : ITestStateV5
    {
        ITestStateV2 ITestStateV5.TestState => TestState;

        ITestStateV3 ITestStateV5.OtherTestState => OtherTestState;

        ITestStateV4 ITestStateV5.AnotherTestState => AnotherTestState;

        public TestStateV2 TestState { get; }

        public TestStateV3 OtherTestState { get; }

        public TestStateV4 AnotherTestState { get; }

        public TestStateV5() : this(null)
        {
        }

        public TestStateV5(
            TestStateV2 testState = null,
            TestStateV3 otherTestState = null,
            TestStateV4 anotherTestState = null)
        {
            TestState = testState;
            OtherTestState = otherTestState;
            AnotherTestState = anotherTestState;
        }

        protected bool Equals(TestStateV5 other)
        {
            return Equals(TestState, other.TestState) &&
                   Equals(OtherTestState, other.OtherTestState) &&
                   Equals(AnotherTestState, other.AnotherTestState);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestStateV5)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TestState, OtherTestState, AnotherTestState);
        }
    }
}
