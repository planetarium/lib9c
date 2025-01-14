using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TableData;
using Nekoyume.ValidatorDelegation;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2097
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimStakeReward : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        private const string ActionTypeText = "claim_stake_reward9";

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward(Address avatarAddress) : this()
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward()
        {
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize());

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var stakeStateAddr = LegacyStakeState.DeriveAddress(context.Signer);

            var validatorRepository = new ValidatorRepository(states, context);
            var isValidator = validatorRepository.TryGetDelegatee(
                context.Signer, out var _);
            if (isValidator)
            {
                throw new InvalidOperationException(
                    "The validator cannot claim the stake reward.");
            }

            if (!states.TryGetStakeState(context.Signer, out var stakeStateV2))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(LegacyStakeState),
                    stakeStateAddr);
            }

            if (stakeStateV2.StateVersion == 2)
            {
                if (!StakeStateUtils.TryMigrateV2ToV3(
                        context,
                        states,
                        StakeState.DeriveAddress(context.Signer),
                        stakeStateV2, out var result))
                {
                    throw new InvalidOperationException(
                        "Failed to migrate stake state. Unexpected situation.");
                }

                states = result.Value.world;
                stakeStateV2 = result.Value.newStakeState;
            }

            if (stakeStateV2.ClaimableBlockIndex > context.BlockIndex)
            {
                throw new RequiredBlockIndexException(
                    ActionTypeText,
                    addressesHex,
                    context.BlockIndex);
            }

            if (!states.TryGetAvatarState(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            states = Claim(
                states,
                context,
                AvatarAddress,
                stakeStateAddr,
                avatarState,
                stakeStateV2);

            return states;
        }

        public static IWorld Claim(
            IWorld states,
            IActionContext context,
            Address avatarAddress,
            Address stakeStateAddress,
            AvatarState avatarState,
            StakeState stakeStateV2)
        {
            var sheets = states.GetSheets(sheetTuples: new[]
            {
                (
                    typeof(StakeRegularFixedRewardSheet),
                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName
                ),
                (
                    typeof(StakeRegularRewardSheet),
                    stakeStateV2.Contract.StakeRegularRewardSheetTableName
                ),
                (typeof(ConsumableItemSheet), nameof(ConsumableItemSheet)),
                (typeof(CostumeItemSheet), nameof(CostumeItemSheet)),
                (typeof(EquipmentItemSheet), nameof(EquipmentItemSheet)),
                (typeof(MaterialItemSheet), nameof(MaterialItemSheet)),
            });
            var stakeRegularFixedRewardSheet = sheets.GetSheet<StakeRegularFixedRewardSheet>();
            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            // NOTE:
            var ncg = states.GetGoldCurrency();
            var stakedNcg = states.GetStaked(stakeStateAddress);
            var stakingLevel = Math.Min(
                stakeRegularRewardSheet.FindLevelByStakedAmount(
                    context.Signer,
                    stakedNcg),
                stakeRegularRewardSheet.Keys.Max());
            var itemSheet = sheets.GetItemSheet();
            // The first reward is given at the claimable block index.
            var rewardSteps = stakeStateV2.ClaimableBlockIndex == context.BlockIndex
                ? 1
                : 1 + (int)Math.DivRem(
                    context.BlockIndex - stakeStateV2.ClaimableBlockIndex,
                    stakeStateV2.Contract.RewardInterval,
                    out _);

            // Fixed Reward
            var random = context.GetRandom();
#pragma warning disable LAA1002
            foreach (var pair in StakeRewardCalculator.CalculateFixedRewards(stakingLevel, random, stakeRegularFixedRewardSheet, itemSheet, rewardSteps))
#pragma warning restore LAA1002
            {
                avatarState.inventory.AddItem(pair.Key, pair.Value);
            }

            // Regular Reward
            var (itemResult, favResult) = StakeRewardCalculator.CalculateRewards(ncg, stakedNcg, stakingLevel, rewardSteps,
                stakeRegularRewardSheet, itemSheet, random);
#pragma warning disable LAA1002
            foreach (var pair in itemResult)
#pragma warning restore LAA1002
            {
                avatarState.inventory.AddItem(pair.Key, pair.Value);
            }

            foreach (var fav in favResult)
            {
                var rewardCurrency = fav.Currency;
                var recipient = Currencies.PickAddress(rewardCurrency, context.Signer, avatarAddress);
                states = states.MintAsset(context, recipient, fav);
            }

            // NOTE: update claimed block index.
            stakeStateV2 = new StakeState(
                stakeStateV2.Contract,
                stakeStateV2.StartedBlockIndex,
                context.BlockIndex);

            return states
                .SetLegacyState(stakeStateAddress, stakeStateV2.Serialize())
                .SetAvatarState(avatarAddress, avatarState);
        }
    }
}
