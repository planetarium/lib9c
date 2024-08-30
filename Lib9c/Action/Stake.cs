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
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Stake;
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
                    stakedBalance: null,
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
                throw new StakeExistingClaimableException();
            }

            // NOTE: When the staking state is locked up.
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

            // NOTE: Withdraw staking.
            if (Amount == 0)
            {
                return Refund(context, states, stakeStateAddress, context.Signer, stakedBalance)
                    .RemoveLegacyState(stakeStateAddress);
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
            FungibleAssetValue? stakedBalance,
            FungibleAssetValue targetStakeBalance,
            Contract latestStakeContract)
        {
            var newStakeState = new StakeState(latestStakeContract, context.BlockIndex);

            if (stakedBalance.HasValue)
            {
                state = Refund(context, state, stakeStateAddr, context.Signer, stakedBalance.Value);
            }

            return state
                .TransferAsset(context, context.Signer, stakeStateAddr, targetStakeBalance)
                .MintAsset(context, stakeStateAddr, GetGuildCoinFromNCG(targetStakeBalance))
                .SetLegacyState(stakeStateAddr, newStakeState.Serialize());
        }

        private static IWorld Refund(
            IActionContext context, IWorld world, Address stakeStateAddress, Address signer,
            FungibleAssetValue stakedBalance)
        {
            return world.BurnAsset(
                context,
                stakeStateAddress,
                GetGuildCoinFromNCG(stakedBalance)
            ).TransferAsset(
                context,
                stakeStateAddress,
                signer,
                stakedBalance);
        }

        private static FungibleAssetValue GetGuildCoinFromNCG(FungibleAssetValue balance)
        {
            return FungibleAssetValue.Parse(Currencies.GuildGold,
                balance.GetQuantityString(true));
        }
    }
}
