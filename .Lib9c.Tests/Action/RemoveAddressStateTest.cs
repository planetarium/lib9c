namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RemoveAddressStateTest
    {
        private readonly Address _accountAddress;
        private readonly Address _targetAddress;
        private readonly Address _adminAddress;
        private readonly IValue _state;
        private readonly IWorld _initialState;

        public RemoveAddressStateTest()
        {
            _accountAddress = ReservedAddresses.LegacyAccount;
            _targetAddress = new PrivateKey().Address;
            _adminAddress = new PrivateKey().Address;
            _state = (Text)"test_state";
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.Admin, new AdminState(_adminAddress, 100).Serialize())
                .SetLegacyState(_targetAddress, _state);
        }

        [Fact]
        public void Execute()
        {
            Assert.NotNull(_initialState.GetLegacyState(_targetAddress));

            var action = new RemoveAddressState(_accountAddress, _targetAddress);
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            Assert.Null(nextState.GetLegacyState(_targetAddress));
        }

        [Fact]
        public void Serialize()
        {
            var action = new RemoveAddressState(_accountAddress, _targetAddress);
            var serialized = (Dictionary)action.PlainValue;
            Assert.Equal((Text)"remove_address_state", serialized["type_id"]);

            var values = (Dictionary)serialized["values"];
            Assert.Equal(_accountAddress, values["a"].ToAddress());
            Assert.Equal(_targetAddress, values["t"].ToAddress());
        }

        [Fact]
        public void Deserialize()
        {
            var action = new RemoveAddressState();
            var dictionary = Dictionary.Empty
                .Add((Text)"type_id", (Text)"remove_address_state")
                .Add((Text)"values", Dictionary.Empty
                    .Add("a", _accountAddress.Serialize())
                    .Add("t", _targetAddress.Serialize()));

            action.LoadPlainValue(dictionary);

            Assert.Equal(_accountAddress, action.AccountAddress);
            Assert.Equal(_targetAddress, action.TargetAddress);
        }

        [Fact]
        public void Execute_ThrowsPermissionDeniedException()
        {
            var action = new RemoveAddressState(_accountAddress, _targetAddress);
            var invalidSigner = new Address("5555555555555555555555555555555555555555");

            Assert.Throws<PermissionDeniedException>(() =>
                action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _initialState,
                        Signer = invalidSigner,
                        BlockIndex = 1,
                    }
                )
            );
        }
    }
}
