using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Nekoyume.Action.Extensions;
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
            context.UseGas(1);
            var world = context.PreviousState;

            // Restrict staking if there is a monster collection until now.
            if (AgentModule.GetAgentState(world, context.Signer) is { } agentState &&
                LegacyModule.TryGetState(
                    world,
                    MonsterCollectionState.DeriveAddress(
                        context.Signer,
                        agentState.MonsterCollectionRound),
                    out Dictionary _))
            {
                throw new MonsterCollectionExistingException();
            }

            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, StakeState.DeriveAddress(context.Signer), MarkChanged);
                world = LegacyModule.MarkBalanceChanged(
                    world,
                    context,
                    GoldCurrencyMock,
                    context.Signer,
                    StakeState.DeriveAddress(context.Signer));
                    return world;
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
            if (!LegacyModule.TryGetSheet<StakePolicySheet>(world, out var stakePolicySheet))
            {
                throw new StateNullException(Addresses.GetSheetAddress<StakePolicySheet>());
            }

            var currentStakeRegularRewardSheetAddr = Addresses.GetSheetAddress(
                stakePolicySheet.StakeRegularRewardSheetValue);
            if (!LegacyModule.TryGetSheet<StakeRegularRewardSheet>(
                world,
                currentStakeRegularRewardSheetAddr,
                out var stakeRegularRewardSheet))
            {
                throw new StateNullException(currentStakeRegularRewardSheetAddr);
            }

            var minimumRequiredGold = stakeRegularRewardSheet.OrderedRows.Min(x => x.RequiredGold);
            // NOTE: When the amount is less than the minimum required gold.
            if (Amount != 0 && Amount < minimumRequiredGold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Amount),
                    $"The amount must be greater than or equal to {minimumRequiredGold}.");
            }

            var stakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var currency = LegacyModule.GetGoldCurrency(world);
            var currentBalance = LegacyModule.GetBalance(world, context.Signer, currency);
            var stakedBalance = LegacyModule.GetBalance(world, stakeStateAddress, currency);
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
            if (!LegacyModule.TryGetStakeStateV2(world, context.Signer, out var stakeStateV2))
            {
                // NOTE: Cannot withdraw staking.
                if (Amount == 0)
                {
                    throw new StateNullException(stakeStateAddress);
                }

                // NOTE: Contract a new staking.
                world = ContractNewStake(
                    context,
                    world,
                    stakeStateAddress,
                    stakedBalance: null,
                    targetStakeBalance,
                    latestStakeContract);
                Log.Debug(
                    "{AddressesHex}Stake Total Executed Time: {Elapsed}",
                    addressesHex,
                    DateTimeOffset.UtcNow - started);
                return world;
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

            // NOTE: Withdraw staking.
            if (Amount == 0)
            {
                world = LegacyModule.SetState(world, stakeStateAddress, Null.Value);
                world = LegacyModule.TransferAsset(world, context, stakeStateAddress, context.Signer, stakedBalance);
                return world;
            }

            // NOTE: Contract a new staking.
            world = ContractNewStake(
                context,
                world,
                stakeStateAddress,
                stakedBalance,
                targetStakeBalance,
                latestStakeContract);
            Log.Debug(
                "{AddressesHex}Stake Total Executed Time: {Elapsed}",
                addressesHex,
                DateTimeOffset.UtcNow - started);
            return world;
        }

        private static IWorld ContractNewStake(
            IActionContext context,
            IWorld world,
            Address stakeStateAddr,
            FungibleAssetValue? stakedBalance,
            FungibleAssetValue targetStakeBalance,
            Contract latestStakeContract)
        {
            var newStakeState = new StakeStateV2(latestStakeContract, context.BlockIndex);
            if (stakedBalance.HasValue)
            {
                world = LegacyModule.TransferAsset(
                    world,
                    context,
                    stakeStateAddr,
                    context.Signer,
                    stakedBalance.Value);
            }

            world = LegacyModule.TransferAsset(
                world,
                context,
                context.Signer,
                stakeStateAddr,
                targetStakeBalance);
            world = LegacyModule.SetState(
                world,
                stakeStateAddr,
                newStakeState.Serialize());
            return world;
        }
    }
}
