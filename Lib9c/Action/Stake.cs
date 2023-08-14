using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;
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
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);

            // Restrict staking if there is a monster collection until now.
            if (account.GetAgentState(context.Signer) is { } agentState &&
                account.TryGetState(MonsterCollectionState.DeriveAddress(
                    context.Signer,
                    agentState.MonsterCollectionRound), out Dictionary _))
            {
                throw new MonsterCollectionExistingException();
            }

            if (context.Rehearsal)
            {
                account = account.SetState(StakeState.DeriveAddress(context.Signer), MarkChanged)
                    .MarkBalanceChanged(
                        context,
                        GoldCurrencyMock,
                        context.Signer,
                        StakeState.DeriveAddress(context.Signer));
                return world.SetAccount(account);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake exec started", addressesHex);
            if (Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var stakeRegularRewardSheet = account.GetSheet<StakeRegularRewardSheet>();
            var minimumRequiredGold = stakeRegularRewardSheet.OrderedRows.Min(x => x.RequiredGold);
            if (Amount != 0 && Amount < minimumRequiredGold)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount));
            }

            var stakeStateAddress = StakeState.DeriveAddress(context.Signer);
            var currency = account.GetGoldCurrency();
            var currentBalance = account.GetBalance(context.Signer, currency);
            var stakedBalance = account.GetBalance(stakeStateAddress, currency);
            var targetStakeBalance = currency * Amount;
            if (currentBalance + stakedBalance < targetStakeBalance)
            {
                throw new NotEnoughFungibleAssetValueException(
                    context.Signer.ToHex(),
                    Amount,
                    currentBalance);
            }

            // Stake if it doesn't exist yet.
            if (!account.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                stakeState = new StakeState(stakeStateAddress, context.BlockIndex);
                account = account
                    .SetState(
                        stakeStateAddress,
                        stakeState.SerializeV2())
                    .TransferAsset(context, context.Signer, stakeStateAddress, targetStakeBalance);
                return world.SetAccount(account);
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
                    account = account
                        .SetState(stakeState.address, Null.Value)
                        .TransferAsset(context, stakeState.address, context.Signer, stakedBalance);
                    return world.SetAccount(account);
                }
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Stake Total Executed Time: {Elapsed}", addressesHex, ended - started);

            // Stake with more or less amount.
            account = account
                .TransferAsset(context, stakeState.address, context.Signer, stakedBalance)
                .TransferAsset(context, context.Signer, stakeState.address, targetStakeBalance)
                .SetState(
                    stakeState.address,
                    new StakeState(stakeState.address, context.BlockIndex).SerializeV2());
            return world.SetAccount(account);
        }
    }
}
