namespace Lib9c.Tests.Fixture.States
{
    using Bencodex.Types;
    using Nekoyume.Model;
    using Nekoyume.Model.State;

    [VersionedState(Moniker, Version)]
    // [VersionedStateType(typeof(Integer))]
    public interface ITestStateV1
    {
        public const string Moniker = "test";
        public const uint Version = 1;

        // [VersionedStateRootObject]
        public Integer Value { get; }
    }

    [VersionedStateImpl(typeof(ITestStateV1))]
    public class TestStateV1 : IState
    {
        public int Value { get; }

        public TestStateV1() : this(0)
        {
        }

        public TestStateV1(int value)
        {
            Value = value;
        }

        public TestStateV1(IValue serialized)
        {
            Value = (Integer)serialized;
        }

        public IValue Serialize()
        {
            return (Integer)Value;
        }
    }
}
