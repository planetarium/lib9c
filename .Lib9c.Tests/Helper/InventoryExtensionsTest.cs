namespace Lib9c.Tests.Helper
{
    using System;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Helper;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class InventoryExtensionsTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IWorld _state;

        public InventoryExtensionsTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _state = new World(MockUtil.MockModernWorldState);
            foreach (var kv in sheets)
            {
                _state = _state.SetLegacyState(Addresses.GetSheetAddress(kv.Key), kv.Value.Serialize());
            }
        }

        [Theory]
        [InlineData(0, 5, false, false, typeof(NotEnoughActionPointException))]
        [InlineData(0, 5, true, false, typeof(NotEnoughMaterialException))]
        [InlineData(120, 5, false, true, null)]
        [InlineData(120, 5, false, false, null)]
        [InlineData(120, 5, true, false, null)]
        [InlineData(120, 5, true, true, null)]
        public void UseAp(int ap, int requiredAp, bool chargeAp, bool materialExist, Type exc)
        {
            var avatarAddress = new PrivateKey().Address;
            var inventory = new Inventory();
            var row = _tableSheets.MaterialItemSheet.Values.First(
                r =>
                    r.ItemSubType == ItemSubType.ApStone);
            if (materialExist)
            {
                var apStone = ItemFactory.CreateMaterial(row);
                inventory.AddItem(apStone);
            }

            Assert.Equal(inventory.HasItem(row.Id), materialExist);
            if (exc is null)
            {
                var resultActionPoint = inventory.UseActionPoint(ap, requiredAp, chargeAp, _tableSheets.MaterialItemSheet, 0L);
                Assert.Equal(materialExist, inventory.TryGetItem(row.Id, out var inventoryItem));
                if (materialExist)
                {
                    Assert.Equal(1, inventoryItem.count);
                }

                Assert.Equal(DailyReward.ActionPointMax - requiredAp, resultActionPoint);
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => inventory.UseActionPoint(ap, requiredAp, chargeAp, _tableSheets.MaterialItemSheet, 0L)
                );
            }
        }
    }
}
