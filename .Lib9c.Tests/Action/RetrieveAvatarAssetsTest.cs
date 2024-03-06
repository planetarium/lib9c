namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RetrieveAvatarAssetsTest
    {
        private static readonly Address _minter = new Address(
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
            var ca = new CreateAvatar
            {
                index = 0,
                hair = 2,
                lens = 3,
                ear = 4,
                tail = 5,
                name = "JohnDoe",
            };
            var signer = new PrivateKey().Address;
            IWorld state = new World(new MockWorldState());
            foreach (var (key, value) in _csv)
            {
                state = state.SetLegacyState(Addresses.GetSheetAddress(key), (Text)value);
            }

            state = state
                .SetLegacyState(
                Addresses.GameConfig,
                new GameConfigState(_csv[nameof(GameConfigSheet)]).Serialize())
                .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(_currency).Serialize());

            var prevState = ca.Execute(new ActionContext
            {
                PreviousState = state,
                BlockIndex = 0,
                Signer = signer,
                RandomSeed = 0,
            });
            var agentState = prevState.GetAgentState(signer);
            Assert.NotNull(agentState);
            var avatarAddress = agentState.avatarAddresses[0];
            prevState = prevState.MintAsset(
                new ActionContext
            {
                Signer = _minter,
            },
                avatarAddress,
                1 * _currency
            );
            Assert.Equal(1 * _currency, prevState.GetBalance(avatarAddress, _currency));

            var action = new RetrieveAvatarAssets(avatarAddress);
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = prevState,
                BlockIndex = 1L,
                Signer = signer,
                RandomSeed = 0,
            });
            Assert.Equal(0 * _currency, nextState.GetBalance(avatarAddress, _currency));
            Assert.Equal(1 * _currency, nextState.GetBalance(signer, _currency));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var avatarAddress = new PrivateKey().Address;
            var action = new RetrieveAvatarAssets(avatarAddress);

            var context = new ActionContext()
            {
                BlockIndex = 0,
                PreviousState = new World(new MockWorldState()),
                RandomSeed = 0,
                Signer = avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(context));
        }
    }
}
