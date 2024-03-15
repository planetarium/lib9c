using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class MaximumRedelegationEntriesException : System.Exception
    {
        public MaximumRedelegationEntriesException(Address address, long count)
            : base($"Redelegation {address} reached maximum entry size : {count}")
        {
        }
    }
}
