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
        public GuildRepository(IDelegationRepository repository)
            : this(repository.World, repository.ActionContext)
        {
        }

        public GuildRepository(IWorld world, IActionContext actionContext)
            : base(
                  world: world,
                  actionContext: actionContext,
                  delegateeAccountAddress: Addresses.ValidatorDelegateeForGuildParticipantMetadata,
                  delegatorAccountAddress: Addresses.GuildParticipant,
                  delegateeMetadataAccountAddress: Addresses.ValidatorDelegateeForGuildParticipantMetadata,
                  delegatorMetadataAccountAddress: Addresses.GuildParticipantMetadata,
                  bondAccountAddress: Addresses.GuildBond,
                  unbondLockInAccountAddress: Addresses.GuildUnbondLockIn,
                  rebondGraceAccountAddress: Addresses.GuildRebondGrace,
                  unbondingSetAccountAddress: Addresses.GuildUnbondingSet,
                  lumpSumRewardRecordAccountAddress: Addresses.GuildLumpSumRewardsRecord)
        {
        }

        public ValidatorDelegateeForGuildParticipant GetValidatorDelegateeForGuildParticipant(Address address)
            => delegateeAccount.GetState(address) is IValue bencoded
                ? new ValidatorDelegateeForGuildParticipant(
                    address,
                    this)
                : throw new FailedLoadStateException("Validator delegatee for guild participant does not exist.");

        public override IDelegatee GetDelegatee(Address address)
            => GetValidatorDelegateeForGuildParticipant(address);

        public GuildParticipant GetGuildParticipant(Address address)
            => delegatorAccount.GetState(address) is IValue bencoded
                ? new GuildParticipant(
                    new AgentAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Guild participant does not exist.");

        public override IDelegator GetDelegator(Address address)
            => GetGuildParticipant(address);

        public void SetValidatorDelegateeForGuildParticipant(ValidatorDelegateeForGuildParticipant validatorDelegateeForGuild)
        {
            SetDelegateeMetadata(validatorDelegateeForGuild.Metadata);
        }

        public override void SetDelegatee(IDelegatee delegatee)
            => SetValidatorDelegateeForGuildParticipant(delegatee as ValidatorDelegateeForGuildParticipant);

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

        public Guild GetGuild(Address address)
            => delegateeAccount.GetState(address) is IValue bencoded
                ? new Guild(
                    new GuildAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Guild does not exist.");

        public void SetGuild(Guild guild)
        {
            delegateeAccount = delegateeAccount.SetState(
                guild.Address, guild.Bencoded);
        }
    }
}
