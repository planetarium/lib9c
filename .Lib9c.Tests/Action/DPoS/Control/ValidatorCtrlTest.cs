namespace Lib9c.Tests.Action.DPoS.Control
{
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Module;
    using Xunit;

    public class ValidatorCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _validatorAddress;
        private readonly ImmutableHashSet<Currency> _nativeTokens;
        private readonly FungibleAssetValue _governanceToken
            = new FungibleAssetValue(Asset.GovernanceToken, 100, 0);

        private IWorld _states;

        public ValidatorCtrlTest()
            : base()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            _states = InitializeStates();
        }

        [Fact]
        public void InvalidCurrencyTest()
        {
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                Asset.ConsensusFromGovernance(50));
            Assert.Throws<InvalidCurrencyException>(
                () => _states = ValidatorCtrl.Create(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _operatorAddress,
                    _operatorPublicKey,
                    Asset.ConsensusFromGovernance(30),
                    _nativeTokens));
        }

        [Theory]
        [InlineData(500, 0)]
        [InlineData(500, 1000)]
        public void InvalidSelfDelegateTest(int mintAmount, int selfDelegateAmount)
        {
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                Asset.GovernanceToken * mintAmount);
            Assert.Throws<InsufficientFungibleAssetValueException>(
                () => _states = ValidatorCtrl.Create(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _operatorAddress,
                    _operatorPublicKey,
                    Asset.GovernanceToken * selfDelegateAmount,
                    _nativeTokens));
        }

        [Theory]
        [InlineData(500, 10)]
        [InlineData(500, 100)]
        public void BalanceTest(int mintAmount, int selfDelegateAmount)
        {
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                Asset.GovernanceToken * mintAmount);
            _states = ValidatorCtrl.Create(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                _operatorPublicKey,
                Asset.GovernanceToken * selfDelegateAmount,
                _nativeTokens);
            Assert.Equal(
                Asset.ConsensusFromGovernance(selfDelegateAmount),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                Asset.GovernanceToken * (mintAmount - selfDelegateAmount),
                _states.GetBalance(_operatorAddress, Asset.GovernanceToken));
            Assert.Equal(
                ShareFromGovernance(selfDelegateAmount),
                _states.GetBalance(
                    Delegation.DeriveAddress(_operatorAddress, _validatorAddress), Asset.Share));
            Assert.Equal(
                ShareFromGovernance(selfDelegateAmount),
                ValidatorCtrl.GetValidator(_states, _validatorAddress)!.DelegatorShares);
        }

        [Fact]
        public void JailTest()
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

            // Test before jailing
            var validator1 = ValidatorCtrl.GetValidator(states, validatorAddress)!;
            var powerIndex1 = ValidatorPowerIndexCtrl.GetValidatorPowerIndex(states)!;
            Assert.False(validator1.Jailed);
            Assert.Contains(
                powerIndex1.ValidatorAddresses,
                address => address.Equals(validatorAddress));

            // Jail
            states = ValidatorCtrl.Jail(
                states,
                validatorAddress: validatorAddress);

            // Test after jailing
            var validator2 = ValidatorCtrl.GetValidator(states, validatorAddress)!;
            var powerIndex2 = ValidatorPowerIndexCtrl.GetValidatorPowerIndex(states)!;

            Assert.True(validator2.Jailed);
            Assert.DoesNotContain(
                powerIndex2.ValidatorAddresses,
                address => address.Equals(validatorAddress));
        }

        [Fact]
        public void Jail_NotPromotedValidator_FailTest()
        {
            Assert.Throws<NullValidatorException>(() =>
            {
                ValidatorCtrl.Jail(
                    world: _states,
                    validatorAddress: _validatorAddress);
            });
        }

        [Fact]
        public void Jail_JailedValidator_FailTest()
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

            // Jail
            states = ValidatorCtrl.Jail(
                states,
                validatorAddress: validatorAddress);

            Assert.Throws<JailedValidatorException>(() =>
            {
                ValidatorCtrl.Jail(
                    world: states,
                    validatorAddress: validatorAddress);
            });
        }

        [Fact]
        public void UnjailTest()
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

            states = ValidatorCtrl.Jail(
                states,
                validatorAddress: _validatorAddress);

            // Test before unjailing
            var validator1 = ValidatorCtrl.GetValidator(states, validatorAddress)!;
            var powerIndex1 = ValidatorPowerIndexCtrl.GetValidatorPowerIndex(states)!;

            Assert.True(validator1.Jailed);
            Assert.DoesNotContain(
                powerIndex1.ValidatorAddresses,
                address => address.Equals(_validatorAddress));

            // Unjail
            states = ValidatorCtrl.Unjail(
                states,
                validatorAddress: validatorAddress);

            // Test after unjailing
            var validator2 = ValidatorCtrl.GetValidator(states, validatorAddress)!;
            var powerIndex2 = ValidatorPowerIndexCtrl.GetValidatorPowerIndex(states)!;
            Assert.False(validator2.Jailed);
            Assert.Contains(
                powerIndex2.ValidatorAddresses,
                address => address.Equals(validatorAddress));
        }

        [Fact]
        public void Unjail_NotPromotedValidator_FailTest()
        {
            Assert.Throws<NullValidatorException>(() =>
            {
                ValidatorCtrl.Unjail(
                    world: _states,
                    validatorAddress: _validatorAddress);
            });
        }

        [Fact]
        public void Unjail_NotJailedValidator_FailTest()
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

            Assert.Throws<JailedValidatorException>(() =>
            {
                ValidatorCtrl.Unjail(
                    world: states,
                    validatorAddress: validatorAddress);
            });
        }
    }
}
