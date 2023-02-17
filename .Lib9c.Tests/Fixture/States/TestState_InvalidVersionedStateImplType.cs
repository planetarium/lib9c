// ReSharper disable InconsistentNaming
namespace Lib9c.Tests.Fixture.States
{
    using Bencodex.Types;
    using Nekoyume.Model;
    using Nekoyume.Model.State;

    public interface INotAttach_VersionedStateAttribute
    {
    }

    [VersionedStateImpl(typeof(INotAttach_VersionedStateAttribute))]
    public class TestState_InvalidVersionedStateImplType : IState
    {
        public int Value { get; }

        public TestState_InvalidVersionedStateImplType() : this(0)
        {
        }

        public TestState_InvalidVersionedStateImplType(int value)
        {
            Value = value;
        }

        public TestState_InvalidVersionedStateImplType(IValue serialized)
        {
            Value = (Integer)serialized;
        }

        public IValue Serialize()
        {
            return (Integer)Value;
        }
    }
}
