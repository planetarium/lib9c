namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class IssueTokenTest
    {
        private readonly IWorld _prevState;
        private readonly Address _signer;
        private readonly Address _avatarAddress;

        public IssueTokenTest()
        {
            _signer = new PrivateKey().Address;
            _avatarAddress = _signer.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CreateAvatar.DeriveFormat,
                0
            ));
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);

            _prevState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_signer, Currencies.Crystal * 1000));

            var materialIds = tableSheets.LoadIntoMyGaragesCostSheet.OrderedList!
                .Where(r => r.ItemId > 0)
                .Take(3)
                .Select(r => r.ItemId);
            IEnumerable<TradableMaterial> materials = tableSheets.MaterialItemSheet.OrderedList!
                .Where(r => materialIds.Contains(r.Id))
                .Select(ItemFactory.CreateTradableMaterial);
            var inventory = new Inventory();
            foreach (TradableMaterial material in materials)
            {
                inventory.AddItem(material, 1000);
            }

            _prevState = _prevState.SetInventory(_avatarAddress, inventory);

            foreach (var (key, value) in sheets)
            {
                _prevState = _prevState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void PlainValue()
        {
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                FungibleAssetValues = new List<FungibleAssetValue>
                {
                    Currencies.Crystal * 1000,
                },
                Items = new List<(int id, int count)>
                {
                    (1, 2),
                    (3, 2),
                },
            };

            var plainValue = action.PlainValue;
            var action2 = new IssueToken();
            action2.LoadPlainValue(plainValue);
            Assert.Equal(_avatarAddress, action2.AvatarAddress);
            var fav = Assert.Single(action2.FungibleAssetValues);
            Assert.Equal(Currencies.Crystal * 1000, fav);
            Assert.Equal(2, action2.Items.Count);
            var first = action2.Items.First();
            Assert.Equal(1, first.id);
            Assert.Equal(2, first.count);
            var last = action2.Items.Last();
            Assert.Equal(3, last.id);
            Assert.Equal(2, last.count);
        }

        [Fact]
        public void Execute_With_FungibleAssetValue()
        {
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                FungibleAssetValues = new List<FungibleAssetValue>
                {
                    Currencies.Crystal * 42,
                },
                Items = new List<(int id, int count)>(),
            };
            var prevState = _prevState.MintAsset(new ActionContext(), _signer, FungibleAssetValue.Parse(Currencies.Garage, "0.0000042"));

            IWorld nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 42,
                }
            );

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);

            Assert.Equal(0 * Currencies.Garage, nextState.GetBalance(_signer, Currencies.Garage));
            Assert.Equal(wrappedCrystal * 42, nextState.GetBalance(_signer, wrappedCrystal));
            Assert.Equal(
                Currencies.Crystal * (1000 - 42),
                nextState.GetBalance(
                    _signer,
                    Currencies.Crystal
                )
            );
        }

        [Fact]
        public void Execute_With_FungibleItemValue()
        {
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                Items = new List<(int id, int count)>
                {
                    (500000, 42),
                },
                FungibleAssetValues = new List<FungibleAssetValue>(),
            };
            var prevState = _prevState.MintAsset(new ActionContext(), _signer, FungibleAssetValue.Parse(Currencies.Garage, "0.42"));

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 42,
                }
            );

            Assert.Equal(0 * Currencies.Garage, nextState.GetBalance(_signer, Currencies.Garage));
            Currency itemCurrency = Currency.Legacy("Item_T_500000", 0, null);
            Assert.Equal(itemCurrency * 42, nextState.GetBalance(_signer, itemCurrency));
            var inventory = nextState.GetInventoryV2(_avatarAddress);
            Assert.True(inventory.TryGetItem(500000, out var item));
            Assert.Equal(1000 - 42, item.count);
        }
    }
}
