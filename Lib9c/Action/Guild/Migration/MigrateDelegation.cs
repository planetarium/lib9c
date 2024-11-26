using System;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Module.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Libplanet.Crypto;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to migrate guild delegation.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class MigrateDelegation : ActionBase
    {
        public const string TypeIdentifier = "migrate_delegation";

        private const string TargetKey = "t";

        public AgentAddress Target { get; private set; }

        [Obsolete("Don't call in code.", error: false)]
        public MigrateDelegation()
        {
        }

        public MigrateDelegation(AgentAddress target)
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

            // Migrate stake state from v2 to v3 (Mint guild gold for staking)
            var stakeStateAddr = LegacyStakeState.DeriveAddress(Target);
            if (world.TryGetStakeState(Target, out var stakeState)
                && stakeState.StateVersion == 2)
            {
                if (!StakeStateUtils.TryMigrateV2ToV3(
                    context,
                    world,
                    StakeState.DeriveAddress(Target),
                    stakeState, out var result))
                {
                    throw new InvalidOperationException(
                        "Failed to migrate stake state. Unexpected situation.");
                }

                world = result.Value.world;
            }


            // Migrate guild participant state from legacy to new
            var value = world.GetAccountState(Addresses.GuildParticipant).GetState(Target) as List;

            var repository = new GuildRepository(world, context);
            if (repository.GetJoinedGuild(GuildConfig.PlanetariumGuildOwner) is not { } planetariumGuildAddress)
            {
                throw new NullReferenceException("Planetarium guild is not found.");
            }

            if (!repository.TryGetGuild(planetariumGuildAddress, out var planetariumGuild))
            {
                throw new GuildMigrationFailedException("Planetarium guild is not found.");
            }

            if (planetariumGuild.GuildMasterAddress != GuildConfig.PlanetariumGuildOwner)
            {
                throw new GuildMigrationFailedException("Unexpected guild master.");
            }

            try
            {
                var legacyGuildParticipant = new LegacyGuildParticipant(value);
                var guildParticipant = new GuildParticipant(
                    Target,
                    legacyGuildParticipant.GuildAddress,
                    repository);
                repository.SetGuildParticipant(guildParticipant);

                // Migrate delegation
                var guild = repository.GetGuild(guildParticipant.GuildAddress);
                var guildGold = repository.GetBalance(guildParticipant.DelegationPoolAddress, Currencies.GuildGold);
                if (guildGold.RawValue > 0)
                {
                    repository.Delegate(guildParticipant.Address, guildGold);
                }

                return repository.World;
            }
            catch (Exception e)
            {
                if (e is FailedLoadStateException || e is NullReferenceException)
                {
                    var pledgeAddress = ((Address)Target).GetPledgeAddress();

                    // Patron contract structure:
                    // [0] = PatronAddress
                    // [1] = IsApproved
                    // [2] = Mead amount to refill.
                    if (!world.TryGetLegacyState(pledgeAddress, out List list) || list.Count < 3 ||
                        list[0] is not Binary || list[0].ToAddress() != MeadConfig.PatronAddress ||
                        list[1] is not Bencodex.Types.Boolean approved || !approved)
                    {
                        throw new GuildMigrationFailedException("Unexpected pledge structure.");
                    }

                    repository.JoinGuild(planetariumGuildAddress, Target);
                    return repository.World;
                }

                throw;
            }
        }
    }
}
