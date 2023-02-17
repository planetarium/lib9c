namespace Lib9c.Tests.Model
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Fixture.States;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class VersionedStateImplAttributeTest
    {
        public VersionedStateImplAttributeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Constructor()
        {
            var type = typeof(ITestStateV1);
            var attr = new VersionedStateImplAttribute(type);
            Assert.Equal(type, attr.Type);
        }

        [Fact]
        public void TryGetFrom()
        {
            var type = typeof(TestStateV1);
            Assert.True(VersionedStateImplAttribute.TryGetFrom(type, out var attr));
            Assert.Equal(typeof(ITestStateV1), attr!.Type);
        }

        [Fact]
        public void CheckInheritIState()
        {
            var tuples = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(type =>
                {
                    var attr = type.GetCustomAttributes(true)
                        .OfType<VersionedStateImplAttribute>()
                        .FirstOrDefault();
                    return (type, attr);
                })
                .Where(tuple => tuple.attr is { })
                .ToArray();
            var hasError = false;
            foreach (var (type, _) in tuples)
            {
                if (!type.IsAssignableTo(typeof(IState)))
                {
                    Log.Debug(
                        "Type {Type} is not assignable to {AssignableTo}",
                        type,
                        typeof(IState));
                    hasError = true;
                }
            }

            Assert.False(hasError);
        }
    }
}
