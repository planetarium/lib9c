namespace Lib9c.Tests.Action
{
    using System;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Consensus;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ValidatorSetOperateTest
    {
        private readonly IWorld _initialState;
        private readonly Validator _validator;

        public ValidatorSetOperateTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new World(MockUtil.MockModernWorldState);
            _validator = new Validator(new PrivateKey().PublicKey, BigInteger.One);

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize())
                    .SetValidatorSet(_initialState.GetValidatorSet().Update(_validator));
            }
        }

        [Fact]
        public void CheckPermission()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, adminState.Serialize());
            var action = ValidatorSetOperate.Append(_validator);
            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = state,
                    Signer = adminAddress,
                });
            Assert.Single(nextState.GetValidatorSet().Validators);
            Assert.Equal(
                _validator,
                nextState.GetValidatorSet().GetValidator(_validator.PublicKey));
        }

        [Fact]
        public void CheckPermission_Throws_PermissionDenied()
        {
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, adminState.Serialize());
            var action = ValidatorSetOperate.Append(_validator);

            var exc1 = Assert.Throws<PermissionDeniedException>(
                () =>
                {
                    action.Execute(
                        new ActionContext()
                        {
                            BlockIndex = 5,
                            PreviousState = state,
                            Signer = new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"),
                        }
                    );
                });
            Assert.Equal(new Address("019101FEec7ed4f918D396827E1277DEda1e20D4"), exc1.Signer);
        }

        [Fact]
        public void Append_Throws_WhenAlreadyExistValidator()
        {
            var action = ValidatorSetOperate.Append(_validator);
            var exc = Assert.Throws<InvalidOperationException>(
                () =>
                    action.Execute(
                        new ActionContext
                        {
                            PreviousState = _initialState,
                        }));
            Assert.Equal(
                "Cannot append validator when its already exist.",
                exc.Message);
        }

        [Fact]
        public void Update_Throws_WhenDoNotExistValidator()
        {
            var state = new World(MockUtil.MockModernWorldState);
            var action = ValidatorSetOperate.Update(_validator);
            var exc = Assert.Throws<InvalidOperationException>(
                () =>
                    action.Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                        }));
            Assert.Equal(
                "Cannot update validator when its do not exist.",
                exc.Message);
        }

        [Fact]
        public void Remove_Throws_WhenDoNotExistValidator()
        {
            var state = new World(MockUtil.MockModernWorldState);
            var action = ValidatorSetOperate.Remove(_validator);
            var exc = Assert.Throws<InvalidOperationException>(
                () =>
                    action.Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                        }));
            Assert.Equal(
                "Cannot remove validator when its do not exist.",
                exc.Message);
        }

        [Fact]
        public void Append()
        {
            var validatorPubKey = new PrivateKey().PublicKey;
            var validator = new Validator(validatorPubKey, BigInteger.One);
            var action = ValidatorSetOperate.Append(validator);
            var states = action.Execute(
                new ActionContext
                {
                    PreviousState = _initialState,
                });

            var validatorSet = states.GetValidatorSet();
            Assert.Equal(2, validatorSet.Validators.Count);
            Assert.Equal(validator, validatorSet.GetValidator(validatorPubKey));
        }

        [Fact]
        public void Update()
        {
            var validator = new Validator(_validator.PublicKey, 10);
            var action = ValidatorSetOperate.Update(validator);
            var states = action.Execute(
                new ActionContext
                {
                    PreviousState = _initialState,
                });

            var validatorSet = states.GetValidatorSet();
            Assert.Single(validatorSet.Validators);
            Assert.Equal(validator, validatorSet.GetValidator(_validator.PublicKey));
        }

        [Fact]
        public void Remove()
        {
            var action = ValidatorSetOperate.Remove(_validator);
            var states = action.Execute(
                new ActionContext
                {
                    PreviousState = _initialState,
                });

            var validatorSet = states.GetValidatorSet();
            Assert.Empty(validatorSet.Validators);
        }

        [Fact]
        public void Serialization_Append()
        {
            var action = ValidatorSetOperate.Append(_validator);
            var deserialized = new ValidatorSetOperate();
            deserialized.LoadPlainValue(action.PlainValue);

            var dict = Assert.IsType<Dictionary>(action.PlainValue);
            Assert.Equal(new Text("op_validator_set"), dict["type_id"]);
            Assert.Equal(ValidatorSetOperatorType.Append, action.Operator);
            Assert.Equal(_validator, action.Operand);
            Assert.Null(deserialized.Error);
        }

        [Fact]
        public void Serialization_Update()
        {
            var action = ValidatorSetOperate.Update(_validator);
            var deserialized = new ValidatorSetOperate();
            deserialized.LoadPlainValue(action.PlainValue);

            var dict = Assert.IsType<Dictionary>(action.PlainValue);
            Assert.Equal(new Text("op_validator_set"), dict["type_id"]);
            Assert.Equal(ValidatorSetOperatorType.Update, action.Operator);
            Assert.Equal(_validator, action.Operand);
            Assert.Null(deserialized.Error);
        }

        [Fact]
        public void Serialization_Remove()
        {
            var action = ValidatorSetOperate.Remove(_validator);
            var deserialized = new ValidatorSetOperate();
            deserialized.LoadPlainValue(action.PlainValue);

            var dict = Assert.IsType<Dictionary>(action.PlainValue);
            Assert.Equal(new Text("op_validator_set"), dict["type_id"]);
            Assert.Equal(ValidatorSetOperatorType.Remove, action.Operator);
            Assert.Equal(_validator, action.Operand);
            Assert.Null(deserialized.Error);
        }

        [Fact]
        public void LoadPlainValueViaActionLoader()
        {
            var loader = new NCActionLoader();
            var action = ValidatorSetOperate.Append(_validator);
            var loaded = loader.LoadAction(0, action.PlainValue);

            var deserialized = Assert.IsType<ValidatorSetOperate>(loaded);
            Assert.Equal(ValidatorSetOperatorType.Append, deserialized.Operator);
            Assert.Equal(_validator, deserialized.Operand);
            Assert.Null(deserialized.Error);
        }
    }
}
