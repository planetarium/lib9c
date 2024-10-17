namespace Lib9c.Tests.Action.Guild
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.Module.ValidatorDelegation;
    using Nekoyume.TypedAddress;
    using Nekoyume.ValidatorDelegation;

    public abstract class GuildTestBase
    {
        protected static readonly Currency GG = Currencies.GuildGold;
        protected static readonly Currency Mead = Currencies.Mead;
        protected static readonly Currency NCG = Currency.Uncapped("NCG", 2, null);

        public GuildTestBase()
        {
            var world = new World(MockUtil.MockModernWorldState);
            var goldCurrencyState = new GoldCurrencyState(NCG);
            World = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
        }

        protected IWorld World { get; }

        protected static IWorld EnsureToMintAsset(
            IWorld world, Address address, FungibleAssetValue amount)
        {
            var actionContext = new ActionContext
            {
                PreviousState = world,
            };
            return world.MintAsset(actionContext, address, amount);
        }

        protected static IWorld EnsureToCreateValidator(
            IWorld world,
            PublicKey validatorPublicKey)
        {
            var validatorAddress = validatorPublicKey.Address;
            var commissionPercentage = 10;
            var actionContext = new ActionContext
            {
                Signer = validatorAddress,
            };

            var validatorRepository = new ValidatorRepository(world, actionContext);
            validatorRepository.CreateValidatorDelegatee(validatorPublicKey, commissionPercentage);

            return validatorRepository.World;
        }

        protected static IWorld EnsureToMakeGuild(
            IWorld world,
            GuildAddress guildAddress,
            AgentAddress guildMasterAddress,
            Address validatorAddress)
        {
            var actionContext = new ActionContext
            {
                Signer = guildMasterAddress,
            };
            var repository = new GuildRepository(world, actionContext);
            repository.MakeGuild(guildAddress, validatorAddress);
            return repository.World;
        }

        protected static IWorld EnsureToJoinGuild(
            IWorld world,
            GuildAddress guildAddress,
            AgentAddress agentAddress)
        {
            var actionContext = new ActionContext
            {
                Signer = agentAddress,
            };
            var repository = new GuildRepository(world, actionContext);
            repository.JoinGuild(guildAddress, agentAddress);
            return repository.World;
        }

        protected static IWorld EnsureToBanGuildMember(
            IWorld world,
            GuildAddress guildAddress,
            AgentAddress guildMasterAddress,
            AgentAddress agentAddress)
        {
            var actionContext = new ActionContext
            {
                Signer = agentAddress,
            };
            var repository = new GuildRepository(world, actionContext);
            repository.Ban(guildAddress, guildMasterAddress, agentAddress);
            return repository.World;
        }
    }
}
