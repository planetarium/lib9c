namespace Lib9c.Tests.Util
{
    using System;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.Stake;
    using Nekoyume.TableData.Stake;

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
    }
}
