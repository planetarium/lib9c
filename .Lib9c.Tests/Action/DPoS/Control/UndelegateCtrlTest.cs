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

    public class UndelegateCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _delegatorAddress;
        private readonly Address _validatorAddress;
        private readonly Address _undelegationAddress;
        private ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public UndelegateCtrlTest()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _delegatorAddress = CreateAddress();
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _undelegationAddress = Undelegation.DeriveAddress(_delegatorAddress, _validatorAddress);
            _states = InitialState;
            _nativeTokens = NativeTokens;
        }

        [Fact]
        public void InvalidCurrencyTest()
        {
            Initialize(500, 500, 100, 100);
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                Asset.ConsensusFromGovernance(GovernanceToken * 50));
            Assert.Throws<InvalidCurrencyException>(
                () => _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    Asset.ConsensusFromGovernance(GovernanceToken * 30),
                    _nativeTokens));
            Assert.Throws<InvalidCurrencyException>(
                () => _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    GovernanceToken * 30,
                    _nativeTokens));
        }

        [Fact]
        public void InvalidValidatorTest()
        {
            Initialize(500, 500, 100, 100);
            Assert.Throws<NullValidatorException>(
                () => _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    CreateAddress(),
                    ShareFromGovernance(10),
                    _nativeTokens));
        }

        [Fact]
        public void MaxEntriesTest()
        {
            Initialize(500, 500, 100, 100);
            for (long i = 0; i < 10; i++)
            {
                _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = i,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    ShareFromGovernance(1),
                    _nativeTokens);
            }

            Assert.Throws<MaximumUndelegationEntriesException>(
                () => _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    ShareFromGovernance(1),
                    _nativeTokens));
        }

        [Fact]
        public void ExceedUndelegateTest()
        {
            Initialize(500, 500, 100, 100);
            Assert.Throws<InsufficientFungibleAssetValueException>(
                () => _states = UndelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    _delegatorAddress,
                    _validatorAddress,
                    ShareFromGovernance(101),
                    _nativeTokens));
        }

        [Theory]
        [InlineData(500, 500, 100, 100, 100)]
        [InlineData(500, 500, 100, 100, 50)]
        public void CompleteUnbondingTest(
            int operatorMintAmount,
            int delegatorMintAmount,
            int selfDelegateAmount,
            int delegateAmount,
            int undelegateAmount)
        {
            Initialize(operatorMintAmount, delegatorMintAmount, selfDelegateAmount, delegateAmount);
            var governanceToken = _states.GetGoldCurrency();
            _states = UndelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                _validatorAddress,
                ShareFromGovernance(undelegateAmount),
                _nativeTokens);
            Assert.Single(
                UndelegateCtrl.GetUndelegation(_states, _undelegationAddress)!
                .UndelegationEntryAddresses);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                governanceToken * (selfDelegateAmount + delegateAmount),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            _states = UndelegateCtrl.Complete(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1000,
                },
                _undelegationAddress);
            Assert.Single(UndelegateCtrl.GetUndelegation(_states, _undelegationAddress)!
                .UndelegationEntryAddresses);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                governanceToken * (selfDelegateAmount + delegateAmount),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            _states = UndelegateCtrl.Complete(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 50400 * 5,
                },
                _undelegationAddress);
            Assert.Empty(UndelegateCtrl.GetUndelegation(_states, _undelegationAddress)!
                .UndelegationEntryAddresses);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount + undelegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                governanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
        }

        [Theory]
        [InlineData(500, 500, 100, 100, 100, 30)]
        [InlineData(500, 500, 100, 100, 50, 30)]
        public void CancelUndelegateTest(
            int operatorMintAmount,
            int delegatorMintAmount,
            int selfDelegateAmount,
            int delegateAmount,
            int undelegateAmount,
            int cancelAmount)
        {
            Initialize(operatorMintAmount, delegatorMintAmount, selfDelegateAmount, delegateAmount);
            var governanceToken = _states.GetGoldCurrency();
            _states = UndelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                _validatorAddress,
                ShareFromGovernance(undelegateAmount),
                _nativeTokens);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount)),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            _states = UndelegateCtrl.Cancel(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 2,
                },
                _undelegationAddress,
                Asset.ConsensusFromGovernance(GovernanceToken * cancelAmount),
                _nativeTokens);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(
                    GovernanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount + cancelAmount)),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            _states = UndelegateCtrl.Complete(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1000,
                },
                _undelegationAddress);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(
                    GovernanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount + cancelAmount)),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            _states = UndelegateCtrl.Complete(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 50400 * 5,
                },
                _undelegationAddress);
            Assert.Equal(
                governanceToken * (delegatorMintAmount - delegateAmount + undelegateAmount - cancelAmount),
                _states.GetBalance(_delegatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(
                    GovernanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount + cancelAmount)),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
        }

        [Theory]
        [InlineData(500, 500, 100, 100, 100)]
        [InlineData(500, 500, 100, 100, 50)]
        public void BalanceTest(
            int operatorMintAmount,
            int delegatorMintAmount,
            int selfDelegateAmount,
            int delegateAmount,
            int undelegateAmount)
        {
            Initialize(operatorMintAmount, delegatorMintAmount, selfDelegateAmount, delegateAmount);
            var governanceToken = _states.GetGoldCurrency();
            _states = UndelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                _delegatorAddress,
                _validatorAddress,
                ShareFromGovernance(undelegateAmount),
                _nativeTokens);
            Assert.Equal(
                governanceToken * 0,
                _states.GetBalance(_validatorAddress, governanceToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * 0),
                _states.GetBalance(_operatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * 0),
                _states.GetBalance(_delegatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_operatorAddress, Asset.Share));
            Assert.Equal(
                ShareFromGovernance(0),
                _states.GetBalance(_delegatorAddress, Asset.Share));
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
                Delegation.DeriveAddress(
                    _operatorAddress,
                    _validatorAddress),
                Asset.Share);
            var balanceB = _states.GetBalance(
                Delegation.DeriveAddress(
                    _delegatorAddress,
                    _validatorAddress),
                Asset.Share);
            Assert.Equal(
                ValidatorCtrl.GetValidator(_states, _validatorAddress)!.DelegatorShares,
                balanceA + balanceB);
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * (selfDelegateAmount + delegateAmount - undelegateAmount)),
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
        }

        private void Initialize(
            int operatorMintAmount,
            int delegatorMintAmount,
            int selfDelegateAmount,
            int delegateAmount)
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
        }
    }
}
