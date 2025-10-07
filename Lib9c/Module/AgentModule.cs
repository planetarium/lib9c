#nullable enable
using System;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Serilog;

namespace Lib9c.Module
{
    public static class AgentModule
    {
        public static AgentState? GetAgentState(this IWorldState worldState, Address address)
        {
            var account = worldState.GetAccountState(Addresses.Agent);
            var serializedAgent = account.GetState(address);
            if (serializedAgent is null)
            {
                Log.Warning("No agent state ({0})", address.ToHex());
                return null;
            }

            try
            {
                if (serializedAgent is List list)
                {
                    return new AgentState(list);
                }

                throw new InvalidCastException(
                    "Serialized agent state must be a dictionary or a list.");
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

        public static IWorld SetAgentState(this IWorld world, Address agent, AgentState state)
        {
            var account = world.GetAccount(Addresses.Agent);
            account = account.SetState(agent, state.SerializeList());
            return world.SetAccount(Addresses.Agent, account);
        }
    }
}
