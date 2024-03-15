using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class NullUndelegationException : System.Exception
    {
        public NullUndelegationException(Address address)
            : base($"Undelegation {address} not found")
        {
        }
    }
}
