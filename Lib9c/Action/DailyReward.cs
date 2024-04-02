using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1828
    /// Updated at https://github.com/planetarium/lib9c/pull/1828
    /// </summary>
    [Serializable]
    [ActionType("daily_reward7")]
    public class DailyReward : GameAction, IDailyRewardV1
    {
        public Address avatarAddress;
        public const string AvatarAddressKey = "a";

        Address IDailyRewardV1.AvatarAddress => avatarAddress;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward exec started", addressesHex);

            if (!states.TryGetDailyRewardInfo(context.Signer, avatarAddress, out var dailyRewardInfo))
            {
                var agentState = states.GetAgentState(context.Signer);
                if (agentState is not null && agentState.avatarAddresses.ContainsValue(avatarAddress))
                {
                    dailyRewardInfo =
                        new DailyRewardModule.DailyRewardInfo(context.Signer, 0L);
                }
                else
                {
                    throw new FailedLoadStateException("failed to load agent state.");
                }
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            if (context.BlockIndex < dailyRewardInfo.ReceivedBlockIndex + gameConfigState.DailyRewardInterval)
            {
                var sb = new StringBuilder()
                    .Append($"{addressesHex}Not enough block index to receive daily rewards.")
                    .Append(
                        $" Expected: Equals or greater than ({dailyRewardInfo.ReceivedBlockIndex + gameConfigState.DailyRewardInterval}).")
                    .Append($" Actual: ({context.BlockIndex})");
                throw new RequiredBlockIndexException(sb.ToString());
            }

            dailyRewardInfo.ReceivedBlockIndex = context.BlockIndex;

            if (gameConfigState.DailyRuneRewardAmount > 0)
            {
                states = states.MintAsset(
                    context,
                    avatarAddress,
                    RuneHelper.DailyRewardRune * gameConfigState.DailyRuneRewardAmount);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetDailyRewardInfo(avatarAddress, dailyRewardInfo)
                .SetActionPoint(avatarAddress, gameConfigState.ActionPointMax);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [AvatarAddressKey] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
