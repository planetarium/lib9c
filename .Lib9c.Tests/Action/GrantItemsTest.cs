namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="GrantItems"/>.
    /// </summary>
    public class GrantItemsTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly IWorld _initialState;

        public GrantItemsTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var agentKey = new PrivateKey();
            _agentAddress = agentKey.Address;
            _avatarAddress = _agentAddress.Derive("avatar");

            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _avatarAddress.Derive("ranking_map")
            );

            var state = new World(MockUtil.MockModernWorldState)
                .SetAvatarState(_avatarAddress, _avatarState);

            foreach (var (key, value) in _sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _initialState = state;
        }

        [Fact]
        public void Execute_UnauthorizedSigner_ThrowsInvalidMinterException_WhenNoAdminState()
        {
            var signer = new PrivateKey().Address;
            var action = new GrantItems();

            Assert.Throws<InvalidMinterException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = signer,
                    RandomSeed = 0,
                    BlockIndex = 1,
                }));
        }

        [Fact]
        public void Execute_UnauthorizedSigner_ThrowsPermissionDeniedException_WhenAdminStateExists()
        {
            var admin = new PrivateKey().Address;
            var signer = new PrivateKey().Address;
            var policy = new AdminState(admin, validUntil: 100);
            var state = _initialState.SetLegacyState(Addresses.Admin, policy.Serialize());

            var action = new GrantItems();

            Assert.Throws<PermissionDeniedException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = signer,
                    RandomSeed = 0,
                    BlockIndex = 1,
                }));
        }

        [Fact]
        public void Execute_AllowsAdminSigner_WhenAdminStateIsValid()
        {
            var admin = new PrivateKey().Address;
            var policy = new AdminState(admin, validUntil: 100);
            var state = _initialState.SetLegacyState(Addresses.Admin, policy.Serialize());

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);
            var requested = FungibleAssetValue.FromRawValue(wrappedCrystal, 10);
            var action = new GrantItems(
                new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (_avatarAddress, new List<FungibleAssetValue> { requested }),
                },
                memo: "memo"
            );

            var result = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = admin,
                RandomSeed = 0,
                BlockIndex = 1,
            });

            Assert.Equal(
                10,
                result.GetBalance(_agentAddress, Currencies.Crystal).RawValue);
        }

        [Fact]
        public void Execute_ForceGrant_GrantsAllEvenWhenSignerHasZeroTokens()
        {
            var signer = new Address("Cb75C84D76A6f97A2d55882Aea4436674c288673");

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);
            var requestedCrystalToken = FungibleAssetValue.FromRawValue(wrappedCrystal, 10);

            var materialRow = _tableSheets.MaterialItemSheet.OrderedList!.First();
            var itemCurrency = Currencies.GetItemCurrency(materialRow.Id, tradable: false);
            var requestedItemToken = 3 * itemCurrency;

            var action = new GrantItems(
                new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (_avatarAddress, new List<FungibleAssetValue> { requestedCrystalToken, requestedItemToken }),
                },
                memo: "memo"
            );

            var result = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = signer,
                RandomSeed = 0,
                BlockIndex = 1,
            });

            // The signer had zero tokens, so no burn occurs.
            Assert.Equal(0, result.GetBalance(signer, wrappedCrystal).RawValue);
            Assert.Equal(0, result.GetBalance(signer, itemCurrency).RawValue);

            // The recipient still receives the full grant.
            Assert.Equal(10, result.GetBalance(_agentAddress, Currencies.Crystal).RawValue);

            var updatedAvatarState = result.GetAvatarState(_avatarAddress, getQuestList: false, getWorldInformation: false);
            var materialCount = updatedAvatarState.inventory.Items
                .Where(i => i.item is Material m && m.Id == materialRow.Id)
                .Sum(i => i.count);
            Assert.Equal(3, materialCount);
        }

        [Fact]
        public void Execute_ForceGrant_BurnsOnlyAvailableTokens_WhenSignerBalanceIsInsufficient()
        {
            var signer = new Address("Cb75C84D76A6f97A2d55882Aea4436674c288673");

            var wrappedCrystal = Currencies.GetWrappedCurrency(Currencies.Crystal);
            var requestedCrystalToken = FungibleAssetValue.FromRawValue(wrappedCrystal, 10);

            var materialRow = _tableSheets.MaterialItemSheet.OrderedList!.First();
            var itemCurrency = Currencies.GetItemCurrency(materialRow.Id, tradable: false);
            var requestedItemToken = 3 * itemCurrency;

            // Give the signer less than requested.
            var state = _initialState
                .MintAsset(new ActionContext(), signer, FungibleAssetValue.FromRawValue(wrappedCrystal, 5))
                .MintAsset(new ActionContext(), signer, 1 * itemCurrency);

            var action = new GrantItems(
                new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (_avatarAddress, new List<FungibleAssetValue> { requestedCrystalToken, requestedItemToken }),
                },
                memo: "memo"
            );

            var result = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = signer,
                RandomSeed = 0,
                BlockIndex = 1,
            });

            // Only the available amounts are burned.
            Assert.Equal(0, result.GetBalance(signer, wrappedCrystal).RawValue);
            Assert.Equal(0, result.GetBalance(signer, itemCurrency).RawValue);

            // The recipient still receives the full grant.
            Assert.Equal(10, result.GetBalance(_agentAddress, Currencies.Crystal).RawValue);

            var updatedAvatarState = result.GetAvatarState(_avatarAddress, getQuestList: false, getWorldInformation: false);
            var materialCount = updatedAvatarState.inventory.Items
                .Where(i => i.item is Material m && m.Id == materialRow.Id)
                .Sum(i => i.count);
            Assert.Equal(3, materialCount);
        }
    }
}
