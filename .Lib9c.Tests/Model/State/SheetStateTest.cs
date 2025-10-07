namespace Lib9c.Tests.Model.State
{
    using System.Linq;
    using System.Reflection;
    using Lib9c.TableData;
    using Xunit;
    using Xunit.Abstractions;

    public class SheetStateTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SheetStateTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void PrintSheetAddresses()
        {
            var assembly = Assembly.GetAssembly(typeof(ISheet));
            Assert.NotNull(assembly);

            var sheetNames = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ISheet).IsAssignableFrom(type))
                .Select(type => type.Name);
            foreach (var sheetName in sheetNames)
            {
                var address = Addresses.GetSheetAddress(sheetName);
                _testOutputHelper.WriteLine("{0}: {1}", sheetName, address.ToHex());
            }
        }
    }
}
