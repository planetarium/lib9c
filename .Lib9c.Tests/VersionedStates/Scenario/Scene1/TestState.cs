namespace Lib9c.Tests.VersionedStates.Scenario.Scene1
{
    using Bencodex.Types;
    using Nekoyume.Model.State;

    public class TestState : IState
    {
        public int Value { get; }

        public TestState(int value = 0)
        {
            Value = value;
        }

        public TestState(IValue serialized)
        {
            Value = serialized.ToInteger();
        }

        public IValue Serialize()
        {
            return Value.Serialize();
        }

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
