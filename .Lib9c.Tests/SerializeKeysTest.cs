namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class SerializeKeysTest
    {
        [Fact]
        public void Keys_Duplicate()
        {
            var type = typeof(SerializeKeys);
            var fields = type.GetFields(BindingFlags.Public
                | BindingFlags.Static
                | BindingFlags.FlattenHierarchy);
            var keyMap = new Dictionary<string, string>();
            foreach (var info in fields.Where(fieldInfo => fieldInfo.IsLiteral && !fieldInfo.IsInitOnly))
            {
                var key = (string)info.GetValue(type);
                Assert.NotNull(key);
                var value = info.Name;
                if (keyMap.ContainsKey(key))
                {
                    throw new Exception($"`{info.Name}`s value `{key}` is already used in {keyMap[key]}.");
                }

                keyMap[key] = value;
            }
        }
    }
}
