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

        public static T CreateInstance<T>(
            string actionTypeIdentifier,
            (Type type, string actionTypeIdentifier)[] tuples)
        {
            if (string.IsNullOrEmpty(actionTypeIdentifier))
            {
                throw new NotMatchFoundException(
                    typeof(T),
                    actionTypeIdentifier);
            }

            var (type, _) = tuples.FirstOrDefault(tuple =>
                tuple.actionTypeIdentifier == actionTypeIdentifier);
            if (type is null)
            {
                throw new NotMatchFoundException(
                    typeof(T),
                    actionTypeIdentifier);
            }

            try
            {
                return (T)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new NotMatchFoundException(
                    typeof(T),
                    actionTypeIdentifier,
                    e);
            }
        }

        public static void SetField(
            object action,
            Type type,
            string fieldName,
            object value)
        {
            var itemIdFi = type.GetField(
                fieldName,
                BindingFlags.Public |
                BindingFlags.SetField |
                BindingFlags.Instance);
            if (itemIdFi is null)
            {
                throw new NullReferenceException(
                    $"Field {fieldName} is not found in {type.FullName}.");
            }

            itemIdFi.SetValue(action, value);
        }
    }
}
