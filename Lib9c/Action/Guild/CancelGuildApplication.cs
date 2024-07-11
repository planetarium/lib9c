using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public class CancelGuildApplication : ActionBase
    {
        public const string TypeIdentifier = "cancel_guild_application";

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty);

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary)
            {
                throw new InvalidCastException();
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var signer = context.GetAgentAddress();

            return world.CancelGuildApplication(signer);
        }
    }
}
