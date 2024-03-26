using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class NullDelegationException : System.Exception
    {
        public NullDelegationException(Address address)
            : base($"Delegation {address} not found")
        {
        }
    }
}
