using System.Collections.Immutable;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Exception;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;
using Xunit;

namespace Lib9c.DPoS.Tests.Control
{
    public class ValidatorCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _validatorAddress;
        private readonly ImmutableHashSet<Currency> _nativeTokens;
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
                Asset.ConsensusToken * 50);
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
                    Asset.ConsensusToken * 30,
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
                Asset.ConsensusToken * selfDelegateAmount,
                _states.GetBalance(_validatorAddress, Asset.ConsensusToken));
            Assert.Equal(
                Asset.GovernanceToken * (mintAmount - selfDelegateAmount),
                _states.GetBalance(_operatorAddress, Asset.GovernanceToken));
            Assert.Equal(
                Asset.Share * selfDelegateAmount,
                _states.GetBalance(
                    Delegation.DeriveAddress(_operatorAddress, _validatorAddress), Asset.Share));
            Assert.Equal(
                Asset.Share * selfDelegateAmount,
                ValidatorCtrl.GetValidator(_states, _validatorAddress)!.DelegatorShares);
        }
    }
}
