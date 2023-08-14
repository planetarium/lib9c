using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Exceptions;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

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
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var world = context.PreviousState;
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
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

            var worldInformation = new WorldInformation(
                context.BlockIndex,
                account.GetSheet<WorldSheet>(),
                targetStage
            );
            account = account.SetState(
                AvatarAddress.Derive(LegacyWorldInformationKey),
                worldInformation.Serialize()
            );
            return world.SetAccount(account);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["typeIdentifier"] = ActionType.TypeIdentifier
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
