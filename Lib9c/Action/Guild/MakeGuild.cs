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
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var random = context.GetRandom();

            // TODO: Remove this check when to deliver features to users.
            if (context.Signer != GuildConfig.PlanetariumGuildOwner)
            {
                throw new InvalidOperationException(
                    $"This action is not allowed for {context.Signer}.");
            }

            var guildAddress = new GuildAddress(random.GenerateAddress());
            var signer = context.GetAgentAddress();

            return world.MakeGuild(guildAddress, signer);
        }
    }
}
