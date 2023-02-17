namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Fixture.States;
    using Nekoyume.Model;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class VersionedStateAttributeTest
    {
        public VersionedStateAttributeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Constructor()
        {
            var attr = new VersionedStateAttribute("moniker", 1);
            Assert.Equal("moniker", attr.Moniker);
            Assert.Equal(1u, attr.Version);
        }

        [Fact]
        public void TryGetFrom()
        {
            var type = typeof(ITestStateV1);
            Assert.True(VersionedStateAttribute.TryGetFrom(type, out var attr));
            Assert.Equal(ITestStateV1.Moniker, attr!.Moniker);
            Assert.Equal(ITestStateV1.Version, attr.Version);
        }

        [Fact]
        public void UniqueMonikerAndVersion()
        {
            var tuples = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Select(type =>
                {
                    var attribute = type.GetCustomAttributes(true)
                        .OfType<VersionedStateAttribute>()
                        .FirstOrDefault();
                    return (type, attribute);
                })
                .Where(tuple => tuple.attribute is { })
                .ToArray();
            var monikerAndVersions = new Dictionary<string, Dictionary<uint, Type>>();
            var errors = new Dictionary<string, Dictionary<uint, List<Type>>>();
            foreach (var (type, attribute) in tuples)
            {
                Assert.NotNull(attribute);
                if (!monikerAndVersions.ContainsKey(attribute.Moniker))
                {
                    monikerAndVersions[attribute.Moniker] = new Dictionary<uint, Type>
                    {
                        { attribute.Version, type },
                    };

                    continue;
                }

                if (monikerAndVersions[attribute.Moniker].ContainsKey(attribute.Version))
                {
                    var prevType = monikerAndVersions[attribute.Moniker][attribute.Version];
                    if (!errors.ContainsKey(attribute.Moniker))
                    {
                        errors[attribute.Moniker] = new Dictionary<uint, List<Type>>
                        {
                            { attribute.Version, new List<Type> { prevType, type } },
                        };

                        continue;
                    }

                    if (!errors[attribute.Moniker].ContainsKey(attribute.Version))
                    {
                        errors[attribute.Moniker][attribute.Version] = new List<Type> { type };

                        continue;
                    }

                    errors[attribute.Moniker][attribute.Version].Add(type);
                }

                monikerAndVersions[attribute.Moniker][attribute.Version] = type;
            }

            if (errors.Any())
            {
                var message = string.Join(
                    Environment.NewLine,
                    errors.Select(kv =>
                        $"\"{kv.Key}\"{string.Join(", ", kv.Value.Select(kv2 => $"({kv2.Key}): {string.Join(", ", kv2.Value)}"))}"));
                Assert.True(false, message);
            }
        }
    }
}
