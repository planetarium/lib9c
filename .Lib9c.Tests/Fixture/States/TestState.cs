namespace Lib9c.Tests.Fixture.States
{
    using Bencodex.Types;
    using Nekoyume.Model.State;

    // NOTE: Does not attach [VersionedStateImpl] attribute.
    public class TestState : IState
    {
        public int Value { get; }

        public TestState(int value)
        {
            Value = value;
        }

        public TestState(IValue serialized)
        {
            Value = (Integer)serialized;
        }

        public IValue Serialize()
        {
            return (Integer)Value;
        }
    }
}
