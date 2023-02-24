namespace Lib9c.Tests.VersionedStates
{
    using Bencodex.Types;

    public static class VersionedStateImplFactory
    {
        public static VersionedStateImpl Create(IValue serialized)
        {
            if (!IVersionedStateImpl.TryDeconstruct(
                    serialized,
                    out _,
                    out _,
                    out var value1,
                    out var value2))
            {
                return null;
            }

            var value1Obj = NonVersionedStateImplFactory.Create(value1);
            return new VersionedStateImpl(value1Obj, value2);
        }
    }
}
