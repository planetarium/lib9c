namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Exceptions;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class BurnAssetTest
    {
        private readonly Address _signer;

        private readonly IWorld _prevState;

        public BurnAssetTest()
        {
            _signer = new PrivateKey().Address;
            _prevState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_signer, Currencies.Crystal * 100)
                    .SetBalance(
                        _signer.Derive(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CreateAvatar.DeriveFormat,
                                1)),
                        Currencies.DailyRewardRune * 20));
        }

        [Fact]
        public void Constructor_Throws_MemoLengthOverflowException()
        {
            Assert.Throws<MemoLengthOverflowException>(
                () => new BurnAsset(
                    _signer,
                    Currencies.Crystal * 42,
                    "very long memo".PadRight(100, ' ')
                )
            );
        }

        [Fact]
        public void PlainValue()
        {
            var action = new BurnAsset(
                _signer,
                Currencies.Crystal * 100,
                "memo"
            );
            var expected = new Dictionary(
                new KeyValuePair<IKey, IValue>[]
                {
                    new (
                        (Text)"type_id",
                        (Text)"burn_asset"
                    ),
                    new (
                        (Text)"values",
                        new List(
                            _signer.Bencoded,
                            (Currencies.Crystal * 100).Serialize(),
                            (Text)"memo"
                        )
                    ),
                }
            );

            Assert.Equal(expected, action.PlainValue);
        }

        [Fact]
        public void LoadPlainValue()
        {
            var bencoded = new Dictionary(
                new KeyValuePair<IKey, IValue>[]
                {
                    new (
                        (Text)"type_id",
                        (Text)"burn_asset"
                    ),
                    new (
                        (Text)"values",
                        new List(
                            _signer.Bencoded,
                            (Currencies.Crystal * 100).Serialize(),
                            (Text)"memo"
                        )
                    ),
                }
            );
            var action = new BurnAsset();
            action.LoadPlainValue(bencoded);

            Assert.Equal(Currencies.Crystal * 100, action.Amount);
            Assert.Equal("memo", action.Memo);
        }

        [Fact]
        public void LoadPlainValue_Throws_MemoLengthOverflowException()
        {
            var bencoded = new Dictionary(
                new KeyValuePair<IKey, IValue>[]
                {
                    new (
                        (Text)"type_id",
                        (Text)"burn_asset"
                    ),
                    new (
                        (Text)"values",
                        new List(
                            _signer.Bencoded,
                            (Currencies.Crystal * 100).Serialize(),
                            (Text)"very long memo".PadRight(100, ' ')
                        )
                    ),
                }
            );
            var action = new BurnAsset();
            Assert.Throws<MemoLengthOverflowException>(
                () => action.LoadPlainValue(bencoded)
            );
        }

        [Fact]
        public void Execute()
        {
            var prevState = _prevState;

            var action = new BurnAsset(
                _signer,
                Currencies.Crystal * 42,
                "42"
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(
                Currencies.Crystal * (100 - 42),
                nextState.GetBalance(_signer, Currencies.Crystal)
            );
        }

        [Fact]
        public void Execute_With_AvatarAddress()
        {
            var prevState = _prevState;
            var avatarAddress = _signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    1
                )
            );

            var action = new BurnAsset(
                avatarAddress,
                Currencies.DailyRewardRune * 10,
                "10"
            );
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _signer,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(
                Currencies.DailyRewardRune * (20 - 10),
                nextState.GetBalance(avatarAddress, Currencies.DailyRewardRune)
            );
        }

        [Fact]
        public void Execute_Throws_InsufficientBalanceException()
        {
            var prevState = _prevState;

            var action = new BurnAsset(
                _signer,
                Currencies.Crystal * 1000,
                "1000"
            );
            Assert.Throws<InsufficientBalanceException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            PreviousState = prevState,
                            Signer = _signer,
                            BlockIndex = 1,
                        }
                    );
                });
        }

        [Fact]
        public void Execute_Throws_InvalidActionFieldException()
        {
            var prevState = _prevState;

            var action = new BurnAsset(
                default, // Wrong address
                Currencies.Crystal * 1000,
                "42"
            );
            Assert.Throws<InvalidActionFieldException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            PreviousState = prevState,
                            Signer = _signer,
                            BlockIndex = 1,
                        }
                    );
                });
        }
    }
}
