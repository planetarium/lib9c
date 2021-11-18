namespace Lib9c.Tests
{
    using System;
    using Lib9c.Formatters;
    using MessagePack;
    using MessagePack.Resolvers;
    using Xunit;

    public class NineChroniclesResolverSetter : IDisposable
    {
        public NineChroniclesResolverSetter()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("Resolver Collection")]
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
#pragma warning disable SA1402
    public class ResolverCollection : ICollectionFixture<NineChroniclesResolverSetter>
#pragma warning restore SA1402
    {
    }
}
