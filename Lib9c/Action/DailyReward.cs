using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Helper;
using Lib9c.Model.State;
using Lib9c.Module;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Serilog;

namespace Lib9c.Action
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
        public const long DailyRewardInterval = 2550L;
        public const int DailyRuneRewardAmount = 1;
        public const int ActionPointMax = 120;

        Address IDailyRewardV1.AvatarAddress => avatarAddress;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward exec started", addressesHex);

            var agentAddress = context.Signer;
            var avatarContains = false;
            for (int i = 0; i < GameConfig.SlotCount; i++)
            {
                var address = Addresses.GetAvatarAddress(agentAddress, i);
                if (address.Equals(avatarAddress))
                {
                    avatarContains = true;
                    break;
                }
            }

            if (!avatarContains)
            {
                throw new InvalidAddressException();
            }

            states.TryGetDailyRewardReceivedBlockIndex(avatarAddress,
                out var receivedBlockIndex);

            if (context.BlockIndex < receivedBlockIndex + DailyRewardInterval)
            {
                var sb = new StringBuilder()
                    .Append($"{addressesHex}Not enough block index to receive daily rewards.")
                    .Append(
                        $" Expected: Equals or greater than ({receivedBlockIndex + DailyRewardInterval}).")
                    .Append($" Actual: ({context.BlockIndex})");
                throw new RequiredBlockIndexException(sb.ToString());
            }

            receivedBlockIndex = context.BlockIndex;

            if (DailyRuneRewardAmount > 0)
            {
                states = states.MintAsset(
                    context,
                    avatarAddress,
                    RuneHelper.DailyRewardRune * DailyRuneRewardAmount);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}DailyReward Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetDailyRewardReceivedBlockIndex(avatarAddress, receivedBlockIndex)
                .SetActionPoint(avatarAddress, ActionPointMax);
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
