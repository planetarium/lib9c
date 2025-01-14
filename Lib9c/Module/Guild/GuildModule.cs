#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Evidence;
using Libplanet.Types.Tx;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.Guild
{
    public static class GuildModule
    {
        public static GuildRepository GetGuildRepository(this IWorld world, IActionContext context)
            => new GuildRepository(world, context);

        public static bool TryGetGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            [NotNullWhen(true)] out Model.Guild.Guild? guild)
        {
            try
            {
                guild = repository.GetGuild(guildAddress);
                return true;
            }
            catch
            {
                guild = null;
                return false;
            }
        }

        public static GuildRepository MakeGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            Address validatorAddress)
        {
            var signer = repository.ActionContext.Signer;
            if (repository.GetJoinedGuild(new AgentAddress(signer)) is not null)
            {
                throw new InvalidOperationException("The signer already has a guild.");
            }

            if (repository.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("Duplicated guild address. Please retry.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (!validatorRepository.TryGetValidatorDelegatee(validatorAddress, out _))
            {
                throw new InvalidOperationException("The validator does not exist.");
            }

            if (validatorRepository.TryGetValidatorDelegatee(signer, out var _))
            {
                throw new InvalidOperationException("Validator cannot make a guild.");
            }

            var guildMasterAddress = new AgentAddress(signer);
            var guild = new Model.Guild.Guild(
                guildAddress, guildMasterAddress, validatorAddress, repository);
            repository.SetGuild(guild);
            repository.JoinGuild(guildAddress, guildMasterAddress);

            return repository;
        }

        public static GuildRepository RemoveGuild(
            this GuildRepository repository)
        {
            var signer = new AgentAddress(repository.ActionContext.Signer);
            if (repository.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!repository.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (repository.GetGuildMemberCount(guildAddress) > 1)
            {
                throw new InvalidOperationException("There are remained participants in the guild.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetDelegatee(guild.ValidatorAddress);
            var bond = validatorRepository.GetBond(validatorDelegatee, guild.Address);
            if (bond.Share > 0)
            {
                throw new InvalidOperationException("The signer has a bond with the validator.");
            }

            repository.RemoveGuildParticipant(signer);
            repository.DecreaseGuildMemberCount(guild.Address);
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.Guild, account => account.RemoveState(guildAddress)));
            repository.RemoveBanList(guildAddress);

            return repository;
        }

        public static (BigInteger Share, BigInteger TotalShare, FungibleAssetValue TotalDelegated) GetDelegationInfo(
            this GuildRepository repository, Address agentAddress)
        {
            var guild = repository.GetGuild(agentAddress);
            var guildDelegatee = repository.GetDelegatee(guild.ValidatorAddress);
            var share = repository.GetBond(guildDelegatee, agentAddress).Share;
            return (share, guildDelegatee.TotalShares, guildDelegatee.TotalDelegated);
        }

        public static (BigInteger Share, BigInteger TotalShare, FungibleAssetValue TotalDelegated) GetDelegationInfo(
            this IWorld world, Address agentAddress)
            => new GuildRepository(world, new HallowActionContext()).GetDelegationInfo(agentAddress);

        public static (BigInteger Share, BigInteger TotalShare, FungibleAssetValue TotalDelegated) GetDelegationInfo(
            this IWorldState worldState, Address agentAddress)
            => GetDelegationInfo(new World(worldState), agentAddress);

        public static FungibleAssetValue FAVFromShare(BigInteger share, BigInteger totalShares, FungibleAssetValue totalDelegated)
            => totalShares == share
                ? totalDelegated
                : (totalDelegated * share).DivRem(totalShares).Quotient;

        public static FungibleAssetValue GetStaked(BigInteger share, BigInteger totalShares, FungibleAssetValue totalDelegated, Currency goldCurrency)
        {
            var delegated = FAVFromShare(share, totalShares, totalDelegated);
            return ConvertCurrency(delegated, goldCurrency).TargetFAV;
        }

        public static FungibleAssetValue GetStaked(this IWorld world, Address agentAddress)
        {
            var delegationInfo = world.GetDelegationInfo(agentAddress);
            return GetStaked(delegationInfo.Share, delegationInfo.TotalShare, delegationInfo.TotalDelegated, world.GetGoldCurrency());
        }

        public static (FungibleAssetValue TargetFAV, FungibleAssetValue Remainder)
            ConvertCurrency(FungibleAssetValue sourceFAV, Currency targetCurrency)
        {
            var sourceCurrency = sourceFAV.Currency;
            if (targetCurrency.DecimalPlaces < sourceCurrency.DecimalPlaces)
            {
                var d = BigInteger.Pow(10, sourceCurrency.DecimalPlaces - targetCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, sourceFAV.RawValue / d);
                var fav2 = FungibleAssetValue.FromRawValue(sourceCurrency, value.RawValue * d);
                return (value, sourceFAV - fav2);
            }
            else
            {
                var d = BigInteger.Pow(10, targetCurrency.DecimalPlaces - sourceCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, sourceFAV.RawValue * d);
                return (value, targetCurrency * 0);
            }
        }

        private class HallowActionContext : IActionContext
        {
            public Address Signer => throw new NotImplementedException();
            public TxId? TxId => throw new NotImplementedException();
            public Address Miner => throw new NotImplementedException();
            public long BlockIndex => throw new NotImplementedException();
            public int BlockProtocolVersion => throw new NotImplementedException();
            public IWorld PreviousState => throw new NotImplementedException();
            public bool IsPolicyAction => throw new NotImplementedException();
            public IReadOnlyList<ITransaction> Txs => throw new NotImplementedException();
            public IReadOnlyList<EvidenceBase> Evidence => throw new NotImplementedException();
            public BlockCommit LastCommit => throw new NotImplementedException();
            public int RandomSeed => throw new NotImplementedException();
            public FungibleAssetValue? MaxGasPrice => throw new NotImplementedException();
            public IRandom GetRandom() => throw new NotImplementedException();
        }
    }
}
