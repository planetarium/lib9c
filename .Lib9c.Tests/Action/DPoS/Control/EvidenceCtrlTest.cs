namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Module;
    using Xunit;
    using Environment = Nekoyume.Action.DPoS.Control.Environment;

    public class EvidenceCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _delegatorAddress;
        private readonly Address _validatorAddress;
        private readonly FungibleAssetValue _governanceToken
            = new FungibleAssetValue(Asset.GovernanceToken, 100, 0);

        private IWorld _states;

        public EvidenceCtrlTest()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _delegatorAddress = CreateAddress();
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _states = InitializeStates();
        }

        [Fact]
        public void Execute_Test()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                ncg: governanceToken);
            states = Update(
                states: states,
                blockIndex: 1);

            var power = GetPower(states, validatorAddress);
            var evidence = new Evidence()
            {
                Address = validatorAddress,
                Height = 1,
                Power = power.RawValue,
            };

            states = EvidenceCtrl.Execute(
                world: states,
                actionContext: new ActionContext() { PreviousState = states, BlockIndex = 2 },
                validatorAddress: validatorAddress,
                evidence: evidence,
                nativeTokens: NativeTokens);

            var validator = ValidatorCtrl.GetValidator(states, validatorAddress);
            var signingInfo = ValidatorSigningInfoCtrl.GetSigningInfo(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);

            Assert.NotEqual(power, actualPower);
            Assert.True(validator.Jailed);
            //Assert.Equal(BondingStatus.Unbonded, validator.Status);
            Assert.Equal(long.MaxValue, signingInfo.JailedUntil);
            Assert.True(signingInfo.Tombstoned);
        }

        [Fact]
        public void Execute_MaxAge_Test()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;
            var blockIndex = 1;

            states = Promote(
                states: states,
                blockIndex: blockIndex,
                operatorPublicKey: operatorPublicKey,
                ncg: governanceToken);
            states = Update(
                states: states,
                blockIndex: blockIndex);

            var farFutureHeight = blockIndex + Environment.MaxAgeNumBlocks + 1;
            var expectedPower = GetPower(states, validatorAddress);
            var evidence = new Evidence()
            {
                Address = validatorAddress,
                Height = blockIndex,
                Power = expectedPower.RawValue,
            };

            states = EvidenceCtrl.Execute(
                world: states,
                actionContext: new ActionContext() { PreviousState = states, BlockIndex = farFutureHeight },
                validatorAddress: validatorAddress,
                evidence: evidence,
                nativeTokens: NativeTokens);

            var validator = ValidatorCtrl.GetValidator(states, validatorAddress);
            var signingInfo = ValidatorSigningInfoCtrl.GetSigningInfo(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);

            Assert.Equal(expectedPower, actualPower);
            Assert.False(validator.Jailed);
            Assert.NotEqual(long.MaxValue, signingInfo.JailedUntil);
            Assert.False(signingInfo.Tombstoned);
        }

        [Fact]
        public void Execute_NotPromotedValidator_FailTest()
        {
            var states = _states;
            var validatorAddress = _validatorAddress;
            var power = GetPower(states, validatorAddress);
            var evidence = new Evidence()
            {
                Address = validatorAddress,
                Height = 1,
                Power = power.RawValue,
            };

            Assert.Throws<NullValidatorException>(() =>
            {
                states = EvidenceCtrl.Execute(
                    world: states,
                    actionContext: new ActionContext() { PreviousState = states, BlockIndex = 2 },
                    validatorAddress: validatorAddress,
                    evidence: evidence,
                    nativeTokens: NativeTokens);
            });
        }

        private static FungibleAssetValue GetPower(IWorldState worldState, Address validatorAddress)
        {
            return worldState.GetBalance(
                address: validatorAddress,
                currency: Asset.ConsensusToken);
        }
    }
}
