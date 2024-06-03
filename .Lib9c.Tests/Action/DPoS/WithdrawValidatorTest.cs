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
    using Nekoyume.Action.DPoS.Sys;
    using Xunit;

    public class WithdrawValidatorTest : PoSTest
    {
        [Fact]
        public void Execute()
        {
            var validatorPrivateKey = new PrivateKey();
            var address = validatorPrivateKey.Address;
            var rewardAddress = AllocateRewardCtrl.RewardAddress(address);
            var states = InitializeStates();
            var amount = Asset.GovernanceToken * 100;
            states = states.MintAsset(
                new ActionContext { PreviousState = states },
                address,
                amount);
            states = new PromoteValidator(validatorPrivateKey.PublicKey, amount: amount).Execute(
                new ActionContext { PreviousState = states, Signer = address });
            states = new UpdateValidators().Execute(new ActionContext { PreviousState = states });
            states = new RecordProposer().Execute(
                new ActionContext { PreviousState = states, Miner = address });
            var bytes = new byte[32];
            new Random().NextBytes(bytes);
            var blockHash = new BlockHash(bytes);
            var power = states.GetValidatorSet()
                .GetValidatorsPower(
                    new[] { validatorPrivateKey.PublicKey }.ToList());
            Assert.Equal(10000, power);
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
            Assert.Equal(
                0,
                states.GetBalance(address, Asset.GovernanceToken).RawValue);
            Assert.Equal(
                72,
                states.GetBalance(rewardAddress, Asset.GovernanceToken).RawValue);
            states = new WithdrawValidator().Execute(
                new ActionContext { PreviousState = states, Signer = address });
            Assert.Equal(
                72,
                states.GetBalance(address, Asset.GovernanceToken).RawValue);
            Assert.Equal(
                0,
                states.GetBalance(rewardAddress, Asset.GovernanceToken).RawValue);
        }
    }
}
