#nullable enable
using System;
using System.Linq;
using System.Reflection;

namespace Nekoyume.Model
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class VersionedStateAttribute : Attribute
    {
        public string Moniker { get; }

        public uint Version { get; }

        public VersionedStateAttribute(string moniker, uint version)
        {
            if (string.IsNullOrEmpty(moniker))
            {
                throw new ArgumentException("Moniker must not be null or empty.");
            }

            Moniker = moniker;
            Version = version;
        }

        public static bool TryGetFrom(Type type, out VersionedStateAttribute? attr)
        {
            try
            {
                attr = type
                    .GetCustomAttributes()
                    .OfType<VersionedStateAttribute>()
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
