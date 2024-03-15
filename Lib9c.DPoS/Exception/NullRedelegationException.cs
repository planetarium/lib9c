using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class NullRedelegationException : System.Exception
    {
        public NullRedelegationException(Address address)
            : base($"Redelegation {address} not found")
        {
        }
    }
}
