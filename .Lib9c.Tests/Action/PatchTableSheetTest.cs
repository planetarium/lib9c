namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class PatchTableSheetTest
    {
        private IAccount _initialState;

        public PatchTableSheetTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new MockAccount();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var worldSheetCsv = _initialState.GetSheetCsv<WorldSheet>();
            var worldSheet = new WorldSheet();
            worldSheet.Set(worldSheetCsv);
            var worldSheetRowCount = worldSheet.Count;

            var worldSheetCsvColumnLine = worldSheetCsv.Split('\n').FirstOrDefault();
            Assert.NotNull(worldSheetCsvColumnLine);

            var patchTableSheetAction = new PatchTableSheet
            {
                TableName = nameof(WorldSheet),
                TableCsv = worldSheetCsvColumnLine,
            };
            var nextWorld = patchTableSheetAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousState = new MockWorld(_initialState),
                Rehearsal = false,
            });

            var nextWorldSheetCsv = nextWorld.GetAccount(ReservedAddresses.LegacyAccount)
                .GetSheetCsv<WorldSheet>();
            Assert.Single(nextWorldSheetCsv.Split('\n'));

            var nextWorldSheet = new WorldSheet();
            nextWorldSheet.Set(nextWorldSheetCsv);
            Assert.Empty(nextWorldSheet);

            patchTableSheetAction = new PatchTableSheet
            {
                TableName = nameof(WorldSheet),
                TableCsv = worldSheetCsv,
            };
            nextWorld = patchTableSheetAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousState = new MockWorld(_initialState),
                Rehearsal = false,
            });

            nextWorldSheet = nextWorld.GetAccount(ReservedAddresses.LegacyAccount)
                .GetSheet<WorldSheet>();
            Assert.Equal(worldSheetRowCount, nextWorldSheet.Count);
        }

        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            const string tableName = "TestTable";
            var initStates = MockAccountState.Empty
                .SetState(AdminState.Address, adminState.Serialize())
                .SetState(Addresses.TableSheet.Derive(tableName), Dictionary.Empty.Add(tableName, "Initial"));
            var account = new MockAccount(initStates);
            var state = new MockWorld(account);
            var action = new PatchTableSheet()
            {
                TableName = tableName,
                TableCsv = "New Value",
            };

            PolicyExpiredException exc1 = Assert.Throws<PolicyExpiredException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 101,
                        PreviousState = state,
                        Signer = adminAddress,
                    }
                );
            });
            Assert.Equal(101, exc1.BlockIndex);

            PermissionDeniedException exc2 = Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(
                    new ActionContext()
                    {
                        BlockIndex = 5,
                        PreviousState = state,
                        Signer = new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"),
                    }
                );
            });
            Assert.Equal(new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"), exc2.Signer);
        }

        [Fact]
        public void ExecuteNewTable()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            const string tableName = "TestTable";
            var initStates = MockAccountState.Empty
                .SetState(AdminState.Address, adminState.Serialize())
                .SetState(Addresses.TableSheet.Derive(tableName), Dictionary.Empty.Add(tableName, "Initial"));
            var account = new MockAccount(initStates);
            var state = new MockWorld(account);
            var action = new PatchTableSheet()
            {
                TableName = nameof(CostumeStatSheet),
                TableCsv = "id,costume_id,stat_type,stat\n1,40100000,ATK,100",
            };

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = state,
                    Signer = adminAddress,
                }
            );

            Assert.NotNull(
                nextState.GetAccount(ReservedAddresses.LegacyAccount).GetSheet<CostumeStatSheet>());
        }
    }
}
