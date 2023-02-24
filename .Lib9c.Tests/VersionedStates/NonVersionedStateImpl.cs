namespace Lib9c.Tests.VersionedStates
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;

    public interface INonVersionedStateImpl : INonVersionedState
    {
        IValue INonVersionedState.Data => Value;

        Integer Value { get; }

        static bool TryDeconstruct(
            IValue serialized,
            out Integer value)
        {
            try
            {
                value = (Integer)serialized;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }

    public class NonVersionedStateImpl : INonVersionedStateImpl
    {
        Integer INonVersionedStateImpl.Value => Value;

        public int Value { get; private set; }

        public NonVersionedStateImpl() : this(0)
        {
        }

        public NonVersionedStateImpl(int value)
        {
            Value = value;
        }
    }
}
