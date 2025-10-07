namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class SetAddressStateTest
    {
        private readonly Address _accountAddress;
        private readonly Address _targetAddress;
        private readonly Address _targetAddress2;
        private readonly Address _adminAddress;
        private readonly IValue _state;
        private readonly IValue _state2;
        private readonly IWorld _initialState;

        public SetAddressStateTest()
        {
            _accountAddress = ReservedAddresses.LegacyAccount;
            _targetAddress = new PrivateKey().Address;
            _targetAddress2 = new PrivateKey().Address;
            _adminAddress = new PrivateKey().Address;
            _state = (Text)"test_state";
            _state2 = (Text)"test_state2";
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.Admin, new AdminState(_adminAddress, 100).Serialize());
        }

        [Fact]
        public void Execute()
        {
            Assert.Null(_initialState.GetLegacyState(_targetAddress));
            Assert.Null(_initialState.GetLegacyState(_targetAddress2));

            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, _state),
                (_accountAddress, _targetAddress2, _state2),
            });
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(_state, nextState.GetLegacyState(_targetAddress));
            Assert.Equal(_state2, nextState.GetLegacyState(_targetAddress2));
        }

        [Fact]
        public void Execute_Throws_When_State_Already_Exists()
        {
            var initialState = _initialState.SetLegacyState(_targetAddress, _state);
            Assert.NotNull(initialState.GetLegacyState(_targetAddress));

            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, _state2),
            });

            Assert.Throws<InvalidOperationException>(() =>
                action.Execute(
                    new ActionContext()
                    {
                        PreviousState = initialState,
                        Signer = _adminAddress,
                        BlockIndex = 1,
                    }
                )
            );
        }

        [Fact]
        public void Constructor_Throws_When_States_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SetAddressState(null));
        }

        [Fact]
        public void Constructor_Throws_When_State_Value_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, null),
            }));
        }

        [Fact]
        public void Deserialize_Throws_When_State_Value_Is_Null()
        {
            var action = new SetAddressState();
            var dictionary = Dictionary.Empty
                .Add((Text)"type_id", (Text)"set_address_state")
                .Add((Text)"values", Dictionary.Empty
                    .Add("s", List.Empty
                        .Add(List.Empty
                            .Add(_accountAddress.Serialize())
                            .Add(_targetAddress.Serialize())
                            .Add(Null.Value))));

            Assert.Throws<ArgumentNullException>(() => action.LoadPlainValue(dictionary));
        }

        [Fact]
        public void Execute_With_Empty_States()
        {
            var action = new SetAddressState(new List<(Address, Address, IValue)>());
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(_initialState, nextState);
        }

        [Fact]
        public void Execute_With_Complex_State()
        {
            var complexState = Dictionary.Empty
                .Add("key1", (Text)"value1")
                .Add("key2", new List(new IValue[] { (Integer)1, (Integer)2, (Integer)3 }))
                .Add("key3", Dictionary.Empty.Add("nested", (Text)"value"));

            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, complexState),
            });
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = _adminAddress,
                    BlockIndex = 1,
                }
            );

            var retrievedState = (Dictionary)nextState.GetLegacyState(_targetAddress);
            Assert.Equal(complexState, retrievedState);
        }

        [Fact]
        public void Serialize()
        {
            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, _state),
                (_accountAddress, _targetAddress2, _state2),
            });
            var serialized = (Dictionary)action.PlainValue;
            Assert.Equal((Text)"set_address_state", serialized["type_id"]);

            var values = (Dictionary)serialized["values"];
            var states = (List)values["s"];
            var state1 = (List)states[0];
            var state2 = (List)states[1];
            Assert.Equal(_accountAddress, state1[0].ToAddress());
            Assert.Equal(_targetAddress, state1[1].ToAddress());
            Assert.Equal(_state, state1[2]);
            Assert.Equal(_accountAddress, state2[0].ToAddress());
            Assert.Equal(_targetAddress2, state2[1].ToAddress());
            Assert.Equal(_state2, state2[2]);
        }

        [Fact]
        public void Deserialize()
        {
            var action = new SetAddressState();
            var dictionary = Dictionary.Empty
                .Add((Text)"type_id", (Text)"set_address_state")
                .Add((Text)"values", Dictionary.Empty
                    .Add("s", List.Empty
                        .Add(List.Empty
                            .Add(_accountAddress.Serialize())
                            .Add(_targetAddress.Serialize())
                            .Add(_state))
                        .Add(List.Empty
                            .Add(_accountAddress.Serialize())
                            .Add(_targetAddress2.Serialize())
                            .Add(_state2))));

            action.LoadPlainValue(dictionary);

            Assert.Equal(_accountAddress, action.States[0].accountAddress);
            Assert.Equal(_targetAddress, action.States[0].targetAddress);
            Assert.Equal(_state, action.States[0].state);
            Assert.Equal(_accountAddress, action.States[1].accountAddress);
            Assert.Equal(_targetAddress2, action.States[1].targetAddress);
            Assert.Equal(_state2, action.States[1].state);
        }

        [Fact]
        public void Deserialize_With_Invalid_Format()
        {
            var action = new SetAddressState();
            var dictionary = Dictionary.Empty
                .Add((Text)"type_id", (Text)"set_address_state")
                .Add((Text)"values", Dictionary.Empty
                    .Add("s", List.Empty
                        .Add(List.Empty
                            .Add(_accountAddress.Serialize())
                            .Add(_targetAddress.Serialize()))));

            Assert.Throws<IndexOutOfRangeException>(() => action.LoadPlainValue(dictionary));
        }

        [Fact]
        public void Execute_ThrowsPermissionDeniedException()
        {
            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, _state),
                (_accountAddress, _targetAddress2, _state2),
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

        [Fact]
        public void Execute_WithOperator()
        {
            Assert.Null(_initialState.GetLegacyState(_targetAddress));
            Assert.Null(_initialState.GetLegacyState(_targetAddress2));

            var action = new SetAddressState(new List<(Address, Address, IValue)>
            {
                (_accountAddress, _targetAddress, _state),
                (_accountAddress, _targetAddress2, _state2),
            });

            // Can execute even after policy expiration when signed by Operator
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _initialState,
                    Signer = PatchTableSheet.Operator,
                    BlockIndex = 101,
                }
            );

            Assert.Equal(_state, nextState.GetLegacyState(_targetAddress));
            Assert.Equal(_state2, nextState.GetLegacyState(_targetAddress2));
        }
    }
}
