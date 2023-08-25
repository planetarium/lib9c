#nullable enable
using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Module
{
    public static class AgentModule
    {
        public static AgentState? GetAgentState(IWorldState worldState, Address address)
        {
            var serializedAgent = AccountHelper.Resolve(worldState, address, Addresses.Agent);
            if (serializedAgent is null)
            {
                Log.Warning("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new AgentState((Bencodex.Types.Dictionary)serializedAgent);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid agent state ({0}): {1}",
                    address.ToHex(),
                    serializedAgent
                );

                return null;
            }
        }

        public static IWorld SetAgentState(IWorld world, Address agent, AgentState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Agent);
            account = account.SetState(agent, state.Serialize());
            return world.SetAccount(account);
        }
    }
}
