using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    /// <summary>
    /// This action acts as if <see cref="MeadConfig.PatronAddress"/> is creating a guild.
    /// This is a temporary action that was originally created to bypass the need for
    /// <see cref="MeadConfig.PatronAddress"/> to sign a transaction directly
    /// to invoke <see cref="MakeGuild"/>, but was created to bypass that.
    /// </summary>
    /// <seealso cref="MakeGuild" />
    [ActionType(TypeIdentifier)]
    public class AdminMakeGuild : ActionBase
    {
        public const string TypeIdentifier = "admin_make_guild";

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Null.Value);

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"type_id", out var rawTypeId) ||
                rawTypeId is not Text typeId ||
                typeId != TypeIdentifier ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Null)
            {
                throw new InvalidCastException();
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            CheckPermission(context);

            var world = context.PreviousState;
            var random = context.GetRandom();

            var guildAddress = new GuildAddress(random.GenerateAddress());
            var guildMasterAddress = new AgentAddress(MeadConfig.PatronAddress);

            return world.MakeGuild(guildAddress, guildMasterAddress);
        }
    }
}
