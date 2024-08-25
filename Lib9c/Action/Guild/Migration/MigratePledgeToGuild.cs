using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.Guild.Migration.Controls;
using Nekoyume.TypedAddress;

namespace Nekoyume.Action.Guild.Migration
{
    /// <summary>
    /// An action to migrate the pledge to the guild.
    /// But it is only for accounts contracted pledge with Planetarium (<see cref="MeadConfig.PatronAddress"/>).
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigratePledgeToGuild : ActionBase
    {
        public const string TypeIdentifier = "migrate_pledge_to_guild";

        private const string TargetKey = "t";

        public AgentAddress Target { get; private set; }

        [Obsolete("Don't call in code.", error: true)]
        public MigratePledgeToGuild()
        {
        }

        public MigratePledgeToGuild(AgentAddress target)
        {
            Target = target;
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetKey, Target.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)TargetKey, out var rawTarget) ||
                rawTarget is not Binary target)
            {
                throw new InvalidCastException();
            }

            Target = new AgentAddress(target);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;

            return GuildMigrationCtrl.MigratePlanetariumPledgeToGuild(world, Target);
        }
    }
}
