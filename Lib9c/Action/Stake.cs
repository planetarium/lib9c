using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private static readonly ActivitySource ActivitySource = new ActivitySource("Lib9c.Action.Stake");

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

            using Activity activity = ActivitySource.StartActivity("Stake");
            activity?.AddTag("Signer", context.Signer.ToString());

            using (Activity _ = ActivitySource.StartActivity(
                "GetMonsterCollectionState",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                // NOTE: Restrict staking if there is a monster collection until now.
                if (states.GetAgentState(context.Signer) is { } agentState &&
                    states.TryGetLegacyState(MonsterCollectionState.DeriveAddress(
                        context.Signer,
                        agentState.MonsterCollectionRound), out Dictionary _))
                {
                    throw new MonsterCollectionExistingException();
                }
            }

            string addressesHex;
            Address stakeStateAddress;
            Currency currency;
            FungibleAssetValue currentBalance;
            FungibleAssetValue stakedBalance;
            FungibleAssetValue targetStakeBalance;
            Contract latestStakeContract;

            using (var stakePolicySheetActivity = ActivitySource.StartActivity(
                "GetStakePolicySheet",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                // NOTE: When the amount is less than 0.
                if (Amount < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(Amount),
                        "The amount must be greater than or equal to 0.");
                }

                addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
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

                stakeStateAddress = LegacyStakeState.DeriveAddress(context.Signer);
                currency = states.GetGoldCurrency();
                currentBalance = states.GetBalance(context.Signer, currency);
                stakedBalance = states.GetBalance(stakeStateAddress, currency);
                targetStakeBalance = currency * Amount;
                // NOTE: When the total balance is less than the target balance.
                if (currentBalance + stakedBalance < targetStakeBalance)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        context.Signer.ToHex(),
                        Amount,
                        currentBalance);
                }

                latestStakeContract = new Contract(stakePolicySheet);
            }

            StakeState stakeStateV2;

            using (var findStakeStateAndStakeFromNoneActivity = ActivitySource.StartActivity(
                "FindStakeStateAndStakeFromNone",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                // NOTE: When the staking state is not exist.
                if (!states.TryGetStakeState(context.Signer, out stakeStateV2))
                {
                    // NOTE: Cannot withdraw staking.
                    if (Amount == 0)
                    {
                        throw new StateNullException(ReservedAddresses.LegacyAccount, stakeStateAddress);
                    }

                    using (var contractNewStakeFromNoneActivity = ActivitySource.StartActivity(
                        "ContractNewStakeFromNone",
                        ActivityKind.Internal,
                        findStakeStateAndStakeFromNoneActivity?.Id ?? string.Empty))
                    {
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
                    }

                    return states;
                }
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
                var migrateV2toV3Activity = ActivitySource.StartActivity(
                   "MigrateV2toV3",
                   ActivityKind.Internal,
                   activity?.Id ?? string.Empty);

                if (!StakeStateUtils.TryMigrateV2ToV3(context, states, stakeStateAddress, stakeStateV2, out var result))
                {
                    throw new InvalidOperationException("Failed to migration. Unexpected situation.");
                }

                states = result.Value.world;

                migrateV2toV3Activity?.Dispose();
            }

            using (var contractNewStakeActivity = ActivitySource.StartActivity(
                "ContractNewStake",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty))
            {
                // NOTE: Contract a new staking.
                states = ContractNewStake(
                    context,
                    states,
                    stakeStateAddress,
                    stakedBalance,
                    targetStakeBalance,
                    latestStakeContract);
            }
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
            using var activity = ActivitySource.StartActivity("ContractNewStake");

            var getLastStakeStateActivity = ActivitySource.StartActivity(
               "GetLastStakeState",
               ActivityKind.Internal,
               activity?.Id ?? string.Empty);
            var stakeStateValue = new StakeState(latestStakeContract, context.BlockIndex).Serialize();
            var additionalBalance = targetStakeBalance - stakedBalance;
            var height = context.BlockIndex;
            var agentAddress = new AgentAddress(context.Signer);
            getLastStakeStateActivity?.Dispose();

            if (additionalBalance.Sign > 0)
            {
                var addStakeActivity = ActivitySource.StartActivity(
                   "AddStake",
                   ActivityKind.Internal,
                   activity?.Id ?? string.Empty);

                var getGuildCoinActivity = ActivitySource.StartActivity(
                   "GetGuildCoin",
                   ActivityKind.Internal,
                   addStakeActivity?.Id ?? string.Empty);
                var gg = GetGuildCoinFromNCG(additionalBalance);

                getGuildCoinActivity?.Dispose();

                var transferActivity = ActivitySource.StartActivity(
                   "GetGuildCoin",
                   ActivityKind.Internal,
                   addStakeActivity?.Id ?? string.Empty);
                state = state
                    .TransferAsset(context, context.Signer, stakeStateAddr, additionalBalance)
                    .MintAsset(context, stakeStateAddr, gg);
                transferActivity?.Dispose();

                var guildActivity = ActivitySource.StartActivity(
                   "Guild",
                   ActivityKind.Internal,
                   addStakeActivity?.Id ?? string.Empty);

                var createGuildRepoActivity = ActivitySource.StartActivity(
                       "CreateGuildRepo",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                var guildRepository = new GuildRepository(state, context);
                createGuildRepoActivity?.Dispose();

                if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                {
                    var getGuildActivity = ActivitySource.StartActivity(
                       "GetGuild",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                    getGuildActivity?.Dispose();

                    var delegateActivity = ActivitySource.StartActivity(
                       "Delegate",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    guildParticipant.Delegate(guild, gg, height);
                    delegateActivity?.Dispose();

                    var worldFromGuildRepoActivity = ActivitySource.StartActivity(
                       "WorldFromGuildRepo",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    state = guildRepository.World;
                    worldFromGuildRepoActivity?.Dispose();
                }

                guildActivity?.Dispose();
                addStakeActivity?.Dispose();
            }
            else if (additionalBalance.Sign < 0)
            {
                var subtractStakeActivity = ActivitySource.StartActivity(
                   "SubtractStake",
                   ActivityKind.Internal,
                   activity?.Id ?? string.Empty);

                var getGuildCoinActivity = ActivitySource.StartActivity(
                   "GetGuildCoin",
                   ActivityKind.Internal,
                   subtractStakeActivity?.Id ?? string.Empty);
                var gg = GetGuildCoinFromNCG(-additionalBalance);
                getGuildCoinActivity?.Dispose();

                var guildActivity = ActivitySource.StartActivity(
                   "Guild",
                   ActivityKind.Internal,
                   subtractStakeActivity?.Id ?? string.Empty);

                var createGuildRepoActivity = ActivitySource.StartActivity(
                    "CreateGuildRepo",
                    ActivityKind.Internal,
                    guildActivity?.Id ?? string.Empty);

                var guildRepository = new GuildRepository(state, context);
                createGuildRepoActivity?.Dispose();

                // TODO : [GuildMigration] Remove below code when the migration is done.
                if (guildRepository.TryGetGuildParticipant(agentAddress, out var guildParticipant))
                {
                    var getGuildActivity = ActivitySource.StartActivity(
                       "GetGuild",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    var guild = guildRepository.GetGuild(guildParticipant.GuildAddress);
                    getGuildActivity?.Dispose();

                    var getGuildDelegateeActivity = ActivitySource.StartActivity(
                       "GetGuildDelegateeActivity",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    var guildDelegatee = guildRepository.GetGuildDelegatee(guild.ValidatorAddress);
                    getGuildDelegateeActivity?.Dispose();

                    var shareFromFAVActivity = ActivitySource.StartActivity(
                       "ShareFromFAV",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    var share = guildDelegatee.ShareFromFAV(gg);
                    shareFromFAVActivity?.Dispose();

                    var getGuildDelegatorActivity = ActivitySource.StartActivity(
                       "GetGuildDelegatorActivity",
                       ActivityKind.Internal,
                       guildActivity?.Id ?? string.Empty);
                    var guildDelegator = guildRepository.GetGuildDelegator(agentAddress);
                    getGuildDelegatorActivity?.Dispose();

                    var unbondGuildActivity = ActivitySource.StartActivity(
                      "UnbondGuild",
                      ActivityKind.Internal,
                      guildActivity?.Id ?? string.Empty);
                    guildDelegatee.Unbond(guildDelegator, share, height);
                    unbondGuildActivity?.Dispose();

                    var createValidatorRepoActivity = ActivitySource.StartActivity(
                        "CreateValidatorRepo",
                        ActivityKind.Internal,
                        guildActivity?.Id ?? string.Empty);
                    var validatorRepository = new ValidatorRepository(guildRepository);
                    createValidatorRepoActivity?.Dispose();

                    var getValidatorDelegateeActivity = ActivitySource.StartActivity(
                        "GetValidatorDelegatee",
                        ActivityKind.Internal,
                        guildActivity?.Id ?? string.Empty);
                    var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
                    getValidatorDelegateeActivity?.Dispose();

                    var getValidatorDelegatorActivity = ActivitySource.StartActivity(
                        "GetValidatorDelegator",
                        ActivityKind.Internal,
                        guildActivity?.Id ?? string.Empty);
                    var validatorDelegator = validatorRepository.GetValidatorDelegator(guild.Address);
                    getValidatorDelegatorActivity?.Dispose();

                    var unbondValidatorActivity = ActivitySource.StartActivity(
                      "UnbondValidator",
                      ActivityKind.Internal,
                      guildActivity?.Id ?? string.Empty);
                    validatorDelegatee.Unbond(validatorDelegator, share, height);
                    unbondValidatorActivity?.Dispose();

                    var worldFromValidatorRepoActivity = ActivitySource.StartActivity(
                      "WorldFromValidatorepo",
                      ActivityKind.Internal,
                      guildActivity?.Id ?? string.Empty);
                    state = validatorRepository.World;
                    worldFromValidatorRepoActivity?.Dispose();

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

                guildActivity?.Dispose();
                subtractStakeActivity?.Dispose();
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
