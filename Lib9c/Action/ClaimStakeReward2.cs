using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1371
    /// </summary>
    [ActionType(ActionTypeText)]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    public class ClaimStakeReward2 : GameAction, IClaimStakeReward, IClaimStakeRewardV1
    {
        public const long ObsoletedIndex = 5_549_200L;
        private const string ActionTypeText = "claim_stake_reward2";

        internal Address AvatarAddress { get; private set; }

        Address IClaimStakeRewardV1.AvatarAddress => AvatarAddress;

        public ClaimStakeReward2(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public ClaimStakeReward2()
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var world = context.PreviousState;
            CheckObsolete(ObsoletedIndex, context);
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            if (!LegacyModule.TryGetStakeState(world, context.Signer, out StakeState stakeState))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(StakeState),
                    StakeState.DeriveAddress(context.Signer));
            }

            if (!stakeState.IsClaimable(context.BlockIndex))
            {
                throw new RequiredBlockIndexException(
                    ActionTypeText,
                    addressesHex,
                    context.BlockIndex);
            }

            if (!AvatarModule.TryGetAvatarStateV2(
                    world,
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

            var sheets = LegacyModule.GetSheets(
                world,
                sheetTypes: new[]
                {
                    typeof(StakeRegularRewardSheet),
                    typeof(ConsumableItemSheet),
                    typeof(CostumeItemSheet),
                    typeof(EquipmentItemSheet),
                    typeof(MaterialItemSheet),
                });

            var currency = LegacyModule.GetGoldCurrency(world);
            var stakedAmount = LegacyModule.GetBalance(world, stakeState.address, currency);
            var stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            int level =
                stakeRegularRewardSheet.FindLevelByStakedAmount(context.Signer, stakedAmount);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            ItemSheet itemSheet = sheets.GetItemSheet();
            var accumulatedRewards =
                stakeState.CalculateAccumulatedItemRewardsV1(context.BlockIndex);
            foreach (var reward in rewards)
            {
                var (quantity, _) = stakedAmount.DivRem(currency * reward.Rate);
                if (quantity < 1 || reward.Type != StakeRegularRewardSheet.StakeRewardType.Item)
                {
                    // If the quantity is zero, it doesn't add the item into inventory.
                    continue;
                }

                ItemSheet.Row row = itemSheet[reward.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, (int)quantity * accumulatedRewards);
            }

            if (LegacyModule.TryGetSheet<StakeRegularFixedRewardSheet>(
                    world,
                    out var stakeRegularFixedRewardSheet))
            {
                var fixedRewards = stakeRegularFixedRewardSheet[level].Rewards;
                foreach (var reward in fixedRewards)
                {
                    ItemSheet.Row row = itemSheet[reward.ItemId];
                    ItemBase item = row is MaterialItemSheet.Row materialRow
                        ? ItemFactory.CreateTradableMaterial(materialRow)
                        : ItemFactory.CreateItem(row, context.Random);
                    avatarState.inventory.AddItem(item, reward.Count * accumulatedRewards);
                }
            }

            stakeState.Claim(context.BlockIndex);

            if (migrationRequired)
            {
                world = AvatarModule.SetAvatarStateV2(world, avatarState.address, avatarState);
                world = LegacyModule.SetState(
                    world,
                    avatarState.address.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize());
                world = LegacyModule.SetState(
                    world,
                    avatarState.address.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());
            }

            world = LegacyModule.SetState(world, stakeState.address, stakeState.Serialize());
            world = LegacyModule.SetState(
                world,
                avatarState.address.Derive(LegacyInventoryKey),
                avatarState.inventory.Serialize());
            return world;
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
