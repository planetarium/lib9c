namespace Lib9c.Tests.Action.Guild;

using System;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
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
    protected static readonly BigInteger SharePerGG
        = BigInteger.Pow(10, Currencies.GuildGold.DecimalPlaces);

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

        var guildRepository = new GuildRepository(validatorRepository);
        guildRepository.CreateGuildDelegatee(validatorAddress);

        return guildRepository.World;
    }

    protected static IWorld EnsureToTombstoneValidator(
        IWorld world,
        Address validatorAddress)
    {
        var actionContext = new ActionContext
        {
            Signer = validatorAddress,
        };

        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);
        validatorDelegatee.Tombstone();

        return validatorRepository.World;
    }

    protected static IWorld EnsureToSlashValidator(
        IWorld world,
        Address validatorAddress,
        BigInteger slashFactor,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            Signer = validatorAddress,
        };

        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);
        validatorDelegatee.Slash(slashFactor, blockHeight, blockHeight);

        var guildRepository = new GuildRepository(
            validatorRepository.World, validatorRepository.ActionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
        guildDelegatee.Slash(slashFactor, blockHeight, blockHeight);

        return guildRepository.World;
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
            BlockIndex = 0L,
        };
        var repository = new GuildRepository(world, actionContext);
        repository.MakeGuild(guildAddress, validatorAddress);
        return repository.World;
    }

    protected static IWorld EnsureToJoinGuild(
        IWorld world,
        GuildAddress guildAddress,
        AgentAddress guildParticipantAddress,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = guildParticipantAddress,
        };

        var repository = new GuildRepository(world, actionContext);
        repository.JoinGuild(guildAddress, guildParticipantAddress);
        return repository.World;
    }

    protected static IWorld EnsureToLeaveGuild(
        IWorld world,
        AgentAddress guildParticipantAddress,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = guildParticipantAddress,
        };

        var repository = new GuildRepository(world, actionContext);
        repository.LeaveGuild(guildParticipantAddress);
        return repository.World;
    }

    protected static IWorld EnsureToBanGuildMember(
        IWorld world,
        AgentAddress guildMasterAddress,
        AgentAddress agentAddress)
    {
        var actionContext = new ActionContext
        {
            Signer = agentAddress,
        };
        var repository = new GuildRepository(world, actionContext);
        repository.Ban(guildMasterAddress, agentAddress);
        return repository.World;
    }

    protected static IWorld EnsureToPrepareGuildGold(
        IWorld world,
        Address address,
        FungibleAssetValue amount)
    {
        if (!Equals(amount.Currency, Currencies.GuildGold))
        {
            throw new ArgumentException("Currency must be GG.", nameof(amount));
        }

        return EnsureToMintAsset(world, StakeState.DeriveAddress(address), amount);
    }

    protected static IWorld EnsureToSetGuildParticipant(
        IWorld world,
        AgentAddress agentAddress,
        GuildAddress guildAddress)
    {
        var repository = new GuildRepository(world, new ActionContext());
        var guildParticipant = new GuildParticipant(
            agentAddress, guildAddress, repository);
        repository.SetGuildParticipant(guildParticipant);
        return repository.World;
    }
}
