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
    using Xunit;

    public class RedelegateTest : PoSTest
    {
        [Fact]
        public void Execute()
        {
            var validator1PrivateKey = new PrivateKey();
            var validator2PrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var validator1OperatorAddress = validator1PrivateKey.Address;
            var validator2OperatorAddress = validator2PrivateKey.Address;
            var validator1Address =
                Nekoyume.Action.DPoS.Model.Validator.DeriveAddress(validator1OperatorAddress);
            var validator2Address =
                Nekoyume.Action.DPoS.Model.Validator.DeriveAddress(validator2OperatorAddress);
            var delegatorAddress = delegatorPrivateKey.Address;
            var states = InitialState;

            // Prepare initial governance token for the delegator and validator
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validator1OperatorAddress,
                GovernanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                validator2OperatorAddress,
                GovernanceToken * 100);
            states = states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                delegatorAddress,
                GovernanceToken * 50);

            // Promote the delegator
            states = new PromoteValidator(
                    validator1PrivateKey.PublicKey,
                    amount: 100)
                .Execute(
                    new ActionContext
                        { PreviousState = states, Signer = validator1OperatorAddress });
            states = new PromoteValidator(
                    validator2PrivateKey.PublicKey,
                    amount: 100)
                .Execute(
                    new ActionContext
                        { PreviousState = states, Signer = validator2OperatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = validator1OperatorAddress });

            // Delegate the validator 1
            states = new Nekoyume.Action.DPoS.Delegate(validator1Address, 50).Execute(
                new ActionContext { PreviousState = states, Signer = delegatorAddress });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            Assert.NotNull(
                DelegateCtrl.GetDelegation(
                    states,
                    Delegation.DeriveAddress(delegatorAddress, validator1Address)));
            Assert.Null(
                DelegateCtrl.GetDelegation(
                    states,
                    Delegation.DeriveAddress(delegatorAddress, validator2Address)));

            var bytes = new byte[32];
            new Random().NextBytes(bytes);
            var blockHash = new BlockHash(bytes);
            var power1 = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validator1PrivateKey.PublicKey }.ToList());
            var power2 = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validator2PrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 50) * 100, power1);
            Assert.Equal(100 * 100, power2);

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
                                validator1PrivateKey.PublicKey,
                                power1,
                                VoteFlag.PreCommit).Sign(validator1PrivateKey),
                            new VoteMetadata(
                                0,
                                0,
                                blockHash,
                                DateTimeOffset.UtcNow,
                                validator2PrivateKey.PublicKey,
                                power2,
                                VoteFlag.PreCommit).Sign(validator2PrivateKey),
                        }.ToImmutableArray()),
                });

            var validator1OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator1OperatorAddress),
                GovernanceToken).RawValue;
            var validator2OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator2OperatorAddress),
                GovernanceToken).RawValue;
            var validator1Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator1Address, GovernanceToken),
                GovernanceToken).RawValue;
            var validator2Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator2Address, GovernanceToken),
                GovernanceToken).RawValue;

            // Sum is 500
            Assert.Equal(53, validator1OperatorReward);
            Assert.Equal(19, validator2OperatorReward);
            Assert.Equal(257, validator1Reward);
            Assert.Equal(171, validator2Reward);

            states = new RecordProposer().Execute(
                new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = states,
                    Miner = validator1OperatorAddress,
                });

            // Redelegate to validator 2
            states = new Redelegate(
                validator1Address,
                validator2Address,
                Asset.Share * 30 * 100).Execute(
                new ActionContext
                    { PreviousState = states, BlockIndex = 1, Signer = delegatorAddress });

            validator1OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator1OperatorAddress),
                GovernanceToken).RawValue;
            validator2OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator2OperatorAddress),
                GovernanceToken).RawValue;
            validator1Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator1Address, GovernanceToken),
                GovernanceToken).RawValue;
            validator2Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator2Address, GovernanceToken),
                GovernanceToken).RawValue;
            var delegatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(delegatorAddress),
                GovernanceToken).RawValue;

            // Sum is 1000
            Assert.Equal(224, validator1OperatorReward);
            Assert.Equal(190, validator2OperatorReward);
            Assert.Equal(1, validator1Reward);
            Assert.Equal(0, validator2Reward);
            Assert.Equal(85, delegatorReward);

            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });

            new Random().NextBytes(bytes);
            blockHash = new BlockHash(bytes);
            power1 = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validator1PrivateKey.PublicKey }.ToList());
            power2 = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validator2PrivateKey.PublicKey }.ToList());
            Assert.Equal((100 + 20) * 100, power1);
            Assert.Equal((100 + 30) * 100, power2);

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
                    BlockIndex = 2,
                    LastCommit = new BlockCommit(
                        1,
                        0,
                        blockHash,
                        new[]
                        {
                            new VoteMetadata(
                                1,
                                0,
                                blockHash,
                                DateTimeOffset.UtcNow,
                                validator1PrivateKey.PublicKey,
                                power1,
                                VoteFlag.PreCommit).Sign(validator1PrivateKey),
                            new VoteMetadata(
                                1,
                                0,
                                blockHash,
                                DateTimeOffset.UtcNow,
                                validator2PrivateKey.PublicKey,
                                power2,
                                VoteFlag.PreCommit).Sign(validator2PrivateKey),
                        }.ToImmutableArray()),
                });

            validator1OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator1OperatorAddress),
                GovernanceToken).RawValue;
            validator2OperatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(validator2OperatorAddress),
                GovernanceToken).RawValue;
            validator1Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator1Address, GovernanceToken),
                GovernanceToken).RawValue;
            validator2Reward = states.GetBalance(
                ValidatorRewards.DeriveAddress(validator2Address, GovernanceToken),
                GovernanceToken).RawValue;
            delegatorReward = states.GetBalance(
                AllocateRewardCtrl.RewardAddress(delegatorAddress),
                GovernanceToken).RawValue;

            // Sum is 1000
            Assert.Equal(271, validator1OperatorReward);
            Assert.Equal(214, validator2OperatorReward);
            Assert.Equal(207, validator1Reward);
            Assert.Equal(223, validator2Reward);
            Assert.Equal(85, delegatorReward);
        }
    }
}
