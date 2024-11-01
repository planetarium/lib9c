using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;

namespace Nekoyume.Model.Guild
{
    public class GuildValidatorRepository
        : DelegationRepository<GuildValidatorRepository, GuildValidatorDelegatee, GuildValidatorDelegator>
    {
        public GuildValidatorRepository(IWorld world, IActionContext actionContext)
            : base(
                  world: world,
                  actionContext: actionContext,
                  delegateeAccountAddress: Addresses.ValidatorDelegatee,
                  delegatorAccountAddress: Addresses.ValidatorDelegator,
                  delegateeMetadataAccountAddress: Addresses.GuildMetadata,
                  delegatorMetadataAccountAddress: Addresses.ValidatorDelegatorMetadata,
                  bondAccountAddress: Addresses.ValidatorBond,
                  unbondLockInAccountAddress: Addresses.ValidatorUnbondLockIn,
                  rebondGraceAccountAddress: Addresses.ValidatorRebondGrace,
                  unbondingSetAccountAddress: Addresses.ValidatorUnbondingSet,
                  lumpSumRewardRecordAccountAddress: Addresses.ValidatorLumpSumRewardsRecord)
        {
        }

        public GuildValidatorDelegatee GetGuildValidatorDelegatee(Address address)
        {
            try
            {
                return new GuildValidatorDelegatee(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new GuildValidatorDelegatee(address, address, this);
            }
        }

        public override GuildValidatorDelegatee GetDelegatee(Address address)
            => GetGuildValidatorDelegatee(address);

        public GuildValidatorDelegator GetGuildValidatorDelegator(Address address)
        {
            try
            {
                return new GuildValidatorDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new GuildValidatorDelegator(address, address, this);
            }
        }

        public override GuildValidatorDelegator GetDelegator(Address address)
            => GetGuildValidatorDelegator(address);

        public void SetGuildValidatorDelegatee(GuildValidatorDelegatee guildValidatorDelegatee)
        {
            SetDelegateeMetadata(guildValidatorDelegatee.Metadata);
        }

        public override void SetDelegatee(GuildValidatorDelegatee delegatee)
            => SetGuildValidatorDelegatee(delegatee);

        public void SetGuildValidatorDelegator(GuildValidatorDelegator guildValidatorDelegator)
        {
            SetDelegatorMetadata(guildValidatorDelegator.Metadata);
        }

        public override void SetDelegator(GuildValidatorDelegator delegator)
            => SetGuildValidatorDelegator(delegator);
    }
}
