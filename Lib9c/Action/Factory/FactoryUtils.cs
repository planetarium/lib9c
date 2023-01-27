using System;
using System.Linq;
using System.Reflection;
using Libplanet.Action;

namespace Nekoyume.Action.Factory
{
    public static class FactoryUtils
    {
        public static (Type type, string actionType)[] GetTuples<T>()
            where T : IAction
        {
            var t1 = typeof(T);
            var t2 = typeof(ActionTypeAttribute);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.GetInterfaces().Contains(t1))
                .Select(t =>
                {
                    var actionType = t.GetCustomAttribute(t2) as ActionTypeAttribute;
                    return (type: t, actionType: actionType?.TypeIdentifier);
                })
                .Where(tuple => !string.IsNullOrEmpty(tuple.actionType))
                .ToArray();
        }
    }
}
