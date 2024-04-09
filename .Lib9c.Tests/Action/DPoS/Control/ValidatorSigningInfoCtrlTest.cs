namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class ValidatorSigningInfoCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _delegatorAddress;
        private readonly Address _validatorAddress;
        private readonly FungibleAssetValue _governanceToken
            = new FungibleAssetValue(Asset.GovernanceToken, 100, 0);

        private IWorld _states;

        public ValidatorSigningInfoCtrlTest()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _delegatorAddress = CreateAddress();
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _states = InitializeStates();
        }

        [Fact]
        public void SetSigningInfo_Test()
        {
            var signingInfo = new ValidatorSigningInfo
            {
                Address = _validatorAddress,
            };

            _states = ValidatorSigningInfoCtrl.SetSigningInfo(
                world: _states,
                signingInfo: signingInfo);
        }

        [Fact]
        public void GetSigningInfo_Test()
        {
            var signingInfo1 = ValidatorSigningInfoCtrl.GetSigningInfo(_states, _validatorAddress);
            Assert.Null(signingInfo1);

            var signingInfo2 = new ValidatorSigningInfo
            {
                Address = _validatorAddress,
            };
            _states = ValidatorSigningInfoCtrl.SetSigningInfo(
                world: _states,
                signingInfo: signingInfo2);

            var signingInfo3 = ValidatorSigningInfoCtrl.GetSigningInfo(_states, _validatorAddress);
            Assert.NotNull(signingInfo3);
            Assert.Equal(_validatorAddress, signingInfo3.Address);
            Assert.Equal(signingInfo2, signingInfo3);
        }

        [Fact]
        public void Tombstone_Test()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: governanceToken);

            states = ValidatorSigningInfoCtrl.Tombstone(states, validatorAddress);

            Assert.True(ValidatorSigningInfoCtrl.IsTombstoned(states, validatorAddress));
        }

        [Fact]
        public void Tombstone_NotPromotedValidator_FailTest()
        {
            var states = _states;
            var validatorAddress = _validatorAddress;

            Assert.Throws<NullValidatorException>(() =>
            {
                ValidatorSigningInfoCtrl.Tombstone(states, validatorAddress);
            });
        }

        [Fact]
        public void Tombstone_TombstonedValidator_FailTest()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: governanceToken);

            states = ValidatorSigningInfoCtrl.Tombstone(states, validatorAddress);

            Assert.Throws<InvalidOperationException>(() =>
            {
                ValidatorSigningInfoCtrl.Tombstone(states, validatorAddress);
            });
        }

        [Fact]
        public void JailUtil_Test()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: governanceToken);

            states = ValidatorSigningInfoCtrl.JailUntil(
                world: states,
                validatorAddress: validatorAddress,
                blockHeight: 2);

            var signingInfo = ValidatorSigningInfoCtrl.GetSigningInfo(states, validatorAddress)!;

            Assert.NotNull(signingInfo);
            Assert.Equal(2, signingInfo.JailedUntil);
        }

        [Fact]
        public void JailUtil_NotPromotedValidator_FailTest()
        {
            var states = _states;
            var validatorAddress = _validatorAddress;

            Assert.Throws<NullValidatorException>(() =>
            {
                ValidatorSigningInfoCtrl.JailUntil(states, validatorAddress, 2);
            });
        }

        [Fact]
        public void JailUtil_NegativeBlockHeight_FailTest()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: governanceToken);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ValidatorSigningInfoCtrl.JailUntil(states, validatorAddress, -1);
            });
        }

        [Fact]
        public void JailUtil_MultipleInvocation_Test()
        {
            var governanceToken = _governanceToken;
            var states = _states;
            var operatorPublicKey = _operatorPublicKey;
            var validatorAddress = _validatorAddress;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: governanceToken);

            states = ValidatorSigningInfoCtrl.JailUntil(
                world: states,
                validatorAddress: validatorAddress,
                blockHeight: 2);

            // Set block height to greater than current
            states = ValidatorSigningInfoCtrl.JailUntil(
                world: states,
                validatorAddress: validatorAddress,
                blockHeight: 3);

            var signingInfo1 = ValidatorSigningInfoCtrl.GetSigningInfo(states, validatorAddress)!;

            Assert.NotNull(signingInfo1);
            Assert.Equal(3, signingInfo1.JailedUntil);

            // Set block height to lower than current
            states = ValidatorSigningInfoCtrl.JailUntil(
                world: states,
                validatorAddress: validatorAddress,
                blockHeight: 1);

            var signingInfo2 = ValidatorSigningInfoCtrl.GetSigningInfo(states, validatorAddress)!;

            Assert.NotNull(signingInfo2);
            Assert.Equal(1, signingInfo2.JailedUntil);
        }
    }
}
