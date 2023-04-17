using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Org.BouncyCastle.Tls.Crypto;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    // Update hard fork PR URL after creating PR
    /// <summary>
    /// Hard forked at
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimStakeReward : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        private const string ActionTypeText = "claim_stake_reward4";

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward()
        {
        }

        private IAccountStateDelta ProcessReward(IActionContext context, IAccountStateDelta states,
            ref AvatarState avatarState,
            ItemSheet itemSheet, FungibleAssetValue stakedAmount,
            int rewardStep, int runeRewardStep,
            List<StakeRegularFixedRewardSheet.RewardInfo> fixedReward,
            List<StakeRegularRewardSheet.RewardInfo> regularReward)
        {
            var currency = stakedAmount.Currency;

            // Regular Reward
            foreach (var reward in regularReward)
            {
                switch (reward.Type)
                {
                    case StakeRegularRewardSheet.StakeRewardType.Item:
                        var (quantity, _) = stakedAmount.DivRem(currency * reward.Rate);
                        if (quantity < 1)
                        {
                            // If the quantity is zero, it doesn't add the item into inventory.
                            continue;
                        }

                        ItemSheet.Row row = itemSheet[reward.ItemId];
                        ItemBase item = row is MaterialItemSheet.Row materialRow
                            ? ItemFactory.CreateTradableMaterial(materialRow)
                            : ItemFactory.CreateItem(row, context.Random);
                        avatarState.inventory.AddItem(item, (int)quantity * rewardStep);
                        break;
                    case StakeRegularRewardSheet.StakeRewardType.Rune:
                        var runeReward = runeRewardStep *
                                         RuneHelper.CalculateStakeReward(stakedAmount, reward.Rate);
                        if (runeReward < 1 * RuneHelper.StakeRune)
                        {
                            continue;
                        }

                        states = states.MintAsset(AvatarAddress, runeReward);
                        break;
                    default:
                        break;
                }
            }

            // Fixed Reward
            foreach (var reward in fixedReward)
            {
                ItemSheet.Row row = itemSheet[reward.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, reward.Count * rewardStep);
            }

            return states;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            CheckActionAvailable(ClaimStakeReward3.ObsoletedIndex, context);
            // TODO: Uncomment this when new version of action is created
            // CheckObsolete(ObsoletedIndex, context);

            // Check condition
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!states.TryGetStakeState(context.Signer, out StakeState stakeState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(StakeState),
                    StakeState.DeriveAddress(context.Signer));
            }

            if (!stakeState.IsClaimable(context.BlockIndex, out int v1Step, out int v2Step))
            {
                throw new RequiredBlockIndexException(
                    ActionTypeText,
                    addressesHex,
                    context.BlockIndex);
            }

            // Get data
            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            var sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            });

            var currency = states.GetGoldCurrency();
            var stakedAmount = states.GetBalance(stakeState.address, currency);
            var stakeRegularRewardSheet =
                states.GetSheet<StakeRegularRewardSheet>(context.BlockIndex);
            // var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            int level =
                stakeRegularRewardSheet.FindLevelByStakedAmount(context.Signer, stakedAmount);
            ItemSheet itemSheet = sheets.GetItemSheet();
            var accumulatedRewards =
                stakeState.CalculateAccumulatedRewards(context.BlockIndex, out v1Step,
                    out v2Step);
            var accumulatedRuneRewards =
                stakeState.CalculateAccumulatedRuneRewards(context.BlockIndex, out int runeV1Step,
                    out int runeV2Step);

            if (v1Step > 0)
            {
                var fixedReward =
                    states.TryGetSheet<StakeRegularFixedRewardSheet>(out var fixedRewardSheet, context.BlockIndex)
                        ? fixedRewardSheet[level].Rewards
                        : new List<StakeRegularFixedRewardSheet.RewardInfo>();
                var regularRewardSheet = states.GetSheet<StakeRegularRewardSheet>();
                var regularReward = regularRewardSheet[level].Rewards;
                states = ProcessReward(context, states, ref avatarState, itemSheet,
                    stakedAmount, v1Step, runeV1Step, fixedReward, regularReward);
            }

            if (v2Step > 0)
            {
                var fixedReward =
                    states.TryGetSheet<StakeRegularFixedRewardSheet>(
                        out var fixedRewardSheet, context.BlockIndex
                    )
                        ? fixedRewardSheet[level].Rewards
                        : new List<StakeRegularFixedRewardSheet.RewardInfo>();
                var regularRewardSheet =
                    states.GetSheet<StakeRegularRewardSheet>(context.BlockIndex);
                var regularReward = regularRewardSheet[level].Rewards;
                states = ProcessReward(context, states, ref avatarState, itemSheet,
                    stakedAmount, v2Step, runeV2Step, fixedReward, regularReward);
            }

            stakeState.Claim(context.BlockIndex);

            if (migrationRequired)
            {
                states = states
                    .SetState(avatarState.address, avatarState.SerializeV2())
                    .SetState(
                        avatarState.address.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        avatarState.address.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            return states
                .SetState(stakeState.address, stakeState.Serialize())
                .SetState(
                    avatarState.address.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
