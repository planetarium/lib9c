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
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class DelegateCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _delegatorAddress;
        private readonly Address _validatorAddress;
        private ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public DelegateCtrlTest()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _delegatorAddress = CreateAddress();
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _states = InitialState;
            _nativeTokens = NativeTokens;
        }

        [Fact]
        public void InvalidCurrencyTest()
        {
            Initialize(500, 500, 100);
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                Asset.ConvertTokens(GovernanceToken * 50, Asset.ConsensusToken));
            Assert.Throws<InvalidCurrencyException>(
                    () => _states = DelegateCtrl.Execute(
                        _states,
                        new ActionContext
                        {
                            PreviousState = _states,
                            BlockIndex = 1,
                        },
                        _delegatorAddress,
                        _validatorAddress,
                        Asset.ConvertTokens(GovernanceToken * 30, Asset.ConsensusToken),
                        _nativeTokens));
        }

        [Fact]
        public void InvalidValidatorTest()
        {
            Initialize(500, 500, 100);
            Assert.Throws<NullValidatorException>(
                    () => _states = DelegateCtrl.Execute(
                        _states,
                        new ActionContext
                        {
                            PreviousState = _states,
                            BlockIndex = 1,
                        },
                        _delegatorAddress,
                        CreateAddress(),
                        GovernanceToken * 10,
                        _nativeTokens));
        }

        [Fact]
        public void InvalidShareTest()
        {
            Initialize(500, 500, 100);
            _states = _states.BurnAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _validatorAddress,
                Asset.ConvertTokens(PoSTest.GovernanceToken * 100, Asset.ConsensusToken));
            Assert.Throws<InvalidExchangeRateException>(
                () => _states = DelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    GovernanceToken * 10,
                    _nativeTokens));
        }

        [Theory]
        [InlineData(500, 500, 100, 10)]
        [InlineData(500, 500, 100, 20)]
        public void BalanceTest(
            int operatorMintAmount,
            int delegatorMintAmount,
            int selfDelegateAmount,
            int delegateAmount)
        {
            Initialize(operatorMintAmount, delegatorMintAmount, selfDelegateAmount);
            _states = DelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                _validatorAddress,
                GovernanceToken * delegateAmount,
                _nativeTokens);
            Assert.Equal(
                GovernanceToken * 0,
                _states.GetBalance(_validatorAddress, GovernanceToken));
            Assert.Equal(
                Asset.ConvertTokens(GovernanceToken * 0, Asset.ConsensusToken),
                _states.GetBalance(_operatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                Asset.ConvertTokens(GovernanceToken * 0, Asset.ConsensusToken),
                _states.GetBalance(_delegatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_operatorAddress, Asset.Share));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_delegatorAddress, Asset.Share));
            Assert.Equal(
                Asset.ConvertTokens(GovernanceToken * (selfDelegateAmount + delegateAmount), Asset.ConsensusToken),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                GovernanceToken * (operatorMintAmount - selfDelegateAmount),
                _states.GetBalance(_operatorAddress, GovernanceToken));
            Assert.Equal(
                GovernanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, GovernanceToken));
            Assert.Equal(
                GovernanceToken * (selfDelegateAmount + delegateAmount),
                _states.GetBalance(ReservedAddress.UnbondedPool, GovernanceToken));
            var balanceA = _states.GetBalance(
                Delegation.DeriveAddress(_operatorAddress, _validatorAddress),
                Asset.Share);
            var balanceB = _states.GetBalance(
                Delegation.DeriveAddress(_delegatorAddress, _validatorAddress),
                Asset.Share);
            Assert.Equal(
                ValidatorCtrl.GetValidator(_states, _validatorAddress)!.DelegatorShares,
                balanceA + balanceB);
        }

        private void Initialize(
            int operatorMintAmount, int delegatorMintAmount, int selfDelegateAmount)
        {
            _states = _states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                _operatorAddress,
                GovernanceToken * operatorMintAmount);
            _states = _states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                _delegatorAddress,
                GovernanceToken * delegatorMintAmount);
            _states = ValidatorCtrl.Create(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                _operatorPublicKey,
                GovernanceToken * selfDelegateAmount,
                _nativeTokens);
        }
    }
}
