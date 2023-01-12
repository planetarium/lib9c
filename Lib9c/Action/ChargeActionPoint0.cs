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
using Serilog;

namespace Lib9c.Action
{
    [Serializable]
    [ActionObsolete(BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("charge_action_point")]
    public class ChargeActionPoint0 : GameAction
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

            if (!states.TryGetAgentAvatarStates(context.Signer, avatarAddress, out var _, out var avatarState))
            {
                return states;
            }

            var row = states.GetSheet<MaterialItemSheet>().Values.FirstOrDefault(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateMaterial(row);
            if (!avatarState.inventory.RemoveFungibleItem2(apStone))
            {
                Log.Error("{AddressesHex}Not enough item {ApStone}", addressesHex, apStone);
                return states;
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                return states;
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
