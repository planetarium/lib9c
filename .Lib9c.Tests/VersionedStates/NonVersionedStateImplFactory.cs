namespace Lib9c.Tests.VersionedStates
{
    using Bencodex.Types;

    public static class NonVersionedStateImplFactory
    {
        public static NonVersionedStateImpl Create(IValue serialized)
        {
            return INonVersionedStateImpl.TryDeconstruct(serialized, out var value)
                ? new NonVersionedStateImpl(value)
                : null;
        }
    }
}
