using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("daily_reward")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("DailyReward exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out _, out AvatarState avatarState))
            {
                throw new FailedLoadStateException("Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Debug("DailyReward Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException("Aborted as the game config was failed to load.");
            }

            sw.Stop();
            Log.Debug("DailyReward Get GameConfig: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= gameConfigState.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = gameConfigState.ActionPointMax;
            }

            states = states.SetState(avatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug("DailyReward SerializeAvatarState: {Elapsed}", sw.Elapsed);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("DailyReward Total Executed Time: {Elapsed}", ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
