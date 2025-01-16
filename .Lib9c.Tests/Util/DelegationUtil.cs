namespace Lib9c.Tests.Util
{
    using System;
    using System.Numerics;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.Stake;
    using Nekoyume.Module;
    using Nekoyume.TableData.Stake;
    using Nekoyume.TypedAddress;
    using Nekoyume.ValidatorDelegation;

    public static class DelegationUtil
    {
        public static IWorld MintGuildGold(
            IWorld world, Address address, FungibleAssetValue amount, long blockHeight)
        {
            if (!amount.Currency.Equals(Currencies.GuildGold))
            {
                throw new ArgumentException(
                    $"The currency of the amount must be {Currencies.GuildGold}.",
                    nameof(amount)
                );
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
            };
            var poolAddress = StakeState.DeriveAddress(address);
            return world.MintAsset(actionContext, poolAddress, amount);
        }

        public static IWorld PromoteValidator(
            IWorld world,
            PublicKey validatorPublicKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            var promoteValidator = new PromoteValidator(validatorPublicKey, amount);

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
                BlockIndex = blockHeight,
            };
            return promoteValidator.ExecutePublic(actionContext);
        }

        public static IWorld SlashValidator(
            IWorld world,
            Address validatorAddress,
            BigInteger slashFactor,
            long blockHeight)
        {
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
            };

            var repository = new ValidatorRepository(world, actionContext);
            var validatorDelegatee = repository.GetDelegatee(validatorAddress);
            if (validatorDelegatee.Jailed)
            {
                return world;
            }

            validatorDelegatee.Slash(slashFactor, blockHeight, blockHeight);

            var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
            var guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
            guildDelegatee.Slash(slashFactor, blockHeight, blockHeight);
            repository.UpdateWorld(guildRepository.World);

            return repository.World;
        }

        public static IWorld MakeGuild(
            IWorld world, Address guildMasterAddress, Address validatorAddress, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
                Signer = guildMasterAddress,
                RandomSeed = Random.Shared.Next(),
            };
            var makeGuild = new MakeGuild(validatorAddress);
            return makeGuild.Execute(actionContext);
        }

        public static IWorld MakeGuild(
            IWorld world,
            Address guildMasterAddress,
            Address validatorAddress,
            long blockHeight,
            out GuildAddress guildAddress)
        {
            world = MakeGuild(world, guildMasterAddress, validatorAddress, blockHeight);
            var guildRepository = new GuildRepository(world, new ActionContext());
            var guildParticipant = guildRepository.GetGuildParticipant(guildMasterAddress);
            guildAddress = guildParticipant.GuildAddress;
            return world;
        }

        public static IWorld JoinGuild(
            IWorld world, Address agentAddress, GuildAddress guildAddress, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
                Signer = agentAddress,
                RandomSeed = Random.Shared.Next(),
            };
            var joinGuild = new JoinGuild(guildAddress);
            return joinGuild.Execute(actionContext);
        }

        public static FungibleAssetValue GetGuildCoinFromNCG(FungibleAssetValue balance)
        {
            return FungibleAssetValue.Parse(Currencies.GuildGold, balance.GetQuantityString(true));
        }

        public static IWorld EnsureValidatorPromotionReady(
            IWorld world, PublicKey validatorPublicKey, long blockHeight)
        {
            world = MintGuildGold(world, validatorPublicKey.Address, Currencies.GuildGold * 10, blockHeight);
            world = PromoteValidator(world, validatorPublicKey, Currencies.GuildGold * 10, blockHeight);
            return world;
        }

        public static IWorld EnsureGuildParticipentIsStaked(
            IWorld world,
            Address agentAddress,
            FungibleAssetValue ncg,
            StakePolicySheet stakePolicySheet,
            long blockHeight)
        {
            return world;
        }

        public static IWorld EnsureUnbondedClaimed(
            IWorld world, Address agentAddress, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                BlockIndex = blockHeight,
            };
            var claimUnbonded = new ClaimUnbonded();
            return claimUnbonded.Execute(actionContext);
            return world;
        }

        public static IWorld CreateAvatar(IWorld world, Address agentAddress, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                BlockIndex = blockHeight,
            };
            var createAvatar = new CreateAvatar
            {
                name = $"avatar{Random.Shared.Next()}",
            };

            return createAvatar.Execute(actionContext);
        }

        public static IWorld CreateAvatar(
            IWorld world, Address agentAddress, long blockHeight, out Address avatarAddress)
        {
            var agentState1 = world.GetAgentState(agentAddress);
            var index = agentState1?.avatarAddresses.Count ?? 0;
            world = CreateAvatar(world, agentAddress, blockHeight);
            var agentState2 = world.GetAgentState(agentAddress);
            avatarAddress = agentState2.avatarAddresses[index];
            return world;
        }

        public static IWorld Stake(
            IWorld world,
            Address agentAddress,
            Address avatarAddress,
            BigInteger amount,
            long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                BlockIndex = blockHeight,
            };
            var stake = new Stake(amount, avatarAddress);
            return stake.Execute(actionContext);
        }
    }
}
