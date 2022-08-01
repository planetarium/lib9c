using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    public class ClaimWordBossKillReward : GameAction
    {
        public Address AvatarAddress;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new [] {
                typeof(RuneSheet),
                typeof(RuneWeightSheet),
                typeof(WorldBossListSheet),
                typeof(WorldBossKillRewardSheet),
            });

            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            int raidId;
            try
            {
                raidId = worldBossListSheet.FindRaidIdByBlockIndex(context.BlockIndex);
            }
            catch (InvalidOperationException)
            {
                raidId = worldBossListSheet.FindPreviousRaidIdByBlockIndex(context.BlockIndex);
            }
            var raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            RaiderState raiderState = states.GetRaiderState(raiderAddress);
            int rank = WorldBossHelper.CalculateRank(raiderState.HighScore);
            var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(AvatarAddress, raidId);
            var rewardRecord = new WorldBossKillRewardRecord((List) states.GetState(worldBossKillRewardRecordAddress));
            Address worldBossAddress = Addresses.GetWorldBossAddress(raidId);
            var worldBossState = new WorldBossState((List) states.GetState(worldBossAddress));
            return states.SetWorldBossKillReward(
                worldBossKillRewardRecordAddress,
                rewardRecord,
                rank,
                worldBossState,
                sheets.GetSheet<RuneWeightSheet>(),
                sheets.GetSheet<WorldBossKillRewardSheet>(),
                sheets.GetSheet<RuneSheet>(),
                context.Random,
                    AvatarAddress
            );
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
