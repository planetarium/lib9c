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
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Libplanet.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Garages;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Xunit;

    public class TransferFromGaragesTest
    {
        private static readonly Address SenderAgentAddr = new PrivateKey().ToAddress();
        private static readonly Address RecipientAgentAddr = new PrivateKey().ToAddress();
        private static readonly int RecipientAvatarIndex = 0;

        private static readonly Address RecipientAvatarAddr =
            Addresses.GetAvatarAddress(RecipientAgentAddr, RecipientAvatarIndex);

        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;
        private readonly (Address balanceAddr, FungibleAssetValue value)[] _fungibleAssetValues;
        private readonly Address? _recipientInventoryAddr;
        private readonly (HashDigest<SHA256> fungibleId, int count)[] _fungibleIdAndCounts;
        private readonly ITradableFungibleItem[] _tradableFungibleItems;
        private readonly IAccountStateDelta _previousStates;

        public TransferFromGaragesTest()
        {
            // NOTE: Garage actions does not consider the avatar state v1.
            (
                _tableSheets,
                _,
                _,
                _,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates(
                agentAddr: RecipientAgentAddr,
                avatarIndex: RecipientAvatarIndex);
            _ncg = _initialStatesWithAvatarStateV2.GetGoldCurrency();
            (
                _fungibleAssetValues,
                _recipientInventoryAddr,
                _fungibleIdAndCounts,
                _tradableFungibleItems,
                _previousStates
            ) = GetSuccessfulPreviousStatesWithPlainValue();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue() =>
            TransferToGaragesTest.Get_Sample_PlainValue();

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? inventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            var actions = new[]
            {
                new TransferFromGarages(),
                new TransferFromGarages(
                    fungibleAssetValues,
                    inventoryAddr,
                    fungibleIdAndCounts),
            };
            foreach (var action in actions)
            {
                var ser = action.PlainValue;
                var des = new TransferFromGarages();
                des.LoadPlainValue(ser);
                Assert.True(action.FungibleAssetValues?.SequenceEqual(des.FungibleAssetValues!) ??
                            des.FungibleAssetValues is null);
                Assert.Equal(action.RecipientInventoryAddr, des.RecipientInventoryAddr);
                Assert.True(action.FungibleIdAndCounts?.SequenceEqual(des.FungibleIdAndCounts!) ??
                            des.FungibleIdAndCounts is null);
                Assert.Equal(ser, des.PlainValue);

                var actionInter = (ITransferFromGarages)action;
                var desInter = (ITransferFromGarages)des;
                Assert.True(
                    actionInter.FungibleAssetValues?.SequenceEqual(desInter.FungibleAssetValues!) ??
                    desInter.FungibleAssetValues is null);
                Assert.Equal(actionInter.RecipientInventoryAddr, desInter.RecipientInventoryAddr);
                Assert.True(
                    actionInter.FungibleIdAndCounts?.SequenceEqual(desInter.FungibleIdAndCounts!) ??
                    desInter.FungibleIdAndCounts is null);
            }
        }

        [Fact]
        public void Execute_Success()
        {
            var (action, nextStates) = Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                _fungibleAssetValues,
                _recipientInventoryAddr,
                _fungibleIdAndCounts);
            var garageBalanceAddr =
                Addresses.GetGarageBalanceAddress(SenderAgentAddr);
            if (action.FungibleAssetValues is { })
            {
                foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                {
                    Assert.Equal(
                        value,
                        nextStates.GetBalance(balanceAddr, value.Currency));
                    Assert.Equal(
                        value.Currency * 0,
                        nextStates.GetBalance(garageBalanceAddr, value.Currency));
                }
            }

            if (action.RecipientInventoryAddr is null ||
                action.FungibleIdAndCounts is null)
            {
                return;
            }

            var inventoryState = nextStates.GetState(action.RecipientInventoryAddr.Value)!;
            var inventory = new Inventory((List)inventoryState);
            foreach (var (fungibleId, count) in action.FungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                Assert.True(nextStates.GetState(garageAddr) is Null);
                Assert.True(inventory.HasTradableFungibleItem(
                    fungibleId,
                    requiredBlockIndex: null,
                    blockIndex: 0,
                    count));
            }
        }

        [Fact]
        public void Execute_Throws_InvalidActionFieldException()
        {
            // FungibleAssetValues and FungibleIdAndCounts are null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                _recipientInventoryAddr,
                null));

            // FungibleAssetValues contains negative value.
            var negativeFungibleAssetValues = _fungibleAssetValues.Select(tuple =>
                (tuple.balanceAddr, tuple.value * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                negativeFungibleAssetValues,
                _recipientInventoryAddr,
                null));

            // RecipientInventoryAddr is null when FungibleIdAndCounts is not null.
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                null,
                _fungibleIdAndCounts));

            // Count of fungible id is negative.
            var negativeFungibleIdAndCounts = _fungibleIdAndCounts.Select(tuple => (
                tuple.fungibleId,
                tuple.count * -1));
            Assert.Throws<InvalidActionFieldException>(() => Execute(
                SenderAgentAddr,
                0,
                _previousStates,
                new TestRandom(),
                null,
                _recipientInventoryAddr,
                negativeFungibleIdAndCounts));
        }

        [Fact]
        public void Execute_Throws_Exception()
        {
            // Sender's FungibleAssetValue garage does not have enough balance.
            var previousStatesWithEmptyBalances = _previousStates;
            var senderFungibleAssetValueGarageAddr =
                Addresses.GetGarageBalanceAddress(SenderAgentAddr);
            foreach (var (_, value) in _fungibleAssetValues)
            {
                previousStatesWithEmptyBalances = previousStatesWithEmptyBalances
                    .BurnAsset(senderFungibleAssetValueGarageAddr, value);
            }

            Assert.Throws<InsufficientBalanceException>(() => Execute(
                SenderAgentAddr,
                0,
                previousStatesWithEmptyBalances,
                new TestRandom(),
                _fungibleAssetValues,
                null,
                null));

            // Inventory state is null.
            var previousStatesWithNullInventoryState =
                _previousStates.SetState(_recipientInventoryAddr!.Value, Null.Value);
            Assert.Throws<StateNullException>(() => Execute(
                SenderAgentAddr,
                0,
                previousStatesWithNullInventoryState,
                new TestRandom(),
                null,
                _recipientInventoryAddr,
                _fungibleIdAndCounts));

            // The state in InventoryAddr is not Inventory.
            foreach (var invalidInventoryState in new IValue[]
                     {
                         new Integer(0),
                         Dictionary.Empty,
                     })
            {
                var previousStatesWithInvalidInventoryState =
                    _previousStates.SetState(_recipientInventoryAddr.Value, invalidInventoryState);
                Assert.Throws<InvalidCastException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithInvalidInventoryState,
                    new TestRandom(),
                    null,
                    _recipientInventoryAddr,
                    _fungibleIdAndCounts));
            }

            // Sender's fungible item garage state is null.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                var previousStatesWithNullGarageState =
                    _previousStates.SetState(garageAddr, Null.Value);
                Assert.Throws<StateNullException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithNullGarageState,
                    new TestRandom(),
                    null,
                    _recipientInventoryAddr,
                    _fungibleIdAndCounts));
            }

            // Sender's fungible item garage does not contain enough items.
            foreach (var (fungibleId, _) in _fungibleIdAndCounts)
            {
                var garageAddr = Addresses.GetGarageAddress(
                    SenderAgentAddr,
                    fungibleId);
                var garageState = _previousStates.GetState(garageAddr);
                var garage = new FungibleItemGarage(garageState);
                garage.Add(-1);
                var previousStatesWithNotEnoughCountOfGarageState =
                    _previousStates.SetState(garageAddr, garage.Serialize());
                if (garage.Count == 0)
                {
                    Assert.Throws<StateNullException>(() => Execute(
                        SenderAgentAddr,
                        0,
                        previousStatesWithNotEnoughCountOfGarageState,
                        new TestRandom(),
                        null,
                        _recipientInventoryAddr,
                        _fungibleIdAndCounts));
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                        SenderAgentAddr,
                        0,
                        previousStatesWithNotEnoughCountOfGarageState,
                        new TestRandom(),
                        null,
                        _recipientInventoryAddr,
                        _fungibleIdAndCounts));
                }
            }

            // Recipient's fungible item garages can be overflowed.
            for (var i = 0; i < _fungibleIdAndCounts.Length; i++)
            {
                var item = _tradableFungibleItems[i];
                var inventory = _previousStates.GetInventory(_recipientInventoryAddr.Value);
                inventory.AddTradableFungibleItem(item, int.MaxValue);
                var previousStatesWithInvalidGarageState =
                    _previousStates.SetState(_recipientInventoryAddr.Value, inventory.Serialize());
                Assert.Throws<ArgumentOutOfRangeException>(() => Execute(
                    SenderAgentAddr,
                    0,
                    previousStatesWithInvalidGarageState,
                    new TestRandom(),
                    null,
                    _recipientInventoryAddr,
                    _fungibleIdAndCounts));
            }
        }

        private static (TransferFromGarages action, IAccountStateDelta nextStates) Execute(
            Address signer,
            long blockIndex,
            IAccountStateDelta previousStates,
            IRandom random,
            IEnumerable<(Address balanceAddr, FungibleAssetValue value)>? fungibleAssetValues,
            Address? recipientInventoryAddr,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts)
        {
            var action = new TransferFromGarages(
                fungibleAssetValues,
                recipientInventoryAddr,
                fungibleIdAndCounts);
            return (
                action,
                action.Execute(new ActionContext
                {
                    Signer = signer,
                    BlockIndex = blockIndex,
                    Rehearsal = false,
                    PreviousStates = previousStates,
                    Random = random,
                }));
        }

        private static (Address balanceAddr, FungibleAssetValue value)[]
            GetFungibleAssetValues(
                Address agentAddr,
                Address avatarAddr)
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav => fav.Sign > 0)
                .Select(fav =>
                {
                    if (Currencies.IsRuneTicker(fav.Currency.Ticker) ||
                        Currencies.IsSoulstoneTicker(fav.Currency.Ticker))
                    {
                        return (avatarAddr, fav);
                    }

                    return (agentAddr, fav);
                })
                .ToArray();
        }

        private (
            (Address balanceAddr, FungibleAssetValue value)[] fungibleAssetValues,
            Address? inventoryAddr,
            (HashDigest<SHA256> fungibleId, int count)[] fungibleIdAndCounts,
            ITradableFungibleItem[] _tradableFungibleItems,
            IAccountStateDelta previousStates)
            GetSuccessfulPreviousStatesWithPlainValue()
        {
            var previousStates = _initialStatesWithAvatarStateV2;
            var senderFavGarageBalanceAddr =
                Addresses.GetGarageBalanceAddress(
                    SenderAgentAddr);
            var fungibleAssetValues = GetFungibleAssetValues(
                RecipientAgentAddr,
                RecipientAvatarAddr);
            foreach (var (_, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    previousStates = previousStates.TransferAsset(
                        Addresses.Admin,
                        senderFavGarageBalanceAddr,
                        value);
                    continue;
                }

                previousStates = previousStates.MintAsset(
                    senderFavGarageBalanceAddr,
                    value);
            }

            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateTradableMaterial)
                .Select((tradableMaterial, index) =>
                {
                    var senderGarageAddr = Addresses.GetGarageAddress(
                        SenderAgentAddr,
                        tradableMaterial.FungibleId);
                    var count = index + 1;
                    var senderGarage = new FungibleItemGarage(tradableMaterial, count);
                    previousStates = previousStates.SetState(
                        senderGarageAddr,
                        senderGarage.Serialize());

                    return (
                        tradableFungibleItem: (ITradableFungibleItem)tradableMaterial,
                        count);
                }).ToArray();
            return (
                fungibleAssetValues,
                inventoryAddr: Addresses.GetInventoryAddress(
                    RecipientAgentAddr,
                    RecipientAvatarIndex),
                fungibleItemAndCounts
                    .Select(tuple => (tuple.tradableFungibleItem.FungibleId, tuple.count))
                    .ToArray(),
                fungibleItemAndCounts.Select(tuple => tuple.tradableFungibleItem).ToArray(),
                previousStates
            );
        }
    }
}
