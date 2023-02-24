namespace Lib9c.Tests.VersionedStates
{
    using Bencodex.Types;
    using Nekoyume.VersionedStates;

    public interface IVersionedStateImpl : IVersionedState
    {
        public static readonly Text MonikerCache = "versioned_state";

        public static readonly Integer VersionCache = 1;

        Text IVersionedState.Moniker => MonikerCache;

        Integer IVersionedState.Version => VersionCache;

        IValue IVersionedState.Data => new List(
            Value1?.Serialize() ?? Null.Value,
            Value2);

        INonVersionedStateImpl Value1 { get; }

        Text Value2 { get; }

        static bool TryDeconstruct(
            IValue serialized,
            out Text moniker,
            out Integer version,
            out IValue value1,
            out Text value2)
        {
            try
            {
                IValue data;
                (moniker, version, data) = Deconstruct(serialized);
                var list = (List)data;
                value1 = list[0];
                value2 = (Text)list[1];
                return true;
            }
            catch
            {
                moniker = default;
                version = default;
                value1 = default;
                value2 = default;
                return false;
            }
        }
    }

    public class VersionedStateImpl : IVersionedStateImpl
    {
        INonVersionedStateImpl IVersionedStateImpl.Value1 => Value1;

        Text IVersionedStateImpl.Value2 => Value2;

        public NonVersionedStateImpl Value1 { get; private set; }

        public string Value2 { get; private set; }

        public VersionedStateImpl() : this(null, string.Empty)
        {
        }

        public VersionedStateImpl(NonVersionedStateImpl value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
        }
    }
}
