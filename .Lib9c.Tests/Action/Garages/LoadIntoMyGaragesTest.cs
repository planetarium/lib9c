namespace Lib9c.Tests.Action.Garages
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Abstractions;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.Garages;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class LoadIntoMyGaragesTest
    {
        private const int AvatarIndex = 0;
        private static readonly Address AgentAddr = Addresses.Admin;

        private readonly TableSheets _tableSheets;
        private readonly Address _avatarAddress;
        private readonly IWorld _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;
        private readonly (Address balanceAddr, FungibleAssetValue value)[] _fungibleAssetValues;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly FungibleAssetValue _cost;
        private readonly IWorld _previousStates;
        private readonly IFungibleItem[] _fungibleItems;

        private readonly int[] _nonTradableIds = new[]
        {
            600201,
            800201,
            800202,
        };

        public LoadIntoMyGaragesTest()
        {
            // NOTE: Garage actions does not consider the avatar state v1.
            (
                _tableSheets,
                _,
                _avatarAddress,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates(
                agentAddr: AgentAddr,
                avatarIndex: AvatarIndex);
            _ncg = _initialStatesWithAvatarStateV2.GetGoldCurrency();
            (
                _fungibleAssetValues,
                _fungibleIdAndCounts,
                _cost,
                _fungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var avatarAddr = Addresses.GetAvatarAddress(AgentAddr, AvatarIndex);
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddr, avatarAddr);
            var inventoryAddr = Addresses.GetInventoryAddress(Addresses.Admin, AvatarIndex);

            var hex = string.Join(
                string.Empty,
                Enumerable.Range(0, 64).Select(i => (i % 10).ToString()));
            var fungibleIdAndCounts = new[]
            {
                (HashDigest<SHA256>.FromString(hex), 1),
                (HashDigest<SHA256>.FromString(hex), int.MaxValue),
            };

            yield return new object[]
            {
                fungibleAssetValues,
                inventoryAddr,
                fungibleIdAndCounts,
                "memo",
            };
        }

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            Address inventoryAddr,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            string? memo)
        {
            var actions = new[]
            {
                new LoadIntoMyGarages(),
                new LoadIntoMyGarages(
                    fungibleAssetValues,
                    inventoryAddr,
                    fungibleIdAndCounts,
                    memo),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new LoadIntoMyGarages();
                des.LoadPlainValue(ser);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                    des.FungibleAssetValues is null);
                Assert.Equal(action.AvatarAddr, des.AvatarAddr);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                    des.FungibleIdAndCounts is null);
                Assert.Equal(action.Memo, des.Memo);
                Assert.Equal(ser, des.PlainValue);

                var actionInter = (ILoadIntoMyGaragesV1)action;
                var desInter = (ILoadIntoMyGaragesV1)des;
                Assert.True(
                    actionInter.FungibleAssetValues?.SequenceEqual(desInter.FungibleAssetValues!) ??
                    desInter.FungibleAssetValues is null);
                Assert.Equal(actionInter.AvatarAddr, desInter.AvatarAddr);
                Assert.True(
                    actionInter.FungibleIdAndCounts?.SequenceEqual(desInter.FungibleIdAndCounts!) ??
                    desInter.FungibleIdAndCounts is null);
                Assert.Equal(actionInter.Memo, desInter.Memo);
            }
        }

        [Fact]
        public void Execute_Success()
        {
            var (action, nextStates) = Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _fungibleAssetValues,
                _avatarAddress,
                _fungibleIdAndCounts,
                "memo");
            Assert.Equal(
                new FungibleAssetValue(Currencies.Garage),
                nextStates.GetBalance(AgentAddr, Currencies.Garage));
            Assert.Equal(
                _cost,
                nextStates.GetBalance(Addresses.GarageWallet, Currencies.Garage));
            var garageBalanceAddr =
                Addresses.GetGarageBalanceAddress(AgentAddr);
            if (action.FungibleAssetValues is not null)
            {
                foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                {
                    Assert.Equal(
                        value.Currency * 0,
                        nextStates.GetBalance(balanceAddr, value.Currency));
                    Assert.Equal(
                        value,
                        nextStates.GetBalance(garageBalanceAddr, value.Currency));
                }
            }

            if (action.AvatarAddr is null ||
                action.FungibleIdAndCounts is null)
            {
                return;
            }

            var avatarState = nextStates.GetAvatarState(action.AvatarAddr.Value)!;
            var inventory = avatarState.inventory;
            foreach (var (fungibleId, count) in action.FungibleIdAndCounts)
            {
                Assert.False(inventory.HasFungibleItem(
                    fungibleId,
                    0,
                    1));
                var garageAddr = Addresses.GetGarageAddress(
                    AgentAddr,
                    fungibleId);
                var garage = new FungibleItemGarage(nextStates.GetLegacyState(garageAddr));
                Assert.Equal(fungibleId, garage.Item.FungibleId);
                Assert.Equal(count, garage.Count);
                Assert.IsType<Material>(garage.Item);
            }
        }

        [Fact]
        public void Execute_Throws_InvalidActionFieldException()
        {
            // FungibleAssetValues and FungibleIdAndCounts are null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                null,
                null));

            // Signer does not have permission of balance address.
            var invalidSignerAddr = new PrivateKey().Address;
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                invalidSignerAddr,
                0,
                _previousStates,
                new TestRandom(),
                _fungibleAssetValues,
                null,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(tuple => (
                tuple.balanceAddr,
                tuple.value * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                negativeFungibleAssetValues,
                null,
                null));

            // InventoryAddr is null when FungibleIdAndCounts is not null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                null,
                _fungibleIdAndCounts));

            // AgentAddr does not have permission of inventory address.
            var invalidInventoryAddr = new PrivateKey().Address;
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                invalidInventoryAddr,
                _fungibleIdAndCounts));

            // Count of fungible id is negative.
            var negativeFungibleIdAndCounts = _fungibleIdAndCounts.Select(tuple => (
                tuple.fungibleId,
                tuple.count * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                AgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                _avatarAddress,
                negativeFungibleIdAndCounts));
        }

        [Fact]
        public void Execute_Throws_Exception()
        {
            // Balance does not enough to pay cost.
            var balance = _previousStates.GetBalance(AgentAddr, Currencies.Garage);
            var previousStatesWithNotEnoughCost = _previousStates.BurnAsset(
                new ActionContext { Signer = AgentAddr, },
                AgentAddr,
                balance);
            Assert.Throws<InsufficientBalanceException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNotEnoughCost,
                new TestRandom(),
                _fungibleAssetValues,
                _avatarAddress,
                _fungibleIdAndCounts));

            // Balance does not enough to send.
            var previousStatesWithEmptyBalances = _previousStates;
            foreach (var (balanceAddr, value) in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances.BurnAsset(
                    new ActionContext { Signer = AgentAddr, },
                    balanceAddr,
                    value);
            }

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithEmptyBalances,
                new TestRandom(),
                _fungibleAssetValues,
                null,
                null));

            AvatarState avatarState;

            // Inventory does not contain the tradable fungible item.
            avatarState = _previousStates.GetAvatarState(_avatarAddress);
            avatarState.inventory = new Inventory();
            var previousStatesWithEmptyInventoryState =
                _previousStates.SetAvatarState(_avatarAddress, avatarState);
            Assert.Throws<ItemNotFoundException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithEmptyInventoryState,
                new TestRandom(),
                null,
                _avatarAddress,
                _fungibleIdAndCounts));

            // Inventory does not have enough tradable fungible item.
            avatarState = _previousStates.GetAvatarState(_avatarAddress);
            foreach (var (fungibleId, count) in _fungibleIdAndCounts)
            {
                avatarState.inventory.RemoveTradableFungibleItem(
                    fungibleId,
                    null,
                    0,
                    count - 1);
            }

            var previousStatesWithNotEnoughInventoryState =
                _previousStates.SetAvatarState(_avatarAddress, avatarState);
            Assert.Throws<NotEnoughItemException>(() => Execute(
                AgentAddr,
                0,
                previousStatesWithNotEnoughInventoryState,
                new TestRandom(),
                null,
                _avatarAddress,
                _fungibleIdAndCounts));

            // Fungible item garage's item mismatch with fungible id.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(AgentAddr, fungibleId);
                var nextIndex = (i + 1) % _fungibleIdAndCounts.Length;
                var garage = new FungibleItemGarage(_fungibleItems[nextIndex], 1);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetLegacyState(addr, garage.Serialize());
                Assert.Throws<Exception>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    null,
                    _avatarAddress,
                    _fungibleIdAndCounts));
            }

            // Fungible item garages can be overflowed.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var (fungibleId, _) = _fungibleIdAndCounts[i];
                var addr = Addresses.GetGarageAddress(AgentAddr, fungibleId);
                var garage = new FungibleItemGarage(_fungibleItems[i], int.MaxValue);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetLegacyState(addr, garage.Serialize());
                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    AgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    null,
                    _avatarAddress,
                    _fungibleIdAndCounts));
            }
        }

        private static (LoadIntoMyGarages action, IWorld nextStates) Execute(
            Address signer,
            long blockIndex,
            IWorld previousState,
            IRandom random,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? avatarAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo = null)
        {
            var action = new LoadIntoMyGarages(
                fungibleAssetValues,
                avatarAddr,
                fungibleIdAndCounts,
                memo);
            var context = new ActionContext
            {
                Signer = signer,
                BlockIndex = blockIndex,
                PreviousState = previousState,
                RandomSeed = random.Seed,
            };
            return (action, action.Execute(context));
        }

        private static (Address balanceAddr, FungibleAssetValue value)[]
            GetFungibleAssetValues(
                Address agentAddr,
                Address avatarAddr,
                TableSheets? tableSheets = null)
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav =>
                    (tableSheets?.LoadIntoMyGaragesCostSheet.HasCost(fav.Currency.Ticker) ??
                        true) &&
                    fav.Sign > 0)
                .Select(fav =>
                {
                    // CRYSTAL's minorUnit is not actually used in network, avoid cost calculate exception in test.
                    var value = 1 * fav.Currency;
                    var recipient = Currencies.PickAddress(fav.Currency, agentAddr, avatarAddr);
                    return (recipient, value);
                })
                .ToArray();
        }

        private (
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            FungibleAssetValue cost,
            IFungibleItem[] _fungibleItems,
            IWorld previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var fungibleAssetValues = GetFungibleAssetValues(
                AgentAddr,
                _avatarAddress,
                _tableSheets);
            var actionContext = new ActionContext { Signer = Addresses.Admin, };
            foreach (var (balanceAddr, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        actionContext,
                        Addresses.Admin,
                        balanceAddr,
                        value);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    actionContext,
                    balanceAddr,
                    value);
            }

            var avatarState = previousStates.GetAvatarState(_avatarAddress);
            var inventory = avatarState.inventory;
            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Where(row => _tableSheets.LoadIntoMyGaragesCostSheet.HasCost(row.ItemId))
                .Select(row => _nonTradableIds.Contains(row.Id)
                    ? ItemFactory.CreateMaterial(row)
                    : ItemFactory.CreateTradableMaterial(row))
                .Select((material, index) =>
                {
                    inventory.AddFungibleItem((ItemBase)material, index + 1);
                    return (
                        fungibleItem: (IFungibleItem)material,
                        count: index + 1);
                }).ToArray();
            var garageCost = _tableSheets.LoadIntoMyGaragesCostSheet.GetGarageCost(
                fungibleAssetValues.Select(tuple => tuple.value),
                fungibleItemAndCounts
                    .Select(tuple => (tuple.fungibleItem.FungibleId, tuple.count)));
            previousStates = previousStates.MintAsset(
                new ActionContext { Signer = AgentAddr, },
                AgentAddr,
                garageCost);
            return (
                fungibleAssetValues,
                fungibleItemAndCounts
                    .Select(tuple => (tuple.fungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                garageCost,
                fungibleItemAndCounts.Select(tuple => tuple.fungibleItem).ToArray(),
                previousStates.SetAvatarState(_avatarAddress, avatarState)
            );
        }
    }
}
