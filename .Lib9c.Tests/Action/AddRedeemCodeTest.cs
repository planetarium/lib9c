namespace Lib9c.Tests.Action
{
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class AddRedeemCodeTest
    {
        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, adminState.Serialize());
            var action = new AddRedeemCode
            {
                redeemCsv = "New Value",
            };

            var exc1 = Assert.Throws<PolicyExpiredException>(
                () =>
                {
                    action.Execute(
                        new ActionContext
                        {
                            BlockIndex = 101,
                            PreviousState = state,
                            Signer = adminAddress,
                        }
                    );
                });
            Assert.Equal(101, exc1.BlockIndex);

            var exc2 = Assert.Throws<PermissionDeniedException>(
                () =>
                {
                    action.Execute(
                        new ActionContext
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
        public void Execute()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);

            var csv = TableSheetsImporter.ImportSheets()[nameof(RedeemCodeListSheet)];

            var action = new AddRedeemCode
            {
                redeemCsv = csv,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    Signer = adminAddress,
                    BlockIndex = 0,
                    PreviousState = new World(MockUtil.MockModernWorldState)
                        .SetLegacyState(Addresses.Admin, adminState.Serialize())
                        .SetLegacyState(Addresses.RedeemCode, new RedeemCodeState(new RedeemCodeListSheet()).Serialize()),
                });

            var sheet = new RedeemCodeListSheet();
            sheet.Set(csv);
            var expectedMap = new RedeemCodeState(sheet).Map;
            var redeemState = nextState.GetRedeemCodeState();
            foreach (var (key, reward) in expectedMap)
            {
                Assert.Equal(reward.RewardId, redeemState.Map[key].RewardId);
            }
        }

        [Fact]
        public void ExecuteThrowSheetRowValidateException()
        {
            var csv = TableSheetsImporter.ImportSheets()[nameof(RedeemCodeListSheet)];
            var sheet = new RedeemCodeListSheet();
            sheet.Set(csv);

            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.RedeemCode, new RedeemCodeState(sheet).Serialize());

            var action = new AddRedeemCode
            {
                redeemCsv = csv,
            };

            Assert.Throws<SheetRowValidateException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = 0,
                        PreviousState = state,
                    })
            );
        }
    }
}
