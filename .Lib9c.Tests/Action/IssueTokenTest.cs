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
        private readonly Currency _runeCurrency = Currencies.GetRune("RUNE_GOLDENLEAF");

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
                    .SetBalance(_signer, Currencies.Crystal * 1000)
                    .SetBalance(_avatarAddress, _runeCurrency * 1000)
            );

            var materialIds = tableSheets.LoadIntoMyGaragesCostSheet.OrderedList!
                .Where(r => r.ItemId > 0)
                .Take(3)
                .Select(r => r.ItemId);
            var inventory = new Inventory();
            foreach (var row in tableSheets.MaterialItemSheet.OrderedList!
                .Where(r => materialIds.Contains(r.Id)))
            {
                var material = ItemFactory.CreateMaterial(row);
                var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
                inventory.AddItem(material, 500);
                inventory.AddItem(tradableMaterial, 500);
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
                Items = new List<(int id, int count, bool tradable)>
                {
                    (1, 2, false),
                    (3, 2, true),
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
            Assert.False(first.tradable);
            var last = action2.Items.Last();
            Assert.Equal(3, last.id);
            Assert.Equal(2, last.count);
            Assert.True(last.tradable);
        }

        [Fact]
        public void Execute_With_FungibleAssetValues()
        {
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                FungibleAssetValues = new List<FungibleAssetValue>
                {
                    Currencies.Crystal * 42,
                    _runeCurrency * 42,
                },
                Items = new List<(int id, int count, bool tradable)>(),
            };
            var prevState = _prevState.MintAsset(new ActionContext(), _signer, FungibleAssetValue.Parse(Currencies.Garage, "4.2000042"));

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 42,
                }
            );

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);
            var wrappedRune = Currencies.GetWrappedCurrency(_runeCurrency);

            Assert.Equal(0 * Currencies.Garage, nextState.GetBalance(_signer, Currencies.Garage));
            Assert.Equal(wrappedCrystal * 42, nextState.GetBalance(_signer, wrappedCrystal));
            Assert.Equal(
                Currencies.Crystal * (1000 - 42),
                nextState.GetBalance(
                    _signer,
                    Currencies.Crystal
                )
            );
            Assert.Equal(wrappedRune * 42, nextState.GetBalance(_signer, wrappedRune));
            Assert.Equal(
                _runeCurrency * (1000 - 42),
                nextState.GetBalance(
                    _avatarAddress,
                    _runeCurrency
                )
            );
        }

        [Fact]
        public void Execute_With_FungibleItemValue()
        {
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                Items = new List<(int id, int count, bool tradable)>
                {
                    (500000, 42, true),
                    (500000, 42, false),
                },
                FungibleAssetValues = new List<FungibleAssetValue>(),
            };
            var prevState = _prevState.MintAsset(new ActionContext(), _signer, FungibleAssetValue.Parse(Currencies.Garage, "0.84"));

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 42,
                }
            );

            Assert.Equal(0 * Currencies.Garage, nextState.GetBalance(_signer, Currencies.Garage));
            var nonTradableItemCurrency = Currency.Legacy("Item_NT_500000", 0, null);
            var tradableItemCurrency = Currency.Legacy("Item_T_500000", 0, null);
            Assert.Equal(nonTradableItemCurrency * 42, nextState.GetBalance(_signer, nonTradableItemCurrency));
            Assert.Equal(tradableItemCurrency * 42, nextState.GetBalance(_signer, tradableItemCurrency));
            var inventory = nextState.GetInventoryV2(_avatarAddress);
            const int expectedCount = 1000 - 42 * 2;
            Assert.True(inventory.HasItem(500000, expectedCount));
            var items = inventory.Items.Where(i => i.item.Id == 500000).ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal(expectedCount, items.Sum(i => i.count));
        }

        [Fact]
        public void Execute_Throw_InvalidCurrencyException()
        {
            var currencyWithMinter = Currency.Legacy("RUNESTONE_CRI", 0, _signer);
            var action = new IssueToken
            {
                AvatarAddress = _avatarAddress,
                FungibleAssetValues = new List<FungibleAssetValue>
                {
                    _runeCurrency * 42,
                    currencyWithMinter * 42,
                },
                Items = new List<(int id, int count, bool tradable)>(),
            };

            var actionContext = new ActionContext
            {
                Signer = _signer,
            };
            var prevState = _prevState
                .MintAsset(actionContext, _signer, Currencies.Garage * 1000)
                .MintAsset(actionContext, _signer, currencyWithMinter * 1000);

            Assert.Throws<InvalidCurrencyException>(() => action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 42,
                }
            ));
        }
    }
}
