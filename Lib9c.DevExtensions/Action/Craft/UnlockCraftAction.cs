using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Lib9c.DevExtensions.Action.Craft
{
    [Serializable]
    [ActionType("unlock_craft_action")]
    public class UnlockCraftAction : GameAction
    {
        public Address AvatarAddress { get; set; }
        public ActionTypeAttribute ActionType { get; set; }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            int targetStage;

            if (ActionType.TypeIdentifier is Text text)
            {
                if (text.Value.Contains("combination_equipment"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                }
                else if (text.Value.Contains("combination_consumable"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
                }
                else if (text.Value.Contains("item_enhancement"))
                {
                    targetStage = GameConfig.RequireClearedStageLevel.ItemEnhancementAction;
                }
                else
                {
                    throw new InvalidActionFieldException(
                        $"{ActionType.TypeIdentifier} is not valid action");
                }
            }
            else
            {
                throw new InvalidActionFieldException(
                    $"{ActionType.TypeIdentifier} is not valid action");
            }

            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            avatarState.worldInformation = new WorldInformation(
                context.BlockIndex,
                states.GetSheet<WorldSheet>(),
                targetStage
            );
            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["typeIdentifier"] = ActionType.TypeIdentifier,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            ActionType = new ActionTypeAttribute(plainValue["typeIdentifier"].ToDotnetString());
        }
    }
}
