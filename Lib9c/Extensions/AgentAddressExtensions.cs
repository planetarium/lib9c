using Lib9c.Action;
using Lib9c.TypedAddress;
using Libplanet.Crypto;

namespace Lib9c.Extensions
{
    public static class AgentAddressExtensions
    {
        public static PledgeAddress GetPledgeAddress(this AgentAddress address)
        {
            return new PledgeAddress(((Address)address).GetPledgeAddress());
        }
    }
}
