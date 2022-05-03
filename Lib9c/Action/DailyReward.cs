using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/615
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("daily_reward6")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;
        public const string AvatarAddressKey = "a";

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out _, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = states.GetGameConfigState();
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

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());
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
