namespace Lib9c.Tests.Action.Snapshot
{
    using System;
    using System.Reflection;
    using Bencodex.Types;
    using Libplanet.Action;

    public static class ActionUtils
    {
        public static IValue GetActionTypeId<T>()
            where T : IAction
        {
            var attrType = typeof(ActionTypeAttribute);
            var actionType = typeof(T);
            return actionType.GetCustomAttribute<ActionTypeAttribute>() is { } attr
                ? attr.TypeIdentifier
                : throw new ArgumentException(
                    $"The action type attribute is missing for {typeof(T)}.");
        }
    }
}
