using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out _, out AvatarState avatarState))
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
                Log.Error(exc.Message);
                throw exc;
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                var exc = new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
                Log.Error(exc.Message);
                throw exc;
            }

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= gameConfigState.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = gameConfigState.ActionPointMax;
            }

            return states.SetState(avatarAddress, avatarState.Serialize());
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
