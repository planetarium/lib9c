using Libplanet.Action;
using Nekoyume.TypedAddress;

namespace Nekoyume.Extensions
{
    public static class ActionContextExtensions
    {
        public static AgentAddress GetAgentAddress(this IActionContext context)
        {
            return new AgentAddress(context.Signer);
        }
    }
}
