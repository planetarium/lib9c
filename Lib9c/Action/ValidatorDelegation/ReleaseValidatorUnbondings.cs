using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Delegation;
using System;
using System.Linq;
using System.Collections.Immutable;
using Libplanet.Types.Assets;
using System.Numerics;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.Module;
using Nekoyume.Model.Stake;
using Lib9c;
using Nekoyume.Action.Guild.Migration.LegacyModels;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class ReleaseValidatorUnbondings : ActionBase
    {
        public ReleaseValidatorUnbondings() { }

        public ReleaseValidatorUnbondings(Address validatorDelegatee)
        {
        }

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;

            if (world.GetDelegationMigrationHeight() is null)
            {
                return world;
            }

            var repository = new GuildRepository(world, context);
            var unbondingSet = repository.GetUnbondingSet();
            var unbondings = unbondingSet.UnbondingsToRelease(context.BlockIndex);

            unbondings = unbondings.Select<IUnbonding, IUnbonding>(unbonding =>
            {
                switch (unbonding)
                {
                    case UnbondLockIn unbondLockIn:
                        unbondLockIn = unbondLockIn.Release(context.BlockIndex, out var releasedFAV);
                        repository.SetUnbondLockIn(unbondLockIn);
                        repository.UpdateWorld(
                            Unstake(repository.World, context, unbondLockIn, releasedFAV));
                        return unbondLockIn;
                    case RebondGrace rebondGrace:
                        rebondGrace = rebondGrace.Release(context.BlockIndex, out _);
                        repository.SetRebondGrace(rebondGrace);
                        return rebondGrace;
                    default:
                        throw new InvalidOperationException("Invalid unbonding type.");
                }
            }).ToImmutableArray();

            repository.SetUnbondingSet(unbondingSet.SetUnbondings(unbondings));

            return repository.World;
        }

        private IWorld Unstake(
            IWorld world, IActionContext context, UnbondLockIn unbondLockIn, FungibleAssetValue? releasedFAV)
        {
            var agentAddress = new AgentAddress(unbondLockIn.DelegatorAddress);
            var guildRepository = new GuildRepository(world, context);
            var goldCurrency = world.GetGoldCurrency();
            if (!IsValidator(world, context, unbondLockIn.DelegateeAddress))
            {
                if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                {
                    var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                    var stakeStateAddress = guildParticipant.DelegationPoolAddress;
                    var gg = world.GetBalance(stakeStateAddress, ValidatorDelegatee.ValidatorDelegationCurrency);
                    if (gg.Sign > 0)
                    {
                        var (ncg, _) = ConvertToGoldCurrency(gg, goldCurrency);
                        world = world.BurnAsset(context, stakeStateAddress, gg);
                        world = world.TransferAsset(
                            context, stakeStateAddress, agentAddress, ncg);
                    }
                }
                else
                {
                    if (releasedFAV is not FungibleAssetValue gg || gg.Sign < 1)
                    {
                        return world;
                    }

                    var stakeStateAddress = StakeState.DeriveAddress(agentAddress);
                    var (ncg, _) = ConvertToGoldCurrency(gg, goldCurrency);
                    world = world
                        .TransferAsset(context, stakeStateAddress, agentAddress, ncg)
                        .BurnAsset(context, stakeStateAddress, gg);
                }
            }

            return world;
        }

        private static bool IsValidator(IWorld world, IActionContext context, Address address)
        {
            var repository = new ValidatorRepository(world, context);
            try
            {
                repository.GetValidatorDelegatee(address);
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        private static (FungibleAssetValue Gold, FungibleAssetValue Remainder)
            ConvertToGoldCurrency(FungibleAssetValue fav, Currency targetCurrency)
        {
            var sourceCurrency = fav.Currency;
            if (targetCurrency.DecimalPlaces < sourceCurrency.DecimalPlaces)
            {
                var d = BigInteger.Pow(10, sourceCurrency.DecimalPlaces - targetCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, fav.RawValue / d);
                var fav2 = FungibleAssetValue.FromRawValue(sourceCurrency, value.RawValue * d);
                return (value, fav - fav2);
            }
            else
            {
                var d = BigInteger.Pow(10, targetCurrency.DecimalPlaces - sourceCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, fav.RawValue * d);
                return (value, targetCurrency * 0);
            }
        }
    }
}
