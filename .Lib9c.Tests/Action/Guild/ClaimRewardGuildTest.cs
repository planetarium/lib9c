namespace Lib9c.Tests.Action.Guild
{
    using System.Linq;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.Stake;
    using Xunit;

    public class ClaimRewardGuildTest : GuildTestBase
    {
        [Fact]
        public void Serialization()
        {
            var action = new ClaimRewardGuild();
            var plainValue = action.PlainValue;

            var deserialized = new ClaimRewardGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact(Skip = "Allow after bond on actual block height")]
        public void Execute()
        {
            // Given
            var validatorKey = new PrivateKey();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var nParticipants = 10;
            var guildParticipantAddresses = Enumerable.Range(0, nParticipants).Select(
                _ => AddressUtil.CreateAgentAddress()).ToList();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMintAsset(world, StakeState.DeriveAddress(guildMasterAddress), GG * 100);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = guildParticipantAddresses.Select((addr, idx) => (addr, idx)).Aggregate(world, (w, item) =>
            {
                w = EnsureToMintAsset(w, item.addr, Mead * 100);
                w = EnsureToMintAsset(w, StakeState.DeriveAddress(item.addr), GG * ((item.idx + 1) * 100));
                return EnsureToJoinGuild(w, guildAddress, item.addr, 1L);
            });

            // When
            var repository = new GuildRepository(world, new ActionContext());
            var guild = repository.GetGuild(guildAddress);
            var reward = NCG * 1000;
            repository.UpdateWorld(EnsureToMintAsset(repository.World, guild.RewardPoolAddress, reward));
            guild.CollectRewards(1);
            world = repository.World;

            for (var i = 0; i < nParticipants; i++)
            {
                var claimRewardGuild = new ClaimRewardGuild();
                var actionContext = new ActionContext
                {
                    PreviousState = world,
                    Signer = guildParticipantAddresses[i],
                    BlockIndex = 2L,
                };
                world = claimRewardGuild.Execute(actionContext);
            }

            //Then
            var expectedRepository = new GuildRepository(world, new ActionContext());
            for (var i = 0; i < nParticipants; i++)
            {
                var expectedGuild = expectedRepository.GetGuild(guildAddress);
                var bond = expectedRepository.GetBond(expectedGuild, guildParticipantAddresses[i]);
                var expectedReward = (reward * bond.Share).DivRem(expectedGuild.TotalShares).Quotient;
                var actualReward = world.GetBalance(guildParticipantAddresses[i], NCG);

                Assert.Equal(expectedReward, actualReward);
            }
        }

        [Fact(Skip = "Allow after bond on actual block height")]
        public void SkipDuplicatedClaim()
        {
            // Given
            var validatorKey = new PrivateKey();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var nParticipants = 10;
            var guildParticipantAddresses = Enumerable.Range(0, nParticipants).Select(
                _ => AddressUtil.CreateAgentAddress()).ToList();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMintAsset(world, StakeState.DeriveAddress(guildMasterAddress), GG * 100);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = guildParticipantAddresses.Select((addr, idx) => (addr, idx)).Aggregate(world, (w, item) =>
            {
                w = EnsureToMintAsset(w, item.addr, Mead * 100);
                w = EnsureToMintAsset(w, StakeState.DeriveAddress(item.addr), GG * ((item.idx + 1) * 100));
                return EnsureToJoinGuild(w, guildAddress, item.addr, 1L);
            });

            // When
            var repository = new GuildRepository(world, new ActionContext());
            var guild = repository.GetGuild(guildAddress);
            var reward = NCG * 1000;
            repository.UpdateWorld(EnsureToMintAsset(repository.World, guild.RewardPoolAddress, reward));
            guild.CollectRewards(1);
            world = repository.World;

            var claimRewardGuild = new ClaimRewardGuild();
            world = claimRewardGuild.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildParticipantAddresses[0],
                BlockIndex = 2L,
            });

            world = claimRewardGuild.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildParticipantAddresses[0],
                BlockIndex = 2L,
            });

            world = claimRewardGuild.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildParticipantAddresses[0],
                BlockIndex = 3L,
            });

            //Then
            var expectedRepository = new GuildRepository(world, new ActionContext());
            var expectedGuild = expectedRepository.GetGuild(guildAddress);
            var bond = expectedRepository.GetBond(expectedGuild, guildParticipantAddresses[0]);
            var expectedReward = (reward * bond.Share).DivRem(expectedGuild.TotalShares).Quotient;
            var actualReward = world.GetBalance(guildParticipantAddresses[0], NCG);

            Assert.Equal(expectedReward, actualReward);
        }
    }
}
