namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class TransferAsset3Test
    {
        private static readonly Address _sender = new Address(
            new byte[]
            {
                 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

        private static readonly Address _recipient = new Address(new byte[]
            {
                 0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private static readonly Currency _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

        [Fact]
        public void Constructor_ThrowsMemoLengthOverflowException()
        {
            Assert.Throws<MemoLengthOverflowException>(() =>
                new TransferAsset3(_sender, _recipient, _currency * 100, new string(' ', 100)));
        }

        [Theory]
        // activation by derive address.
        [InlineData(true, false, false)]
        // activation by ActivatedAccountsState.
        [InlineData(false, true, false)]
        // state exist.
        [InlineData(false, false, true)]
        public void Execute(bool activate, bool legacyActivate, bool stateExist)
        {
            var mockState = MockState.Empty
                .SetBalance(_sender, _currency * 1000)
                .SetBalance(_recipient, _currency * 10);

            if (activate)
            {
                mockState = mockState.SetState(_recipient.Derive(ActivationKey.DeriveKey), true.Serialize());
            }

            if (legacyActivate)
            {
                var activatedAccountState = new ActivatedAccountsState();
                activatedAccountState = activatedAccountState.AddAccount(_recipient);
                mockState = mockState.SetState(activatedAccountState.address, activatedAccountState.Serialize());
            }

            if (stateExist)
            {
                mockState = mockState.SetState(_recipient, new AgentState(_recipient).Serialize());
            }

            var prevState = new Account(mockState);
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );
            IAccount nextState = action.Execute(new ActionContext()
            {
                PreviousState = prevState,
                Signer = _sender,
                BlockIndex = 1,
            });

            Assert.Equal(_currency * 900, nextState.GetBalance(_sender, _currency));
            Assert.Equal(_currency * 110, nextState.GetBalance(_recipient, _currency));
        }

        [Fact]
        public void ExecuteWithInvalidSigner()
        {
            var prevState = new Account(
                MockState.Empty
                    .SetBalance(_sender, _currency * 1000)
                    .SetBalance(_recipient, _currency * 10));
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );

            var exc = Assert.Throws<InvalidTransferSignerException>(() =>
            {
                _ = action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    // 송금자가 직접 사인하지 않으면 실패해야 합니다.
                    Signer = _recipient,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(exc.Sender, _sender);
            Assert.Equal(exc.Recipient, _recipient);
            Assert.Equal(exc.TxSigner, _recipient);
        }

        [Fact]
        public void ExecuteWithInvalidRecipient()
        {
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, _currency), _currency * 1000);
            var prevState = new Account(
                MockState.Empty
                    .SetBalance(_sender, _currency * 1000));

            // Should not allow TransferAsset with same sender and recipient.
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _sender,
                amount: _currency * 100
            );

            var exc = Assert.Throws<InvalidTransferRecipientException>(() =>
            {
                _ = action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(exc.Sender, _sender);
            Assert.Equal(exc.Recipient, _sender);
        }

        [Fact]
        public void ExecuteWithInsufficientBalance()
        {
            var prevState = new Account(
                MockState.Empty
                    .SetBalance(_sender, _currency * 1000)
                    .SetBalance(_recipient, _currency * 10))
                    .SetState(_recipient, new AgentState(_recipient).Serialize());
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100000
            );

            Assert.Throws<InsufficientBalanceException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });
            });
        }

        [Fact]
        public void ExecuteWithMinterAsSender()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currencyBySender = Currency.Legacy("NCG", 2, _sender);
#pragma warning restore CS0618
            var prevState = new Account(
                MockState.Empty
                    .SetState(_recipient, new AgentState(_recipient).Serialize())
                    .SetBalance(_sender, currencyBySender * 1000)
                    .SetBalance(_recipient, currencyBySender * 10));
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: currencyBySender * 100
            );
            var ex = Assert.Throws<InvalidTransferMinterException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(new[] { _sender }, ex.Minters);
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Fact]
        public void ExecuteWithMinterAsRecipient()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currencyByRecipient = Currency.Legacy("NCG", 2, _sender);
#pragma warning restore CS0618
            var prevState = new Account(
                MockState.Empty
                    .SetBalance(_sender, currencyByRecipient * 1000)
                    .SetBalance(_recipient, currencyByRecipient * 10)
                    .SetState(_recipient, new AgentState(_recipient).Serialize()));
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: currencyByRecipient * 100
            );
            var ex = Assert.Throws<InvalidTransferMinterException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(new[] { _sender }, ex.Minters);
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Fact]
        public void ExecuteWithUnactivatedRecipient()
        {
            var activatedAddress = new ActivatedAccountsState().AddAccount(new PrivateKey().Address);
            var prevState = new Account(
                MockState.Empty
                    .SetState(_sender.Derive(ActivationKey.DeriveKey), true.Serialize())
                    .SetState(Addresses.ActivatedAccount, activatedAddress.Serialize())
                    .SetBalance(_sender, _currency * 1000)
                    .SetBalance(_recipient, _currency * 10));
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );
            var ex = Assert.Throws<InvalidTransferUnactivatedRecipientException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });
            });
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void PlainValue(string memo)
        {
            var action = new TransferAsset3(_sender, _recipient, _currency * 100, memo);
            Dictionary plainValue = (Dictionary)action.PlainValue;
            Dictionary values = (Dictionary)plainValue["values"];

            Assert.Equal("transfer_asset3", (Text)plainValue["type_id"]);
            Assert.Equal(_sender, values["sender"].ToAddress());
            Assert.Equal(_recipient, values["recipient"].ToAddress());
            Assert.Equal(_currency * 100, values["amount"].ToFungibleAssetValue());
            if (!(memo is null))
            {
                Assert.Equal(memo, values["memo"].ToDotnetString());
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void LoadPlainValue(string memo)
        {
            IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"recipient", _recipient.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"amount", (_currency * 100).Serialize()),
            };
            if (!(memo is null))
            {
                pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text)"memo", memo.Serialize()));
            }

            var plainValue = Dictionary.Empty
                .Add("type_id", "transfer_asset3")
                .Add("values", new Dictionary(pairs));
            var action = new TransferAsset3();
            action.LoadPlainValue(plainValue);

            Assert.Equal(_sender, action.Sender);
            Assert.Equal(_recipient, action.Recipient);
            Assert.Equal(_currency * 100, action.Amount);
            Assert.Equal(memo, action.Memo);
        }

        [Fact]
        public void Execute_Throw_InvalidTransferCurrencyException()
        {
            var crystal = CrystalCalculator.CRYSTAL;
            var prevState = new Account(
                MockState.Empty
                    .SetState(_recipient.Derive(ActivationKey.DeriveKey), true.Serialize())
                    .SetBalance(_sender, crystal * 1000));
            var action = new TransferAsset3(
                sender: _sender,
                recipient: _recipient,
                amount: 1000 * crystal
            );
            Assert.Throws<InvalidTransferCurrencyException>(() => action.Execute(new ActionContext()
            {
                PreviousState = prevState,
                Signer = _sender,
                BlockIndex = TransferAsset3.CrystalTransferringRestrictionStartIndex,
            }));
        }

        [Fact]
        public void LoadPlainValue_ThrowsMemoLengthOverflowException()
        {
            var action = new TransferAsset3();
            var values = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"recipient", _recipient.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"amount", (_currency * 100).Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"memo", new string(' ', 81).Serialize()),
            });
            var plainValue = Dictionary.Empty
                .Add("type_id", "transfer_asset3")
                .Add("values", values);

            Assert.Throws<MemoLengthOverflowException>(() => action.LoadPlainValue(plainValue));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void SerializeWithDotnetAPI(string memo)
        {
            var formatter = new BinaryFormatter();
            var action = new TransferAsset3(_sender, _recipient, _currency * 100, memo);

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (TransferAsset3)formatter.Deserialize(ms);

            Assert.Equal(_sender, deserialized.Sender);
            Assert.Equal(_recipient, deserialized.Recipient);
            Assert.Equal(_currency * 100, deserialized.Amount);
            Assert.Equal(memo, deserialized.Memo);
        }
    }
}
