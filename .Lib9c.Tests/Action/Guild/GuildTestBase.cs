namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

public abstract class GuildTestBase
{
    protected static readonly Currency GG = Currencies.GuildGold;
    protected static readonly FungibleAssetValue GGEpsilon = new FungibleAssetValue(GG, 0, 1);
    protected static readonly FungibleAssetValue GGZero = GG * 0;
    protected static readonly Currency Mead = Currencies.Mead;
    protected static readonly Currency NCG = Currency.Uncapped("NCG", 2, null);
    protected static readonly FungibleAssetValue NCGEpsilon = new FungibleAssetValue(NCG, 0, 1);
    protected static readonly BigInteger SharePerGG
        = BigInteger.Pow(10, Currencies.GuildGold.DecimalPlaces);

    private static readonly int _maximumIntegerLength = 15;
    private static readonly BigInteger _minimumNCGMajorUnit = 50;

    public GuildTestBase()
    {
        var world = new World(MockUtil.MockModernWorldState);
        var goldCurrencyState = new GoldCurrencyState(NCG);
        World = world
            .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
        World = InitializeUtil.InitializeTableSheets(World, isDevEx: false).states;
    }

    protected IWorld World { get; }

    protected static IWorld EnsureToInitializeValidator(
        IWorld world, PrivateKey validatorKey, FungibleAssetValue ncg, long blockHeight)
    {
        var validatorAddress = validatorKey.Address;
        var validatorPublicKey = validatorKey.PublicKey;
        world = EnsureToMintAsset(world, validatorAddress, ncg, blockHeight);
        world = EnsureToStake(world, validatorAddress, ncg, blockHeight);
        world = EnsureToCreateValidator(world, validatorPublicKey, NCGToGG(ncg));
        return world;
    }

    protected static IWorld EnsureToJailValidator(
        IWorld world, PrivateKey validatorPrivateKey, long period, long blockHeight)
    {
        var validatorAddress = validatorPrivateKey.Address;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorAddress,
        };

        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);
        validatorDelegatee.Jail(blockHeight + period);

        var guildRepository = new GuildRepository(
            validatorRepository.World, validatorRepository.ActionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
        guildDelegatee.Jail(blockHeight + period);

        return guildRepository.World;
    }

    protected static IWorld EnsureToInitializeAgent(
        IWorld world, AgentAddress agentAddress, long blockHeight)
    {
        return EnsureToInitializeAgent(world, agentAddress, NCG * 0, blockHeight);
    }

    protected static IWorld EnsureToInitializeAgent(
        IWorld world, AgentAddress agentAddress, FungibleAssetValue ncg, long blockHeight)
    {
        var avatarIndex = 0;
        if (ncg.RawValue > 0)
        {
            world = EnsureToMintAsset(world, agentAddress, ncg, blockHeight);
        }

        world = EnsureToCreateAvatar(world, agentAddress, avatarIndex, blockHeight);
        return world;
    }

    protected static IWorld EnsureToMintAsset(
        IWorld world, Address address, FungibleAssetValue amount, long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
        };
        return world.MintAsset(actionContext, address, amount);
    }

    protected static IWorld EnsureToTombstoneValidator(
        IWorld world,
        PrivateKey validatorPrivateKey,
        long blockHeight)
    {
        var validatorAddress = validatorPrivateKey.Address;
        var actionContext = new ActionContext
        {
            Signer = validatorAddress,
            BlockIndex = blockHeight,
        };

        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);
        validatorDelegatee.Tombstone();

        var guildRepository = new GuildRepository(
            validatorRepository.World, validatorRepository.ActionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
        guildDelegatee.Tombstone();

        return guildRepository.World;
    }

    protected static IWorld EnsureToSlashValidator(
        IWorld world, PrivateKey validatorPrivateKey, BigInteger slashFactor, long blockHeight)
    {
        var validatorAddress = validatorPrivateKey.Address;
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

    protected static IWorld EnsureToUndelegateValidator(
        IWorld world, PrivateKey validatorPrivateKey, BigInteger share, long blockHeight)
    {
        var validatorAddress = validatorPrivateKey.Address;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorAddress,
            BlockIndex = blockHeight,
        };
        var undelegateValidator = new UndelegateValidator(share);
        return undelegateValidator.Execute(actionContext);
    }

    protected static IWorld EnsureToMakeGuild(
        IWorld world,
        GuildAddress guildAddress,
        AgentAddress masterAddress,
        PrivateKey validatorPrivateKey,
        long blockHeight)
    {
        var validatorAddress = validatorPrivateKey.Address;
        var actionContext = new ActionContext
        {
            Signer = masterAddress,
            BlockIndex = blockHeight,
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

    protected static IWorld EnsureToBanMember(
        IWorld world,
        AgentAddress masterAddress,
        AgentAddress agentAddress,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            Signer = agentAddress,
            BlockIndex = blockHeight,
        };
        var repository = new GuildRepository(world, actionContext);
        repository.Ban(masterAddress, agentAddress);
        return repository.World;
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

    protected static IWorld EnsureToStake(
        IWorld world,
        AgentAddress agentAddress,
        int avatarIndex,
        FungibleAssetValue ncg,
        long blockHeight)
    {
        if (!ncg.Currency.Equals(NCG))
        {
            throw new ArgumentException("Currency must be NCG.", nameof(ncg));
        }

        if (ncg.MinorUnit != 0)
        {
            throw new ArgumentException("Minor unit must be zero.", nameof(ncg));
        }

        var amount = ncg.MajorUnit;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = blockHeight,
        };
        var agentState = world.GetAgentState(agentAddress);
        var avatarAddress = agentState.avatarAddresses[avatarIndex];
        var stake = new Stake(amount, avatarAddress);
        return stake.Execute(actionContext);
    }

    protected static IWorld EnsureToStakeValidator(
        IWorld world, PrivateKey privateKey, FungibleAssetValue ncg, long blockHeight)
    {
        return EnsureToStake(world, privateKey.Address, ncg, blockHeight);
    }

    protected static IWorld EnsureToStake(
        IWorld world, Address address, FungibleAssetValue ncg, long blockHeight)
    {
        if (!ncg.Currency.Equals(NCG))
        {
            throw new ArgumentException("Currency must be NCG.", nameof(ncg));
        }

        if (ncg.MinorUnit != 0)
        {
            throw new ArgumentException("Minor unit must be zero.", nameof(ncg));
        }

        return EnsureToStake(world, address, ncg.MajorUnit, blockHeight);
    }

    protected static IWorld EnsureToReleaseUnbonding(
        IWorld world,
        AgentAddress agentAddress,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = agentAddress,
        };

        var guildRepository = new GuildRepository(world, actionContext);
        var guildDelegator = guildRepository.GetDelegator(agentAddress);
        guildDelegator.ReleaseUnbondings(blockHeight);
        world = guildRepository.World;

        var validatorRepository = new ValidatorRepository(world, actionContext);
        var validatorDelegator = validatorRepository.GetDelegator(agentAddress);
        validatorDelegator.ReleaseUnbondings(blockHeight);

        return validatorRepository.World;
    }

    protected static FungibleAssetValue GetRandomFAV(Currency currency) => GetRandomFAV(currency, Random.Shared);

    protected static FungibleAssetValue GetRandomFAV(Currency currency, Random random)
    {
        var decimalLength = random.Next(currency.DecimalPlaces);
        var integerLength = random.Next(1, _maximumIntegerLength);
        var decimalPart = Enumerable.Range(0, decimalLength)
            .Aggregate(string.Empty, (s, i) => s + random.Next(10));
        var integerPart = Enumerable.Range(0, integerLength)
            .Aggregate(string.Empty, (s, i) => s + (i != 0 ? random.Next(10) : random.Next(1, 10)));
        var isDecimalZero = decimalLength == 0 || decimalPart.All(c => c == '0');
        var text = isDecimalZero is false ? $"{integerPart}.{decimalPart}" : integerPart;

        return FungibleAssetValue.Parse(currency, text);
    }

    protected static FungibleAssetValue GetRandomGG(Random random)
    {
        return GetRandomFAV(GG, random);
    }

    protected static FungibleAssetValue GetRandomNCG(Random random)
    {
        var ncg = GetRandomFAV(NCG, random);
        var majorUnit = ncg.MajorUnit;
        if (majorUnit < _minimumNCGMajorUnit)
        {
            majorUnit += _minimumNCGMajorUnit;
        }

        return new FungibleAssetValue(NCG, majorUnit, 0);
    }

    protected static PrivateKey GetRandomKey(Random random)
    {
        var bytes = Enumerable.Range(0, 32).Select(_ => (byte)random.Next(256)).ToArray();
        return new PrivateKey(bytes, informedConsent: true);
    }

    protected static AgentAddress GetRandomAgentAddress(Random random)
    {
        return new AgentAddress(GetRandomKey(random).Address);
    }

    protected static GuildAddress GetRandomGuildAddress(Random random)
    {
        return new GuildAddress(GetRandomKey(random).Address);
    }

    protected static BigInteger GetRandomSlashFactor(Random random)
    {
        return random.Next(0, 100);
    }

    protected static FungibleAssetValue SlashFAV(
        BigInteger slashFactor, params FungibleAssetValue[] favs)
    {
        if (favs.Length == 0)
        {
            throw new ArgumentException("FAVs must not be empty.", nameof(favs));
        }

        if (slashFactor.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slashFactor), slashFactor, "Slash factor must be positive.");
        }

        if (slashFactor == BigInteger.Zero)
        {
            return favs.Aggregate((a, b) => a + b);
        }

        var currency = favs[0].Currency;
        var sum = new FungibleAssetValue(currency, BigInteger.Zero, BigInteger.Zero);
        foreach (var fav in favs)
        {
            if (!currency.Equals(fav.Currency))
            {
                throw new ArgumentException("Currencies must be the same.", nameof(favs));
            }

            var value = fav.DivRem(slashFactor, out var remainder);
            if (remainder.Sign > 0)
            {
                value += FungibleAssetValue.FromRawValue(currency, 1);
            }

            sum += fav - value;
        }

        return sum;
    }

    protected static FungibleAssetValue NCGToGG(FungibleAssetValue ncg)
    {
        if (!ncg.Currency.Equals(NCG))
        {
            throw new ArgumentException("Currency must be NCG.", nameof(ncg));
        }

        var (fav, remainder) = GuildModule.ConvertCurrency(ncg, GG);
        if (remainder.Sign != 0)
        {
            throw new InvalidOperationException("Remainder must be zero.");
        }

        return fav;
    }

    protected static FungibleAssetValue GGToNCG(FungibleAssetValue gg)
    {
        if (!gg.Currency.Equals(GG))
        {
            throw new ArgumentException("Currency must be NCG.", nameof(gg));
        }

        var (fav, _) = GuildModule.ConvertCurrency(gg, NCG);
        return fav;
    }

    private static IWorld EnsureToStake(
        IWorld world, Address address, BigInteger amount, long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = address,
            BlockIndex = blockHeight,
        };
        var stake = new Stake(amount);
        return stake.Execute(actionContext);
    }

    private static IWorld EnsureToCreateAvatar(
        IWorld world,
        AgentAddress agentAddress,
        int avatarIndex,
        long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = blockHeight,
        };
        var createAvatar = new CreateAvatar
        {
            index = avatarIndex,
            name = $"avatar{avatarIndex}",
        };
        return createAvatar.Execute(actionContext);
    }

    private static IWorld EnsureToCreateValidator(
        IWorld world,
        PublicKey validatorPublicKey,
        FungibleAssetValue gg)
    {
        if (!gg.Currency.Equals(GG))
        {
            throw new ArgumentException("Currency must be GG.", nameof(gg));
        }

        var validatorAddress = validatorPublicKey.Address;
        var commissionPercentage = 10;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorAddress,
        };
        var promoteValidator = new PromoteValidator(
            publicKey: validatorPublicKey,
            fav: gg,
            commissionPercentage: commissionPercentage);

        return promoteValidator.Execute(actionContext);
    }

    protected sealed class FungibleAssetValueEqualityComparer
        : IEqualityComparer<FungibleAssetValue>
    {
        private readonly FungibleAssetValue _difference;

        public FungibleAssetValueEqualityComparer(FungibleAssetValue difference)
        {
            _difference = difference;
        }

        public bool Equals(FungibleAssetValue x, FungibleAssetValue y)
        {
            var diff = y - x;
            return diff.RawValue == 0 || diff == _difference;
        }

        public int GetHashCode([DisallowNull] FungibleAssetValue obj)
        {
            return obj.GetHashCode();
        }
    }
}
