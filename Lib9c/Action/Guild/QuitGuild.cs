using System;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Model.Guild;
using Lib9c.Module.Guild;
using Libplanet.Action;
using Libplanet.Action.State;

namespace Lib9c.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public class QuitGuild : ActionBase
    {
        public const string TypeIdentifier = "quit_guild";

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Null.Value);

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Null)
            {
                throw new InvalidCastException();
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);
            var signer = context.GetAgentAddress();

            repository.LeaveGuild(signer);

            return repository.World;
        }
    }
}
