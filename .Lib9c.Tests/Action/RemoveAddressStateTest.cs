namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
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
        private readonly Address _targetAddress2;
        private readonly Address _adminAddress;
        private readonly IValue _state;
        private readonly IValue _state2;
        private readonly IWorld _initialState;

        public RemoveAddressStateTest()
        {
            _accountAddress = ReservedAddresses.LegacyAccount;
            _targetAddress = new PrivateKey().Address;
            _targetAddress2 = new PrivateKey().Address;
            _adminAddress = new PrivateKey().Address;
            _state = (Text)"test_state";
            _state2 = (Text)"test_state2";
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.Admin, new AdminState(_adminAddress, 100).Serialize())
                .SetLegacyState(_targetAddress, _state)
                .SetLegacyState(_targetAddress2, _state2);
        }

        [Fact]
        public void Execute()
        {
            Assert.NotNull(_initialState.GetLegacyState(_targetAddress));
            Assert.NotNull(_initialState.GetLegacyState(_targetAddress2));

            var action = new RemoveAddressState(new List<(Address, Address)>
            {
                (_accountAddress, _targetAddress),
                (_accountAddress, _targetAddress2),
            });
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            Assert.Null(nextState.GetLegacyState(_targetAddress));
            Assert.Null(nextState.GetLegacyState(_targetAddress2));
        }

        [Fact]
        public void Serialize()
        {
            var action = new RemoveAddressState(new List<(Address, Address)>
            {
                (_accountAddress, _targetAddress),
                (_accountAddress, _targetAddress2),
            });
            var serialized = (Dictionary)action.PlainValue;
            Assert.Equal((Text)"remove_address_state", serialized["type_id"]);

            var values = (Dictionary)serialized["values"];
            var pairs = (List)values["r"];
            var pair1 = (List)pairs[0];
            var pair2 = (List)pairs[1];
            Assert.Equal(_accountAddress, pair1[0].ToAddress());
            Assert.Equal(_targetAddress, pair1[1].ToAddress());
            Assert.Equal(_accountAddress, pair2[0].ToAddress());
            Assert.Equal(_targetAddress2, pair2[1].ToAddress());
        }

        [Fact]
        public void Deserialize()
        {
            var action = new RemoveAddressState();
            var dictionary = Dictionary.Empty
                .Add((Text)"type_id", (Text)"remove_address_state")
                .Add((Text)"values", Dictionary.Empty
                    .Add("r", List.Empty
                        .Add(List.Empty.Add(_accountAddress.Serialize()).Add(_targetAddress.Serialize()))
                        .Add(List.Empty.Add(_accountAddress.Serialize()).Add(_targetAddress2.Serialize()))));

            action.LoadPlainValue(dictionary);

            Assert.Equal(_accountAddress, action.Removals[0].accountAddress);
            Assert.Equal(_targetAddress, action.Removals[0].targetAddress);
            Assert.Equal(_accountAddress, action.Removals[1].accountAddress);
            Assert.Equal(_targetAddress2, action.Removals[1].targetAddress);
        }

        [Fact]
        public void Execute_ThrowsPermissionDeniedException()
        {
            var action = new RemoveAddressState(new List<(Address, Address)>
            {
                (_accountAddress, _targetAddress),
                (_accountAddress, _targetAddress2),
            });
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
