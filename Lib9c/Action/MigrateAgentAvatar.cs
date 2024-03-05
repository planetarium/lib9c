using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("migrate_agent_avatar")]
    public class MigrateAgentAvatar : ActionBase
    {
        public List<Address> AgentAddresses;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "migrate_agent_avatar")
            .Add("values", new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"agent_addresses",
                        new List(AgentAddresses.Select(a => a.Serialize()))
                    ),
                }
            ));

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            AgentAddresses = ((List)asDict["agent_addresses"]).Select(v => new Address(v)).ToList();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var migrationStarted = DateTimeOffset.UtcNow;
            Log.Debug("Migration in block index #{Index} started", context.BlockIndex);

            const int maxAvatarCount = 3;
            var avatarAddresses = Enumerable
                .Range(0, AgentAddresses.Count * maxAvatarCount)
                .Select(i => AgentAddresses[i / maxAvatarCount]
                    .Derive(
                        string.Format(CultureInfo.InvariantCulture, CreateAvatar.DeriveFormat, i % maxAvatarCount)))
                .ToList();

            foreach (var address in AgentAddresses)
            {
                // Try migrating if not already migrated
                var started = DateTimeOffset.UtcNow;
                Log.Debug("Migrating agent {Address}", address);
                if (states.GetAccountState(Addresses.Agent).GetState(address) is null)
                {
                    Log.Debug("Getting agent {Address}", address);
                    var agentState = states.GetAgentState(address);
                    if (agentState is null) continue;
                    Log.Debug("Setting agent {Address} to modern account", address);
                    states = states.SetAgentState(address, agentState);
                }

                // Delete AgentState in Legacy
                Log.Debug("Deleting agent {Address} from legacy account", address);
                states = states.SetLegacyState(address, null);
                Log.Debug(
                    "Migrating agent {Address} finished in: {Elapsed}",
                    address,
                    DateTimeOffset.UtcNow - started);
            }

            foreach (var address in avatarAddresses)
            {
                var started = DateTimeOffset.UtcNow;
                Log.Debug("Migrating avatar {Address}", address);
                // Try migrating if not already migrated
                if (states.GetAccountState(Addresses.Avatar).GetState(address) is null)
                {
                    Log.Debug("Getting avatar {Address}", address);
                    var avatarState = states.GetAvatarState(address);
                    if (avatarState is null) continue;
                    Log.Debug("Setting avatar {Address} to modern account", address);
                    states = states.SetAvatarState(address, avatarState);
                }

                // Delete AvatarState in Legacy
                Log.Debug("Deleting avatar {Address} from legacy account", address);
                states = states.SetLegacyState(address, null);
                states = states.SetLegacyState(address.Derive(LegacyInventoryKey), null);
                states = states.SetLegacyState(address.Derive(LegacyQuestListKey), null);
                states = states.SetLegacyState(address.Derive(LegacyWorldInformationKey), null);
                Log.Debug(
                    "Migrating avatar {Address} finished in: {Elapsed}",
                    address,
                    DateTimeOffset.UtcNow - started);
            }

            Log.Debug(
                "Migration in block index #{Index} finished in: {Elapsed}",
                context.BlockIndex,
                DateTimeOffset.UtcNow - migrationStarted);
            return states;
        }
    }
}
