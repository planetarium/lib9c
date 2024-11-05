namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RetrieveAvatarAssetsTest
    {
        private static readonly Address _minter = new (
            new byte[]
            {
                0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private static readonly Currency _currency = Currency.Legacy("NCG", 2, _minter);
#pragma warning restore CS0618

        private static readonly Dictionary<string, string>
            _csv = TableSheetsImporter.ImportSheets();

        private readonly Address _signer;
        private readonly IWorld _state;

        public RetrieveAvatarAssetsTest()
        {
            var ca = new CreateAvatar
            {
                index = 0,
                hair = 2,
                lens = 3,
                ear = 4,
                tail = 5,
                name = "JohnDoe",
            };
            _signer = new PrivateKey().Address;
            IWorld state = new World(MockWorldState.CreateModern());
            foreach (var (key, value) in _csv)
            {
                state = state.SetLegacyState(Addresses.GetSheetAddress(key), (Text)value);
            }

            state = state
                .SetLegacyState(
                    Addresses.GameConfig,
                    new GameConfigState(_csv[nameof(GameConfigSheet)]).Serialize())
                .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(_currency).Serialize());

            _state = ca.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    BlockIndex = 0,
                    Signer = _signer,
                    RandomSeed = 0,
                });
        }

        [Fact]
        public void PlainValue()
        {
            var avatarAddress = new PrivateKey().Address;
            var action = new RetrieveAvatarAssets(avatarAddress);
            var plainValue = (Dictionary)action.PlainValue;
            var values = (Dictionary)plainValue["values"];
            Assert.Equal((Text)RetrieveAvatarAssets.TypeIdentifier, plainValue["type_id"]);
            Assert.Equal(avatarAddress, values["a"].ToAddress());
        }

        [Fact]
        public void LoadPlainValue()
        {
            var avatarAddress = new PrivateKey().Address;
            var expectedValue = Dictionary.Empty
                .Add("type_id", RetrieveAvatarAssets.TypeIdentifier)
                .Add("values", Dictionary.Empty.Add("a", avatarAddress.Serialize()));

            // Let's assume that serializedAction is the serialized representation of the action, which might be obtained through some other part of your tests
            var action = new RetrieveAvatarAssets();
            action.LoadPlainValue(expectedValue);

            var plainValue = (Dictionary)action.PlainValue;
            var values = (Dictionary)plainValue["values"];

            Assert.Equal((Text)RetrieveAvatarAssets.TypeIdentifier, plainValue["type_id"]);
            Assert.Equal(avatarAddress, values["a"].ToAddress());
        }

        [Fact]
        public void Execute()
        {
            var agentState = _state.GetAgentState(_signer);
            Assert.NotNull(agentState);
            var avatarAddress = agentState.avatarAddresses[0];
            var prevState = _state.MintAsset(
                new ActionContext
                {
                    Signer = _minter,
                },
                avatarAddress,
                1 * _currency
            );
            Assert.Equal(1 * _currency, prevState.GetBalance(avatarAddress, _currency));

            var action = new RetrieveAvatarAssets(avatarAddress);
            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = prevState,
                    BlockIndex = 1L,
                    Signer = _signer,
                    RandomSeed = 0,
                });
            Assert.Equal(0 * _currency, nextState.GetBalance(avatarAddress, _currency));
            Assert.Equal(1 * _currency, nextState.GetBalance(_signer, _currency));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var avatarAddress = new PrivateKey().Address;
            var action = new RetrieveAvatarAssets(avatarAddress);

            var context = new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = new World(MockWorldState.CreateModern()),
                RandomSeed = 0,
                Signer = avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(context));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var agentState = _state.GetAgentState(_signer);
            Assert.NotNull(agentState);
            var avatarAddress = agentState.avatarAddresses[0];
            Assert.Equal(0 * _currency, _state.GetBalance(avatarAddress, _currency));

            var action = new RetrieveAvatarAssets(avatarAddress);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _state,
                        BlockIndex = 1L,
                        Signer = _signer,
                        RandomSeed = 0,
                    }));
        }
    }
}
