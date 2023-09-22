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
                world = AvatarModule.MarkChanged(world, avatarAddress, true, false, false, false);
                world = LegacyModule.MarkBalanceChanged(world, context, GoldCurrencyMock, avatarAddress);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward exec started", addressesHex);

            if (!AvatarModule.TryGetAvatarState(
                world,
                context.Signer,
                avatarAddress,
                out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            if (avatarState is null || avatarState.agentAddress != context.Signer)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            if (context.BlockIndex < avatarState.dailyRewardReceivedIndex + gameConfigState.DailyRewardInterval)
            {
                var sb = new StringBuilder()
                    .Append($"{addressesHex}Not enough block index to receive daily rewards.")
                    .Append(
                        $" Expected: Equals or greater than ({avatarState.dailyRewardReceivedIndex + gameConfigState.DailyRewardInterval}).")
                    .Append($" Actual: ({context.BlockIndex})");
                throw new RequiredBlockIndexException(sb.ToString());
            }

            avatarState.dailyRewardReceivedIndex = context.BlockIndex;
            avatarState.actionPoint = gameConfigState.ActionPointMax;

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

            world = AvatarModule.SetAvatarState(
                world,
                avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            return world;
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
