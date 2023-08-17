using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;

namespace Nekoyume.Module
{
    public class AgentModule
    {
        public static AgentState GetState(IWorld world, Address agent)
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Agent);

            // TODO: Move AccountStateExtensions to Lib9c.Modules?
            return account.GetAgentState1(agent);
        }

        public static IWorld SetState(IWorld world, Address agent, AgentState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Agent);
            account = account.SetState(agent, state.Serialize());
            return world.SetAccount(account);
        }
    }
}
