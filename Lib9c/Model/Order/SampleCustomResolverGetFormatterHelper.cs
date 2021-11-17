using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using MessagePack.Formatters;

namespace Lib9c.Model.Order
{
    public static class SampleCustomResolverGetFormatterHelper
    {
        // If type is concrete type, use type-formatter map
        static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
        {
            {typeof(Address), new AddressFormatter()},
            {typeof(Exception), new ExceptionFormatter<Exception>()},
            {typeof(IValue), new BencodexFormatter()},
            {typeof(FungibleAssetValue), new FungibleAssetValueFormatter()},
            {typeof(IAccountStateDelta), new AccountStateDeltaFormatter()}
            // add more your own custom serializers.
        };

        internal static object GetFormatter(Type t)
        {
            object formatter;
            if (formatterMap.TryGetValue(t, out formatter))
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
