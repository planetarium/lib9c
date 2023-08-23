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
            var world = context.PreviousState;
            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                world = LegacyModule.MarkBalanceChanged(world, context, GoldCurrencyMock, avatarAddress);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward exec started", addressesHex);

            // FIXME: Should be replaced with `AvatarModule.TryGetAvatarState`
            if (!LegacyModule.TryGetState(world, avatarAddress, out Dictionary serializedAvatar))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            Address? agentAddress = null;
            bool useLegacyKey = false;
            if (serializedAvatar.ContainsKey(AgentAddressKey))
            {
                agentAddress = serializedAvatar[AgentAddressKey].ToAddress();
            }
            else if (serializedAvatar.ContainsKey(LegacyAgentAddressKey))
            {
                {
                    agentAddress = serializedAvatar[LegacyAgentAddressKey].ToAddress();
                    useLegacyKey = true;
                }
            }

            if (agentAddress is null || agentAddress != context.Signer)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            var indexKey = useLegacyKey ? LegacyDailyRewardReceivedIndexKey : DailyRewardReceivedIndexKey;
            var dailyRewardReceivedIndex = (long)(Integer)serializedAvatar[indexKey];
            if (context.BlockIndex < dailyRewardReceivedIndex + gameConfigState.DailyRewardInterval)
            {
                var sb = new StringBuilder()
                    .Append($"{addressesHex}Not enough block index to receive daily rewards.")
                    .Append(
                        $" Expected: Equals or greater than ({dailyRewardReceivedIndex + gameConfigState.DailyRewardInterval}).")
                    .Append($" Actual: ({context.BlockIndex})");
                throw new RequiredBlockIndexException(sb.ToString());
            }

            var apKey = useLegacyKey ? LegacyActionPointKey : ActionPointKey;
            serializedAvatar = serializedAvatar
                .SetItem(indexKey, context.BlockIndex)
                .SetItem(apKey, gameConfigState.ActionPointMax);

            if (gameConfigState.DailyRuneRewardAmount > 0)
            {
                world = LegacyModule.MintAsset(
                    world,
                    context,
                    avatarAddress,
                    RuneHelper.DailyRewardRune * gameConfigState.DailyRuneRewardAmount);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return LegacyModule.SetState(world, avatarAddress, serializedAvatar);
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
