using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Lib9c.DevExtensions.Action.Craft
{
    [Serializable]
    [ActionType("unlock_recipe")]
    public class UnlockRecipe : GameAction
    {
        public Address AvatarAddress { get; set; }
        public int TargetStage { get; set; }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var recipeIdList = List.Empty;
            for (var i = 1; i <= TargetStage; i++)
            {
                recipeIdList = recipeIdList.Add(i.Serialize());
            }

            return states.SetLegacyState(
                AvatarAddress.Derive("recipe_ids"),
                recipeIdList
            );
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["targetStage"] = TargetStage.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            TargetStage = plainValue["targetStage"].ToInteger();
        }
    }
}
