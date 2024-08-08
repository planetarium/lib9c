using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.TypedAddress;

namespace Nekoyume.Extensions
{
    public static class AgentAddressExtensions
    {
        public static PledgeAddress GetPledgeAddress(this AgentAddress address)
        {
            return new PledgeAddress(((Address)address).GetPledgeAddress());
        }
    }
}
