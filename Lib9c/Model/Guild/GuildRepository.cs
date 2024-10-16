using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class GuildRepository : DelegationRepository
    {
        public GuildRepository(IWorld world, IActionContext actionContext)
            : base(
                  world: world,
                  actionContext: actionContext,
                  delegateeAccountAddress: Addresses.Guild,
                  delegatorAccountAddress: Addresses.GuildParticipant,
                  delegateeMetadataAccountAddress: Addresses.GuildMetadata,
                  delegatorMetadataAccountAddress: Addresses.GuildParticipantMetadata,
                  bondAccountAddress: Addresses.GuildBond,
                  unbondLockInAccountAddress: Addresses.GuildUnbondLockIn,
                  rebondGraceAccountAddress: Addresses.GuildRebondGrace,
                  unbondingSetAccountAddress: Addresses.GuildUnbondingSet,
                  lumpSumRewardRecordAccountAddress: Addresses.GuildLumpSumRewardsRecord)
        {
        }

        public Guild GetGuild(Address address)
            => delegateeAccount.GetState(address) is IValue bencoded
                ? new Guild(
                    new GuildAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Guild does not exist.");

        public override IDelegatee GetDelegatee(Address address)
            => GetGuild(address);


        public GuildParticipant GetGuildParticipant(Address address)
            => delegatorAccount.GetState(address) is IValue bencoded
                ? new GuildParticipant(
                    new AgentAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Delegator does not exist.");

        public override IDelegator GetDelegator(Address address)
            => GetGuildParticipant(address);

        public void SetGuild(Guild guild)
        {
            delegateeAccount = delegateeAccount.SetState(
                guild.Address, guild.Bencoded);
            SetDelegateeMetadata(guild.Metadata);
        }

        public override void SetDelegatee(IDelegatee delegatee)
            => SetGuild(delegatee as Guild);

        public void SetGuildParticipant(GuildParticipant guildParticipant)
        {
            delegatorAccount = delegatorAccount.SetState(
                guildParticipant.Address, guildParticipant.Bencoded);
            SetDelegatorMetadata(guildParticipant.Metadata);
        }

        public override void SetDelegator(IDelegator delegator)
            => SetGuildParticipant(delegator as GuildParticipant);

        public void RemoveGuildParticipant(Address guildParticipantAddress)
        {
            delegatorAccount = delegatorAccount.RemoveState(guildParticipantAddress);
        }
    }
}
