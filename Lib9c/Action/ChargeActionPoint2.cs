using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.Policy;
using Lib9c.TableData.Item;
using Libplanet;
using Libplanet.Action;

namespace Lib9c.Action
{
    [Serializable]
    [ActionObsolete(BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("charge_action_point2")]
    public class ChargeActionPoint2 : GameAction
    {
        public Address avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            CheckObsolete(BlockPolicySource.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAvatarState(context.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var row = states.GetSheet<MaterialItemSheet>().Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            avatarState.actionPoint = gameConfigState.ActionPointMax;
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
