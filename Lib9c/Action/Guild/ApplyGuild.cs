using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild
{
    // TODO(GUILD-FEATURE): Enable again when Guild features are enabled.
    // [ActionType(TypeIdentifier)]
    public class ApplyGuild : ActionBase
    {
        public const string TypeIdentifier = "apply_guild";

        private const string GuildAddressKey = "ga";

        public ApplyGuild() {}

        public ApplyGuild(GuildAddress guildAddress)
        {
            GuildAddress = guildAddress;
        }

        public GuildAddress GuildAddress { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(GuildAddressKey, GuildAddress.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)GuildAddressKey, out var rawGuildAddress))
            {
                throw new InvalidCastException();
            }

            GuildAddress = new GuildAddress(rawGuildAddress);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var repository = new GuildRepository(world, context);
            var signer = context.GetAgentAddress();

            // TODO: Do something related with ConsensusPower delegation.

            repository.ApplyGuild(signer, GuildAddress);
            return repository.World;
        }
    }
}
