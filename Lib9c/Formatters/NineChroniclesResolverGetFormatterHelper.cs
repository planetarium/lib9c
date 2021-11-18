using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using MessagePack.Formatters;
using Nekoyume.Action;

namespace Lib9c.Formatters
{
    public static class NineChroniclesResolverGetFormatterHelper
    {
        // If type is concrete type, use type-formatter map
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            {typeof(Address), new AddressFormatter()},
            {typeof(Exception), new ExceptionFormatter<Exception>()},
            {typeof(FungibleAssetValue), new FungibleAssetValueFormatter()},
            {typeof(IAccountStateDelta), new AccountStateDeltaFormatter()},
            {typeof(PublicKeyFormatter), new PublicKeyFormatter()},
            {typeof(Dictionary), new BencodexFormatter<Dictionary>()},
            {typeof(IValue), new BencodexFormatter<IValue>()}
            // add more your own custom serializers.
        };

        internal static object GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter))
            {
                return formatter;
            }

            // If target type is generics, use MakeGenericType.
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            {
                return Activator.CreateInstance(typeof(ValueTupleFormatter<,>).MakeGenericType(t.GenericTypeArguments));
            }

            // If type can not get, must return null for fallback mechanism.
            return null;
        }
    }
}
