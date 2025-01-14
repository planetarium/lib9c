using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Model.Stake
{
    public static class StakeStateUtils
    {
        public static bool TryMigrateV1ToV2(
            IWorldState state,
            Address stakeStateAddr,
            out StakeState stakeState)
        {
            var nullableStateState =
                MigrateV1ToV2(state.GetLegacyState(stakeStateAddr), state.GetGameConfigState());
            if (nullableStateState is null)
            {
                stakeState = default;
                return false;
            }

            stakeState = nullableStateState.Value;
            return true;
        }

        public static bool TryMigrateV1ToV2(
            IValue serialized,
            GameConfigState gameConfigState,
            out StakeState stakeState)
        {
            var nullableStateState = MigrateV1ToV2(serialized, gameConfigState);
            if (nullableStateState is null)
            {
                stakeState = default;
                return false;
            }

            stakeState = nullableStateState.Value;
            return true;
        }

        public static StakeState? MigrateV1ToV2(
            IValue serialized,
            GameConfigState gameConfigState)
        {
            if (serialized is null or Null)
            {
                return null;
            }

            // NOTE: StakeStateV2 is serialized as Bencodex List.
            if (serialized is List list)
            {
                return new StakeState(list);
            }

            // NOTE: StakeState is serialized as Bencodex Dictionary.
            if (serialized is not Dictionary dict)
            {
                return null;
            }

            // NOTE: Migration needs GameConfigState.
            if (gameConfigState is null)
            {
                return null;
            }

            // NOTE: Below is the migration logic from StakeState to StakeStateV2.
            //       The migration logic is based on the following assumptions:
            //       - The migration target is StakeState which is serialized as Bencodex Dictionary.
            //       - The started block index of StakeState is less than or equal to ActionObsoleteConfig.V200080ObsoleteIndex.
            //       - Migrated StakeStateV2 will be contracted by StakeStateV2.Contract.
            //       - StakeStateV2.Contract.StakeRegularFixedRewardSheetTableName is one of the following:
            //         - "StakeRegularFixedRewardSheet_V1"
            //         - "StakeRegularFixedRewardSheet_V2"
            //       - StakeStateV2.Contract.StakeRegularRewardSheetTableName is one of the following:
            //         - "StakeRegularRewardSheet_V1"
            //         - "StakeRegularRewardSheet_V2"
            //         - "StakeRegularRewardSheet_V3"
            //         - "StakeRegularRewardSheet_V4"
            //         - "StakeRegularRewardSheet_V5"
            //       - StakeStateV2.Contract.RewardInterval is StakeState.RewardInterval.
            //       - StakeStateV2.Contract.LockupInterval is StakeState.LockupInterval.
            //       - StakeStateV2.StartedBlockIndex is StakeState.StartedBlockIndex.
            //       - StakeStateV2.ReceivedBlockIndex is StakeState.ReceivedBlockIndex.
            var stakeStateV1 = new LegacyStakeState(dict);
            var stakeRegularFixedRewardSheetTableName =
                stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularFixedRewardSheet_V2_StartBlockIndex
                    ? "StakeRegularFixedRewardSheet_V1"
                    : "StakeRegularFixedRewardSheet_V2";
            string stakeRegularRewardSheetTableName;
            if (stakeStateV1.StartedBlockIndex <
                gameConfigState.StakeRegularRewardSheet_V2_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V1";
            }
            else if (stakeStateV1.StartedBlockIndex <
                     gameConfigState.StakeRegularRewardSheet_V3_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V2";
            }
            else if (stakeStateV1.StartedBlockIndex <
                     gameConfigState.StakeRegularRewardSheet_V4_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V3";
            }
            else if (stakeStateV1.StartedBlockIndex <
                     gameConfigState.StakeRegularRewardSheet_V5_StartBlockIndex)
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V4";
            }
            else
            {
                stakeRegularRewardSheetTableName = "StakeRegularRewardSheet_V5";
            }

            return new StakeState(
                stakeStateV1,
                new Contract(
                    stakeRegularFixedRewardSheetTableName: stakeRegularFixedRewardSheetTableName,
                    stakeRegularRewardSheetTableName: stakeRegularRewardSheetTableName,
                    rewardInterval: LegacyStakeState.RewardInterval,
                    lockupInterval: LegacyStakeState.LockupInterval));
        }

        public static bool TryMigrateV2ToV3(
            IActionContext context,
            IWorld world,
            Address stakeStateAddr,
            StakeState stakeState,
            [NotNullWhen(true)]
            out (IWorld world, StakeState newStakeState)? result
        )
        {
            if (stakeState.StateVersion != 2)
            {
                result = null;
                return false;
            }

            var goldCurrency = world.GetGoldCurrency();
            var goldBalance = world.GetBalance(stakeStateAddr, goldCurrency);
            var newStakeState = new StakeState(
                stakeState.Contract,
                stakeState.StartedBlockIndex,
                stakeState.ReceivedBlockIndex,
                stateVersion: 3);

            result = (
                world.MintAsset(context, stakeStateAddr,
                        FungibleAssetValue.Parse(Currencies.GuildGold,
                            goldBalance.GetQuantityString(true)))
                    .SetLegacyState(stakeStateAddr, newStakeState.Serialize()), newStakeState);
            return true;
        }
    }
}
