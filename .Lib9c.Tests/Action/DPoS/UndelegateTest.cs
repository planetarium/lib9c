namespace Lib9c.Tests.Action.DPoS
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Consensus;
    using Nekoyume.Action.DPoS;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Action.DPoS.Sys;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class UndelegateTest : PoSTest
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

            // Prepare initial governance token for the delegator and validator
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validatorOperatorAddress,
                GovernanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                delegatorAddress,
                GovernanceToken * 50);

            // Promote the validator
            states = new PromoteValidator(
                validatorPrivateKey.PublicKey,
                amount: GovernanceToken * 100)
                    .Execute(
                        new ActionContext
                            { PreviousState = states, Signer = validatorOperatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = validatorOperatorAddress });

            // Delegate the delegator
            states = new Nekoyume.Action.DPoS.Delegate(validatorAddress, GovernanceToken * 50).Execute(
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

            // Undelegate the delegator
            states = new Undelegate(validatorAddress, Asset.Share * 30 * 100).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50) * 100 - 3000, power);

            var bytes = new byte[32];
            new Random().NextBytes(bytes);
            var blockHash = new BlockHash(bytes);

            // Mint and allocate rewards
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                ReservedAddress.RewardPool,
                GovernanceToken * 5);
            states = new AllocateReward().Execute(
                new ActionContext
                {
                    PreviousState = states,
                    BlockIndex = 1,
                    LastCommit = new BlockCommit(
                        0,
                        0,
                        blockHash,
                        new[]
                        {
                            new VoteMetadata(
                                0,
                                0,
                                blockHash,
                                DateTimeOffset.UtcNow,
                                validatorPrivateKey.PublicKey,
                                power,
                                VoteFlag.PreCommit).Sign(validatorPrivateKey),
                        }.ToImmutableArray()),
                });

            var validatorOperatorReward = states.GetBalance(
                    AllocateRewardCtrl.RewardAddress(validatorOperatorAddress),
                    GovernanceToken).RawValue;
            var validatorReward = states.GetBalance(
                    ValidatorRewards.DeriveAddress(validatorAddress, GovernanceToken),
                    GovernanceToken).RawValue;
            Assert.Equal(72, validatorOperatorReward);
            Assert.Equal(428, validatorReward);

            states = new WithdrawDelegator(validatorAddress).Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = 2, Signer = delegatorAddress });
            Assert.Equal(
                71,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);
            Assert.Equal(
                0,
                states.GetBalance(validatorOperatorAddress, GovernanceToken).RawValue);
            validatorReward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validatorAddress, GovernanceToken),
                GovernanceToken).RawValue;
            Assert.Equal(357, validatorReward);

            // Check if balance is not yet returned to the delegator
            states = new UpdateValidators().Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = UnbondingSet.Period - 1 });
            Assert.Equal(
                71,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);

            // Check if balance is returned to the delegator
            states = new UpdateValidators().Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = UnbondingSet.Period + 10 });
            Assert.Equal(
                71 + 30 * 100,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);
        }

        [Fact]
        public void UndelegateAll()
        {
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var validatorOperatorAddress = validatorPrivateKey.Address;
            var validatorAddress = Nekoyume.Action.DPoS.Model.Validator.DeriveAddress(validatorOperatorAddress);
            var delegatorAddress = delegatorPrivateKey.Address;
            var states = InitialState;

            // Prepare initial governance token for the delegator and validator
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validatorOperatorAddress,
                GovernanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                delegatorAddress,
                GovernanceToken * 50);

            // Promote the validator
            states = new PromoteValidator(
                    validatorPrivateKey.PublicKey,
                    amount: GovernanceToken * 100)
                .Execute(
                    new ActionContext
                        { PreviousState = states, Signer = validatorOperatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = validatorOperatorAddress });

            // Delegate the delegator
            states = new Nekoyume.Action.DPoS.Delegate(validatorAddress, GovernanceToken * 50).Execute(
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

            // Undelegate the delegator
            states = new Undelegate(validatorAddress, Asset.Share * 5000).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal(100 * 100, power);

            var bytes = new byte[32];
            new Random().NextBytes(bytes);
            var blockHash = new BlockHash(bytes);

            // Mint and allocate rewards
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                ReservedAddress.RewardPool,
                GovernanceToken * 5);
            states = new AllocateReward().Execute(
                new ActionContext
                {
                    PreviousState = states,
                    BlockIndex = 1,
                    LastCommit = new BlockCommit(
                        0,
                        0,
                        blockHash,
                        new[]
                        {
                            new VoteMetadata(
                                0,
                                0,
                                blockHash,
                                DateTimeOffset.UtcNow,
                                validatorPrivateKey.PublicKey,
                                power,
                                VoteFlag.PreCommit).Sign(validatorPrivateKey),
                        }.ToImmutableArray()),
                });

            var validatorOperatorReward = states.GetBalance(
                    AllocateRewardCtrl.RewardAddress(validatorOperatorAddress),
                    GovernanceToken).RawValue;
            var validatorReward = states.GetBalance(
                    ValidatorRewards.DeriveAddress(validatorAddress, GovernanceToken),
                    GovernanceToken).RawValue;
            Assert.Equal(72, validatorOperatorReward);
            Assert.Equal(428, validatorReward);

            states = new WithdrawDelegator(validatorAddress).Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = 2, Signer = delegatorAddress });
            Assert.Equal(
                0,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);
            Assert.Equal(
                0,
                states.GetBalance(validatorOperatorAddress, GovernanceToken).RawValue);
            validatorReward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validatorAddress, GovernanceToken),
                GovernanceToken).RawValue;
            Assert.Equal(428, validatorReward);

            // Check if balance is not yet returned to the delegator
            states = new UpdateValidators().Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = UnbondingSet.Period - 1 });
            Assert.Equal(
                0,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);

            // Check if balance is returned to the delegator
            states = new UpdateValidators().Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = UnbondingSet.Period + 10 });
            Assert.Equal(
                50 * 100,
                states.GetBalance(delegatorAddress, GovernanceToken).RawValue);
        }
    }
}
