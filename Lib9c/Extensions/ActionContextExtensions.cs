using Lib9c.TypedAddress;
using Libplanet.Action;

namespace Lib9c.Extensions
{
    public static class ActionContextExtensions
    {
        public static AgentAddress GetAgentAddress(this IActionContext context)
        {
            return new AgentAddress(context.Signer);
        }
    }
}
