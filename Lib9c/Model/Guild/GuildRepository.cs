using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.TypedAddress;
using Nekoyume.Model.Stake;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildRepository
        : DelegationRepository<GuildRepository, GuildDelegatee, GuildDelegator>
    {
        private readonly Address guildAddress = Addresses.Guild;
        private readonly Address guildParticipantAddress = Addresses.GuildParticipant;

        private IAccount _guildAccount;
        private IAccount _guildParticipantAccount;

        public GuildRepository(ValidatorRepository repository)
            : this(repository.World, repository.ActionContext)
        {
        }

        public GuildRepository(IWorld world, IActionContext actionContext)
            : base(
                  world: world,
                  actionContext: actionContext,
                  delegateeAccountAddress: Addresses.GuildDelegateeMetadata,
                  delegatorAccountAddress: Addresses.GuildDelegatorMetadata,
                  delegateeMetadataAccountAddress: Addresses.GuildDelegateeMetadata,
                  delegatorMetadataAccountAddress: Addresses.GuildDelegatorMetadata,
                  bondAccountAddress: Addresses.GuildBond,
                  unbondLockInAccountAddress: Addresses.GuildUnbondLockIn,
                  rebondGraceAccountAddress: Addresses.GuildRebondGrace,
                  unbondingSetAccountAddress: Addresses.GuildUnbondingSet,
                  rewardBaseAccountAddress: Addresses.GuildRewardBase,
                  lumpSumRewardRecordAccountAddress: Addresses.GuildLumpSumRewardsRecord)
        {
            _guildAccount = world.GetAccount(guildAddress);
            _guildParticipantAccount = world.GetAccount(guildParticipantAddress);
        }

        public override IWorld World => base.World
            .SetAccount(guildAddress, _guildAccount)
            .SetAccount(guildParticipantAddress, _guildParticipantAccount);

        public override GuildDelegatee GetDelegatee(Address address)
            => new GuildDelegatee(address, this);

        public override GuildDelegator GetDelegator(Address address)
        {
            try
            {
                return new GuildDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new GuildDelegator(
                    address,
                    StakeState.DeriveAddress(address),
                    this);
            }
        }

        // public override void SetDelegatee(GuildDelegatee delegatee)
        //     => SetDelegateeMetadata(delegatee.Metadata);

        // public override void SetDelegator(GuildDelegator delegator)
        //     => SetDelegatorMetadata(delegator.Metadata);

        public Guild GetGuild(Address address)
            => _guildAccount.GetState(address) is IValue bencoded
                ? new Guild(
                    new GuildAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Guild does not exist.");

        public void SetGuild(Guild guild)
        {
            _guildAccount = _guildAccount.SetState(
                guild.Address, guild.Bencoded);
        }

        public GuildParticipant GetGuildParticipant(Address address)
            => _guildParticipantAccount.GetState(address) is IValue bencoded
                ? new GuildParticipant(
                    new AgentAddress(address),
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Guild participant does not exist.");

        public void SetGuildParticipant(GuildParticipant guildParticipant)
        {
            _guildParticipantAccount = _guildParticipantAccount.SetState(
                guildParticipant.Address, guildParticipant.Bencoded);
        }

        public void RemoveGuildParticipant(Address guildParticipantAddress)
        {
            _guildParticipantAccount = _guildParticipantAccount.RemoveState(guildParticipantAddress);
        }

        public override void UpdateWorld(IWorld world)
        {
            base.UpdateWorld(world);
            _guildAccount = world.GetAccount(guildAddress);
            _guildParticipantAccount = world.GetAccount(guildParticipantAddress);
        }
    }
}
