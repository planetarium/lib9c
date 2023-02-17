#nullable enable
using System;
using System.Linq;
using System.Reflection;

namespace Nekoyume.Model
{
    /// <summary>
    /// Implementation attribute of <see cref="VersionedStateAttribute"/>.
    /// The class or struct that has this attribute must implement parameterless constructor
    /// and <see cref="State.IState"/>.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = true)]
    public class VersionedStateImplAttribute : Attribute
    {
        public Type Type { get; }

        public VersionedStateImplAttribute(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException("Type must not be null.");
            }

            Type = type;
        }

        public static bool TryGetFrom(Type type, out VersionedStateImplAttribute? attr)
        {
            try
            {
                attr = type
                    .GetCustomAttributes()
                    .OfType<VersionedStateImplAttribute>()
                    .First();
                return true;
            }
            catch
            {
                attr = null;
                return false;
            }
        }
    }
}
