using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class NullDelegationException : System.Exception
    {
        public NullDelegationException(Address address)
            : base($"Delegation {address} not found")
        {
        }
    }
}
