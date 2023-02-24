namespace Lib9c.Tests.VersionedStates.Scenario.Scene4
{
    using System;
    using Lib9c.Tests.VersionedStates.Scenario.Scene3;

    // + Implement `ITestStateV4` interface.
    public class TestStateV4 : ITestStateV4
    {
        ITestStateV2 ITestStateV4.TestState => TestState;

        ITestStateV3 ITestStateV4.OtherTestState => OtherTestState;

        public TestStateV2 TestState { get; }

        public TestStateV3 OtherTestState { get; }

        public TestStateV4() : this(null)
        {
        }

        public TestStateV4(
            TestStateV2 testState = null,
            TestStateV3 otherTestState = null)
        {
            TestState = testState;
            OtherTestState = otherTestState;
        }

        protected bool Equals(TestStateV4 other)
        {
            return Equals(TestState, other.TestState) &&
                   Equals(OtherTestState, other.OtherTestState);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestStateV4)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TestState, OtherTestState);
        }
    }
}
