namespace Lib9c.Tests.Action.DPoS.Control
{
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Module;
    using Xunit;

    public class ValidatorDelegationSetCtrlTest : PoSTest
    {
        private readonly PublicKey _operatorPublicKey;
        private readonly Address _operatorAddress;
        private readonly Address _validatorAddress;
        private readonly ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public ValidatorDelegationSetCtrlTest()
            : base()
        {
            _operatorPublicKey = new PrivateKey().PublicKey;
            _operatorAddress = _operatorPublicKey.Address;
            _validatorAddress = Validator.DeriveAddress(_operatorAddress);
            _nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            _states = InitializeStates();
            _states = _states.MintAsset(
                context: new ActionContext { PreviousState = _states, },
                recipient: _operatorAddress,
                value: Asset.GovernanceToken * 100000);
            _states = ValidatorCtrl.Create(
                states: _states,
                ctx: new ActionContext { PreviousState = _states, },
                operatorAddress: _operatorAddress,
                operatorPublicKey: _operatorPublicKey,
                Asset.GovernanceToken * 10,
                nativeTokens: _nativeTokens
            );
        }

        [Fact]
        public void PromoteTest()
        {
            var validatorDelegationSet = ValidatorDelegationSetCtrl.GetValidatorDelegationSet(
                _states,
                _validatorAddress
            );
            Assert.NotNull(validatorDelegationSet);
            Assert.Single(validatorDelegationSet.Set);
            var delegation = DelegateCtrl.GetDelegation(
                states: _states,
                delegationAddress: validatorDelegationSet.Set[0]
            );
            Assert.NotNull(delegation);
            Assert.Equal(_validatorAddress, delegation.ValidatorAddress);
            Assert.Equal(_operatorAddress, delegation.DelegatorAddress);
        }
    }
}
