using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("charge_action_point")]
    public class ChargeActionPoint : GameAction
    {
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("ChargeActionPoint exec started.");
            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out var _, out var avatarState))
            {
                return states;
            }

            sw.Stop();
            Log.Debug("ChargeActionPoint Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var row = states.GetSheet<MaterialItemSheet>().Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem(apStone))
            {
                Log.Error($"Not enough item {apStone}");
                return states;
            }

            sw.Stop();
            Log.Debug("ChargeActionPoint Use ApStone: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                return states;
            }

            sw.Stop();
            Log.Debug("ChargeActionPoint Get GameConfigState: {Elapsed}", sw.Elapsed);

            avatarState.actionPoint = gameConfigState.ActionPointMax;

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("ChargeActionPoint Total Executed Time: {Elapsed}", ended - started);

            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
