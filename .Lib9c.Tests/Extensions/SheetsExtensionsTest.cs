namespace Lib9c.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Extensions;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class SheetsExtensionsTest
    {
        private IWorld _states;
        private Dictionary<string, string> _sheetNameAndFiles;
        private Dictionary<Address, IValue> _sheetsAddressAndValues;
        private Type[] _sheetTypes;
        private Dictionary<Type, (Address, ISheet)> _stateSheets;

        public SheetsExtensionsTest()
        {
            _states = new World(MockUtil.MockModernWorldState);
            InitSheets(
                _states,
                out _sheetNameAndFiles,
                out _sheetsAddressAndValues,
                out _sheetTypes,
                out _stateSheets);
        }

        [Fact]
        public void GetAddress()
        {
            foreach (var sheetType in _sheetTypes)
            {
                var address = _stateSheets.GetAddress(sheetType);
                var expectedAddress = Addresses.TableSheet.Derive(sheetType.Name);
                Assert.Equal(address, expectedAddress);
            }
        }

        [Fact]
        public void GetSheet()
        {
            foreach (var sheetType in _sheetTypes)
            {
                var sheet = _stateSheets.GetSheet(sheetType);
                Assert.NotNull(sheet);
            }
        }

        internal static void InitSheets(
            IWorld states,
            out Dictionary<string, string> sheetNameAndFiles,
            out Dictionary<Address, IValue> sheetsAddressAndValues,
            out Type[] sheetTypes,
            out Dictionary<Type, (Address Address, ISheet Sheet)> stateSheets)
        {
            sheetNameAndFiles = TableSheetsImporter.ImportSheets();
            sheetsAddressAndValues = sheetNameAndFiles.ToDictionary(
                pair => Addresses.TableSheet.Derive(pair.Key),
                pair => pair.Value.Serialize());
            foreach (var (address, value) in sheetsAddressAndValues)
            {
                states = states.SetLegacyState(address, value);
            }

            var iSheetType = typeof(ISheet);
            var sheetNameAndFilesTemp = sheetNameAndFiles;
            sheetTypes = Assembly.GetAssembly(typeof(ISheet))?.GetTypes()
                .Where(
                    type =>
                        iSheetType.IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        sheetNameAndFilesTemp.ContainsKey(type.Name))
                .ToArray();
            Assert.NotNull(sheetTypes);
            Assert.NotEmpty(sheetTypes);
            stateSheets = states.GetSheets(sheetTypes);
        }
    }
}
