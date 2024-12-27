using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TableData;
using Nekoyume.TableData.Stake;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("stake3")]
    public class Stake : GameAction, IStakeV1
    {
        internal BigInteger Amount { get; set; }

        BigInteger IStakeV1.Amount => Amount;

        public Stake(BigInteger amount)
        {
            Amount = amount >= 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public Stake()
        {
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty.Add(AmountKey, (IValue)(Integer)Amount);

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            Amount = plainValue[AmountKey].ToBigInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            var started = DateTimeOffset.UtcNow;
            GasTracer.UseGas(1);
            IWorld states = context.PreviousState;

            // NOTE: Restrict staking if there is a monster collection until now.
            if (states.GetAgentState(context.Signer) is { } agentState &&
                states.TryGetLegacyState(MonsterCollectionState.DeriveAddress(
                    context.Signer,
                    agentState.MonsterCollectionRound), out Dictionary _))
            {
                throw new MonsterCollectionExistingException();
            }

            // NOTE: When the amount is less than 0.
            if (Amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Amount),
                    "The amount must be greater than or equal to 0.");
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            Log.Debug("{AddressesHex}Stake exec started", addressesHex);
            if (!states.TryGetSheet<StakePolicySheet>(out var stakePolicySheet))
            {
                throw new StateNullException(ReservedAddresses.LegacyAccount, Addresses.GetSheetAddress<StakePolicySheet>());
            }

            var currentStakeRegularRewardSheetAddr = Addresses.GetSheetAddress(
                stakePolicySheet.StakeRegularRewardSheetValue);
            if (!states.TryGetSheet<StakeRegularRewardSheet>(
                    currentStakeRegularRewardSheetAddr,
                    out var stakeRegularRewardSheet))
            {
                throw new StateNullException(ReservedAddresses.LegacyAccount, currentStakeRegularRewardSheetAddr);
            }

            var minimumRequiredGold = stakeRegularRewardSheet.OrderedRows.Min(x => x.RequiredGold);
            // NOTE: When the amount is less than the minimum required gold.
            if (Amount != 0 && Amount < minimumRequiredGold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Amount),
                    $"The amount must be greater than or equal to {minimumRequiredGold}.");
            }

            var stakeStateAddress = LegacyStakeState.DeriveAddress(context.Signer);
            var currency = states.GetGoldCurrency();
            var currentBalance = states.GetBalance(context.Signer, currency);
            var stakedBalance = states.GetBalance(stakeStateAddress, currency);
            var targetStakeBalance = currency * Amount;
            // NOTE: When the total balance is less than the target balance.
            if (currentBalance + stakedBalance < targetStakeBalance)
            {
                throw new NotEnoughFungibleAssetValueException(
                    context.Signer.ToHex(),
                    Amount,
                    currentBalance);
            }

            var latestStakeContract = new Contract(stakePolicySheet);
            // NOTE: When the staking state is not exist.
            if (!states.TryGetStakeState(context.Signer, out var stakeStateV2))
            {
                // NOTE: Cannot withdraw staking.
                if (Amount == 0)
                {
                    throw new StateNullException(ReservedAddresses.LegacyAccount, stakeStateAddress);
                }

                // NOTE: Contract a new staking.
                states = ContractNewStake(
                    context,
                    states,
                    stakeStateAddress,
                    stakedBalance: currency * 0,
                    targetStakeBalance,
                    latestStakeContract);
                Log.Debug(
                    "{AddressesHex}Stake Total Executed Time: {Elapsed}",
                    addressesHex,
                    DateTimeOffset.UtcNow - started);
                return states;
            }

            // NOTE: Cannot anything if staking state is claimable.
            if (stakeStateV2.ClaimableBlockIndex <= context.BlockIndex)
            {
                var validatorRepository = new ValidatorRepository(states, context);
                var isValidator = validatorRepository.TryGetValidatorDelegatee(
                    context.Signer, out var validatorDelegatee);
                if (!isValidator)
                {
                    throw new StakeExistingClaimableException();
                }
            }

            // NOTE: When the staking state is locked up.
            // TODO: Remove this condition after the migration is done.
            if (stakeStateV2.CancellableBlockIndex > context.BlockIndex)
            {
                // NOTE: Cannot re-contract with less balance.
                if (targetStakeBalance < stakedBalance)
                {
                    throw new RequiredBlockIndexException();
                }
            }

            if (stakeStateV2.StateVersion == 2)
            {
                if (!StakeStateUtils.TryMigrateV2ToV3(context, states, stakeStateAddress, stakeStateV2, out var result))
                {
                    throw new InvalidOperationException("Failed to migration. Unexpected situation.");
                }

                states = result.Value.world;
            }

            // NOTE: Contract a new staking.
            states = ContractNewStake(
                context,
                states,
                stakeStateAddress,
                stakedBalance,
                targetStakeBalance,
                latestStakeContract);
            Log.Debug(
                "{AddressesHex}Stake Total Executed Time: {Elapsed}",
                addressesHex,
                DateTimeOffset.UtcNow - started);
            return states;
        }

        private static IWorld ContractNewStake(
            IActionContext context,
            IWorld state,
            Address stakeStateAddr,
            FungibleAssetValue stakedBalance,
            FungibleAssetValue targetStakeBalance,
            Contract latestStakeContract)
        {
            var stakeStateValue = new StakeState(latestStakeContract, context.BlockIndex).Serialize();
            var additionalBalance = targetStakeBalance - stakedBalance;
            var height = context.BlockIndex;
            var agentAddress = new AgentAddress(context.Signer);

            if (additionalBalance.Sign > 0)
            {
                var gg = GetGuildCoinFromNCG(additionalBalance);
                state = state
                    .TransferAsset(context, context.Signer, stakeStateAddr, additionalBalance)
                    .MintAsset(context, stakeStateAddr, gg);

                var guildRepository = new GuildRepository(state, context);
                if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                {
                    var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                    guildParticipant.Delegate(guild, gg, height);
                    state = guildRepository.World;
                }
            }
            else if (additionalBalance.Sign < 0)
            {
                var gg = GetGuildCoinFromNCG(-additionalBalance);

                var guildRepository = new GuildRepository(state, context);

                // TODO : [GuildMigration] Remove below code when the migration is done.
                if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                {
                    var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                    var guildDelegatee = guildRepository.GetDelegatee(guild.ValidatorAddress);
                    var share = guildDelegatee.ShareFromFAV(gg);

                    var guildDelegator = guildRepository.GetDelegator(agentAddress);
                    guildDelegatee.Unbond(guildDelegator, share, height);

                    var validatorRepository = new ValidatorRepository(guildRepository);
                    var validatorDelegatee = validatorRepository.GetDelegatee(guild.ValidatorAddress);
                    var validatorDelegator = validatorRepository.GetDelegator(guild.Address);
                    validatorDelegatee.Unbond(validatorDelegator, share, height);

                    state = validatorRepository.World;
                    state = state.BurnAsset(context, guildDelegatee.DelegationPoolAddress, gg);
                }
                else
                {
                    state = state.BurnAsset(context, stakeStateAddr, gg);
                }

                state = state
                    .TransferAsset(context, stakeStateAddr, context.Signer, -additionalBalance);

                // TODO : [GuildMigration] Revive below code when the migration is done.
                // if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                // {
                //     var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                //     var guildDelegatee = guildRepository.GetGuildDelegatee(guild.ValidatorAddress);
                //     var share = guildDelegatee.ShareFromFAV(gg);
                //     guildParticipant.Undelegate(guild, share, height);
                //     state = guildRepository.World;
                // }
                // else
                // {
                //     var delegateeAddress = Addresses.NonValidatorDelegatee;
                //     var delegatorAddress = context.Signer;
                //     var repository = new GuildRepository(state, context);
                //     var unbondLockInAddress = DelegationAddress.UnbondLockInAddress(delegateeAddress, repository.DelegateeAccountAddress, delegatorAddress);
                //     var unbondLockIn = new UnbondLockIn(
                //         unbondLockInAddress, ValidatorDelegatee.ValidatorMaxUnbondLockInEntries, delegateeAddress, delegatorAddress, null);
                //     unbondLockIn = unbondLockIn.LockIn(
                //         gg, height, height + ValidatorDelegatee.ValidatorUnbondingPeriod);
                //     repository.SetUnbondLockIn(unbondLockIn);
                //     repository.SetUnbondingSet(
                //         repository.GetUnbondingSet().SetUnbonding(unbondLockIn));
                //     state = repository.World;
                // }

                if ((stakedBalance + additionalBalance).Sign == 0)
                {
                    return state.MutateAccount(
                        ReservedAddresses.LegacyAccount,
                        state => state.RemoveState(stakeStateAddr));
                }
            }

            return state.SetLegacyState(stakeStateAddr, stakeStateValue);
        }

        private static FungibleAssetValue GetGuildCoinFromNCG(FungibleAssetValue balance)
        {
            return FungibleAssetValue.Parse(Currencies.GuildGold,
                balance.GetQuantityString(true));
        }
    }
}
