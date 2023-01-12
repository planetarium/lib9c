using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Helper;
using Lib9c.Model.State;
using Lib9c.TableData;
using Lib9c.TableData.Character;
using Libplanet;
using Libplanet.Action;

namespace Lib9c.Action
{
    [Serializable]
    [ActionType("claim_world_boss_kill_reward")]
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
                typeof(WorldBossCharacterSheet),
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
            var row = sheets.GetSheet<WorldBossListSheet>().Values.First(r => r.Id == raidId);
            var bossRow = sheets.GetSheet<WorldBossCharacterSheet>().Values.First(x => x.BossId == row.BossId);
            int rank = WorldBossHelper.CalculateRank(bossRow, raiderState.HighScore);
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
                AvatarAddress,
                context.Signer
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
