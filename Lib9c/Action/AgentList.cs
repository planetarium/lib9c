using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Nekoyume.Action
{
    public static class AgentList
    {
        public const int Count = 366909;

        public static ImmutableList<string> Addresses
        {
            get
            {
                if (_addresses is null)
                {
                    var list = new List<string>();
                    var rm = new ResourceManager("Odin", Assembly.GetExecutingAssembly());

                    list.AddRange(
                        from DictionaryEntry entry in rm.GetResourceSet(CultureInfo.InvariantCulture, true, true) select entry.Value?.ToString());

                    rm = new ResourceManager("Heimdall", Assembly.GetExecutingAssembly());
                    list.AddRange(
                        from DictionaryEntry entry in rm.GetResourceSet(CultureInfo.InvariantCulture, true, true) select entry.Value?.ToString());

                    _addresses = list.ToImmutableList();
                }

                return _addresses;
            }
        }

        private static ImmutableList<string> _addresses = null;
    }
}
