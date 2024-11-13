using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.Guild.Migration.LegacyModels;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to migrate the delegation height.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigrateDelegationHeight : ActionBase
    {
        public const string TypeIdentifier = "migrate_delegation_height";

        private const string HeightKey = "h";

        public long Height { get; private set; }

        [Obsolete("Don't call in code.", error: false)]
        public MigrateDelegationHeight()
        {
        }

        public MigrateDelegationHeight(long height)
        {
            Height = height;
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(HeightKey, Height));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)HeightKey, out var rawHeight) ||
                rawHeight is not Integer height)
            {
                throw new InvalidCastException();
            }

            Height = height;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;

            return world.SetDelegationMigrationHeight(Height);
        }
    }
}
