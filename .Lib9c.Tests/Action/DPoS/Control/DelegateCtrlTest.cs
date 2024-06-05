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
            var governanceToken = _states.GetGoldCurrency();
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
                        governanceToken * 10,
                        _nativeTokens));
        }

        [Fact]
        public void InvalidShareTest()
        {
            Initialize(500, 500, 100);
            var governanceToken = _states.GetGoldCurrency();
            _states = _states.BurnAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _validatorAddress,
                Asset.ConvertTokens(GovernanceToken * 100, Asset.ConsensusToken));
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
                    governanceToken * 10,
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
            var governanceToken = _states.GetGoldCurrency();
            _states = DelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                _validatorAddress,
                governanceToken * delegateAmount,
                _nativeTokens);
            Assert.Equal(
                governanceToken * 0,
                _states.GetBalance(_validatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConvertTokens(governanceToken * 0, Asset.ConsensusToken),
                _states.GetBalance(_operatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                Asset.ConvertTokens(governanceToken * 0, Asset.ConsensusToken),
                _states.GetBalance(_delegatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_operatorAddress, Asset.Share));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_delegatorAddress, Asset.Share));
            Assert.Equal(
                Asset.ConvertTokens(governanceToken * (selfDelegateAmount + delegateAmount), Asset.ConsensusToken),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                governanceToken * (operatorMintAmount - selfDelegateAmount),
                _states.GetBalance(_operatorAddress, governanceToken));
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                governanceToken * (selfDelegateAmount + delegateAmount),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
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
            var governanceToken = _states.GetGoldCurrency();
            _states = _states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                _operatorAddress,
                governanceToken * operatorMintAmount);
            _states = _states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                _delegatorAddress,
                governanceToken * delegatorMintAmount);
            _states = ValidatorCtrl.Create(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _operatorAddress,
                _operatorPublicKey,
                governanceToken * selfDelegateAmount,
                _nativeTokens);
        }
    }
}
