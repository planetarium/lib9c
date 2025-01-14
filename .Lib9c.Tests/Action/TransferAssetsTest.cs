namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class TransferAssetsTest
    {
        private static readonly Address _sender = new (
            new byte[]
            {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

        private static readonly Address _recipient = new (
            new byte[]
            {
                0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

        private static readonly Address _recipient2 = new (
            new byte[]
            {
                0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
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
            Assert.Throws<MemoLengthOverflowException>(
                () =>
                    new TransferAssets(
                        _sender,
                        new List<(Address, FungibleAssetValue)>()
                        {
                            (_recipient, _currency * 100),
                        },
                        new string(' ', 100)
                    )
            );
        }

        [Fact]
        public void Execute()
        {
            var contractAddress = _sender.Derive(nameof(RequestPledge));
            var patronAddress = new PrivateKey().Address;
            var prevState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_sender, _currency * 1000)
                    .SetBalance(_recipient, _currency * 10));
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, _currency * 100),
                    (_recipient2, _currency * 100),
                }
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _sender,
                    BlockIndex = 1,
                });

            Assert.Equal(_currency * 800, nextState.GetBalance(_sender, _currency));
            Assert.Equal(_currency * 110, nextState.GetBalance(_recipient, _currency));
            Assert.Equal(_currency * 100, nextState.GetBalance(_recipient2, _currency));
            Assert.Equal(Currencies.Mead * 0, nextState.GetBalance(_sender, Currencies.Mead));
            Assert.Equal(Currencies.Mead * 0, nextState.GetBalance(patronAddress, Currencies.Mead));
        }

        [Fact]
        public void Execute_Throw_InvalidTransferSignerException()
        {
            var prevState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_sender, _currency * 1000)
                    .SetBalance(_recipient, _currency * 10));
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, _currency * 100),
                }
            );

            var exc = Assert.Throws<InvalidTransferSignerException>(
                () =>
                {
                    _ = action.Execute(
                        new ActionContext()
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
        public void Execute_Throw_InvalidTransferRecipientException()
        {
            var prevState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_sender, _currency * 1000));
            // Should not allow TransferAsset with same sender and recipient.
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_sender, _currency * 100),
                }
            );

            var exc = Assert.Throws<InvalidTransferRecipientException>(
                () =>
                {
                    _ = action.Execute(
                        new ActionContext()
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
        public void Execute_Throw_InsufficientBalanceException()
        {
            var prevState = new World(
                    MockWorldState.CreateModern()
                        .SetBalance(_sender, _currency * 1000)
                        .SetBalance(_recipient, _currency * 10))
                .SetAgentState(_recipient, new AgentState(_recipient));
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, _currency * 100000),
                }
            );

            var exc = Assert.Throws<InsufficientBalanceException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            PreviousState = prevState,
                            Signer = _sender,
                            BlockIndex = 1,
                        });
                });

            Assert.Equal(_sender, exc.Address);
            Assert.Equal(_currency, exc.Balance.Currency);
        }

        [Fact]
        public void Execute_Throw_InvalidTransferMinterException()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currencyBySender = Currency.Legacy("NCG", 2, _sender);
#pragma warning restore CS0618
            var prevState = new World(
                    MockWorldState.CreateModern()
                        .SetBalance(_sender, currencyBySender * 1000)
                        .SetBalance(_recipient, currencyBySender * 10))
                .SetAgentState(_recipient, new AgentState(_recipient));
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, currencyBySender * 100),
                }
            );
            var ex = Assert.Throws<InvalidTransferMinterException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            PreviousState = prevState,
                            Signer = _sender,
                            BlockIndex = 1,
                        });
                });

            Assert.Equal(new[] { _sender, }, ex.Minters);
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void PlainValue(string memo)
        {
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, _currency * 100),
                },
                memo
            );

            var plainValue = (Dictionary)action.PlainValue;
            var values = (Dictionary)plainValue["values"];

            var recipients = (List)values["recipients"];
            var info = (List)recipients[0];
            Assert.Equal((Text)"transfer_assets3", plainValue["type_id"]);
            Assert.Equal(_sender, values["sender"].ToAddress());
            Assert.Equal(_recipient, info[0].ToAddress());
            Assert.Equal(_currency * 100, info[1].ToFungibleAssetValue());
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
                new KeyValuePair<IKey, IValue>((Text)"recipients", List.Empty.Add(List.Empty.Add(_recipient.Serialize()).Add((_currency * 100).Serialize()))),
            };
            if (!(memo is null))
            {
                pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text)"memo", memo.Serialize()));
            }

            var plainValue = Dictionary.Empty
                .Add("type_id", "transfer_assets")
                .Add("values", new Dictionary(pairs));
            var action = new TransferAssets();
            action.LoadPlainValue(plainValue);

            Assert.Equal(_sender, action.Sender);
            Assert.Equal(_recipient, action.Recipients.Single().recipient);
            Assert.Equal(_currency * 100, action.Recipients.Single().amount);
            Assert.Equal(memo, action.Memo);
        }

        [Fact]
        public void LoadPlainValue_ThrowsMemoLengthOverflowException()
        {
            var action = new TransferAssets();
            var plainValue = Dictionary.Empty
                .Add("type_id", "transfer_assets")
                .Add(
                    "values",
                    new Dictionary(
                        new[]
                        {
                            new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                            new KeyValuePair<IKey, IValue>((Text)"recipients", List.Empty.Add(List.Empty.Add(_recipient.Serialize()).Add((_currency * 100).Serialize()))),
                            new KeyValuePair<IKey, IValue>((Text)"memo", new string(' ', 81).Serialize()),
                        }));

            Assert.Throws<MemoLengthOverflowException>(() => action.LoadPlainValue(plainValue));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void SerializeWithDotnetAPI(string memo)
        {
            var formatter = new BinaryFormatter();
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, _currency * 100),
                },
                memo
            );

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (TransferAssets)formatter.Deserialize(ms);

            Assert.Equal(_sender, deserialized.Sender);
            Assert.Equal(_recipient, deserialized.Recipients.Single().recipient);
            Assert.Equal(_currency * 100, deserialized.Recipients.Single().amount);
            Assert.Equal(memo, deserialized.Memo);
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var recipients = new List<(Address, FungibleAssetValue)>();

            for (var i = 0; i < TransferAssets.RecipientsCapacity + 1; i++)
            {
                recipients.Add((_recipient, _currency * 100));
            }

            var action = new TransferAssets(_sender, recipients);
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            PreviousState = new World(MockUtil.MockModernWorldState),
                            Signer = _sender,
                            BlockIndex = 1,
                        });
                });
        }

        [Fact]
        public void Execute_Throw_InvalidTransferCurrencyException()
        {
            var crystal = CrystalCalculator.CRYSTAL;
            var prevState = new World(
                    MockWorldState.CreateModern()
                        .SetBalance(_sender, crystal * 1000))
                .SetLegacyState(_recipient.Derive(ActivationKey.DeriveKey), true.Serialize());
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (_recipient, 1000 * crystal),
                    (_recipient, 100 * _currency),
                }
            );
            Assert.Throws<InvalidTransferCurrencyException>(
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = prevState,
                        Signer = _sender,
                        BlockIndex = TransferAsset3.CrystalTransferringRestrictionStartIndex,
                    }));
        }

        [Fact]
        public void Execute_Throw_ArgumentException()
        {
            var baseState = new World(MockWorldState.CreateModern().SetBalance(_sender, _currency * 1000));
            var action = new TransferAssets(
                _sender,
                new List<(Address, FungibleAssetValue)>
                {
                    (LegacyStakeState.DeriveAddress(_recipient), _currency * 100),
                    (_recipient2, _currency * 100),
                }
            );
            // 스테이킹 주소에 송금하려고 하면 실패합니다.
            Assert.Throws<ArgumentException>("recipient", () => action.Execute(new ActionContext()
            {
                PreviousState = baseState
                    .SetLegacyState(
                        LegacyStakeState.DeriveAddress(_recipient),
                        new LegacyStakeState(LegacyStakeState.DeriveAddress(_recipient), 0).SerializeV2()),
                Signer = _sender,
                BlockIndex = 1,
            }));
            Assert.Throws<ArgumentException>("recipient", () => action.Execute(new ActionContext()
            {
                PreviousState = baseState
                    .SetLegacyState(
                        LegacyStakeState.DeriveAddress(_recipient),
                        new StakeState(
                            new Contract(
                                "StakeRegularFixedRewardSheet_V1",
                                "StakeRegularRewardSheet_V1",
                                50400,
                                201600),
                            0).Serialize()),
                Signer = _sender,
                BlockIndex = 1,
            }));
            Assert.Throws<ArgumentException>("recipient", () => action.Execute(new ActionContext()
            {
                PreviousState = baseState
                    .SetLegacyState(
                        LegacyStakeState.DeriveAddress(_recipient),
                        new MonsterCollectionState(
                                MonsterCollectionState.DeriveAddress(_sender, 0),
                                1,
                                0)
                            .Serialize()),
                Signer = _sender,
                BlockIndex = 1,
            }));
            var monsterCollectionRewardSheet = new MonsterCollectionRewardSheet();
            monsterCollectionRewardSheet.Set(
                "level,required_gold,reward_id\n1,500,1\n2,1800,2\n3,7200,3\n4,54000,4\n5,270000,5\n6,480000,6\n7,1500000,7\n");
            Assert.Throws<ArgumentException>("recipient", () => action.Execute(new ActionContext()
            {
                PreviousState = baseState
                    .SetLegacyState(
                        LegacyStakeState.DeriveAddress(_recipient),
                        new MonsterCollectionState0(
                                MonsterCollectionState.DeriveAddress(_sender, 0),
                                1,
                                0,
                                monsterCollectionRewardSheet)
                            .Serialize()),
                Signer = _sender,
                BlockIndex = 1,
            }));
        }
    }
}
