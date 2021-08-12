using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("migration_weekly_arena_state")]
    public class MigrationWeeklyArenaState : GameAction
    {
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(Addresses.ActivatedAccount, MarkChanged);
            }

            CheckPermission(context);

            Log.Debug($"Start {nameof(MigrationWeeklyArenaState)}");
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) context.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var weekly2 = new WeeklyArenaState2(index);
            Log.Debug($"target count: {weekly.Count}");
#pragma warning disable LAA1002
            foreach (var kv in weekly)
#pragma warning restore LAA1002
            {
                var info = kv.Value;
                var infoAddress = weekly2.address.Derive(info.AvatarAddress.ToHex());
                var info2 = new ArenaInfo2(info);
                weekly2.Update(info.AvatarAddress);
                states = states.SetState(infoAddress, info2.Serialize());
            }
            weekly2.Update(weekly.ResetIndex);
            states = states.SetState(weekly2.address, weekly2.Serialize());
            Log.Debug($"Finished {nameof(MigrationWeeklyArenaState)}: result count: {weekly2.AvatarAddresses.Count}");
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
        }
    }
}
