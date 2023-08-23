using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("stake2")]
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
            ImmutableDictionary<string, IValue>.Empty.Add(AmountKey, (IValue) (Integer) Amount);

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Amount = plainValue[AmountKey].ToBigInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
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

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake exec started", addressesHex);
            if (Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var stakeRegularRewardSheet = LegacyModule.GetSheet<StakeRegularRewardSheet>(world);
            var minimumRequiredGold = stakeRegularRewardSheet.OrderedRows.Min(x => x.RequiredGold);
            if (Amount != 0 && Amount < minimumRequiredGold)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var stakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var currency = LegacyModule.GetGoldCurrency(world);
            var currentBalance = LegacyModule.GetBalance(world, context.Signer, currency);
            var stakedBalance = LegacyModule.GetBalance(world, stakeStateAddress, currency);
            var targetStakeBalance = currency * Amount;
            if (currentBalance + stakedBalance < targetStakeBalance)
            {
                throw new NotEnoughFungibleAssetValueException(
                    context.Signer.ToHex(),
                    Amount,
                    currentBalance);
            }

            // Stake if it doesn't exist yet.
            if (!LegacyModule.TryGetStakeState(world, context.Signer, out StakeState stakeState))
            {
                stakeState = new StakeState(stakeStateAddress, context.BlockIndex);
                world = LegacyModule.SetState(
                    world,
                    stakeStateAddress,
                    stakeState.SerializeV2());
                world = LegacyModule.TransferAsset(
                    world,
                    context,
                    context.Signer,
                    stakeStateAddress,
                    targetStakeBalance);
                return world;
            }

            if (stakeState.IsClaimable(context.BlockIndex))
            {
                throw new StakeExistingClaimableException();
            }

            if (!stakeState.IsCancellable(context.BlockIndex) &&
                (context.BlockIndex >= 4611070
                    ? targetStakeBalance <= stakedBalance
                    : targetStakeBalance < stakedBalance))
            {
                throw new RequiredBlockIndexException();
            }

            // Cancel
            if (Amount == 0)
            {
                if (stakeState.IsCancellable(context.BlockIndex))
                {
                    world = LegacyModule.SetState(world, stakeState.address, Null.Value);
                    world = LegacyModule.TransferAsset(
                        world,
                        context,
                        stakeState.address,
                        context.Signer,
                        stakedBalance);
                    return world;
                }
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake Total Executed Time: {Elapsed}", addressesHex, ended - started);

            // Stake with more or less amount.
            world = LegacyModule.TransferAsset(
                world,
                context,
                stakeState.address,
                context
                    .Signer,
                stakedBalance);
            world = LegacyModule.TransferAsset(
                world,
                context,
                context.Signer,
                stakeState
                    .address,
                targetStakeBalance);
            world = LegacyModule.SetState(
                world,
                stakeState.address,
                new StakeState(stakeState.address, context.BlockIndex).SerializeV2());
            return world;
        }
    }
}
