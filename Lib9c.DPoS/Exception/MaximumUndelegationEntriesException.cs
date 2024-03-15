using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class MaximumUndelegationEntriesException : System.Exception
    {
        public MaximumUndelegationEntriesException(Address address, long count)
            : base($"Undelegation {address} reached maximum entry size : {count}")
        {
        }
    }
}
