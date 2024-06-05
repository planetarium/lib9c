namespace Lib9c.Tests.Action.DPoS
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.DPoS;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Action.DPoS.Sys;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class CancelUndelegationTest : PoSTest
    {
        [Fact]
        public void Execute()
        {
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var validatorOperatorAddress = validatorPrivateKey.Address;
            var validatorAddress =
                Nekoyume.Action.DPoS.Model.Validator.DeriveAddress(validatorOperatorAddress);
            var delegatorAddress = delegatorPrivateKey.Address;
            var states = InitialState;
            var governanceToken = states.GetGoldCurrency();

            // Prepare initial governance token for the delegator and validator
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validatorOperatorAddress,
                governanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                delegatorAddress,
                governanceToken * 50);

            // Promote the validator
            states = new PromoteValidator(
                validatorPrivateKey.PublicKey,
                amount: governanceToken * 100)
                    .Execute(
                        new ActionContext
                            { PreviousState = states, Signer = validatorOperatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = validatorOperatorAddress });

            // Delegate the delegator
            states = new Nekoyume.Action.DPoS.Delegate(validatorAddress, governanceToken * 50).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            var delegation = DelegateCtrl.GetDelegation(
                states,
                Delegation.DeriveAddress(delegatorAddress, validatorAddress));
            Assert.NotNull(delegation);
            Assert.Equal(delegatorAddress, delegation.DelegatorAddress);
            Assert.Equal(validatorAddress, delegation.ValidatorAddress);
            var power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50) * 100, power);

            // Undelegate and cancel the delegator
            states = new Undelegate(validatorAddress, Asset.Share * 30 * 100).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new CancelUndelegation(validatorAddress, Asset.ConsensusToken * 20 * 100).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50 - 30 + 20) * 100, power);
        }

        [Fact]
        public void CancelAll()
        {
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var validatorOperatorAddress = validatorPrivateKey.Address;
            var validatorAddress =
                Nekoyume.Action.DPoS.Model.Validator.DeriveAddress(validatorOperatorAddress);
            var delegatorAddress = delegatorPrivateKey.Address;
            var states = InitialState;
            var governanceToken = states.GetGoldCurrency();

            // Prepare initial governance token for the delegator and validator
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validatorOperatorAddress,
                governanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                delegatorAddress,
                governanceToken * 50);

            // Promote the validator
            states = new PromoteValidator(
                validatorPrivateKey.PublicKey,
                amount: governanceToken * 100)
                    .Execute(
                        new ActionContext
                            { PreviousState = states, Signer = validatorOperatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = validatorOperatorAddress });

            // Delegate the delegator
            states = new Nekoyume.Action.DPoS.Delegate(validatorAddress, governanceToken * 50).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            var delegation = DelegateCtrl.GetDelegation(
                states,
                Delegation.DeriveAddress(delegatorAddress, validatorAddress));
            Assert.NotNull(delegation);
            Assert.Equal(delegatorAddress, delegation.DelegatorAddress);
            Assert.Equal(validatorAddress, delegation.ValidatorAddress);
            var power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50) * 100, power);

            // Undelegate and cancel the delegator
            states = new Undelegate(validatorAddress, Asset.Share * 30 * 100).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new CancelUndelegation(validatorAddress, Asset.ConsensusToken * 30 * 100).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50) * 100, power);
        }
    }
}
