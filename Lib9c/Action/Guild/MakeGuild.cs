using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public class MakeGuild : ActionBase
    {
        public const string TypeIdentifier = "make_guild";

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
            context.UseGas(1);
            var world = context.PreviousState;
            var random = context.GetRandom();

            var guildAddress = new GuildAddress(random.GenerateAddress());
            var signer = context.GetAgentAddress();

            if (world.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer already has a guild.");
            }

            if (world.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("Duplicated guild address. Please retry.");
            }

            return world.MakeGuild(guildAddress, signer)
                .JoinGuild(guildAddress, signer);
        }
    }
}
