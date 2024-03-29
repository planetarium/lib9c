using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class MaximumUndelegationEntriesException : System.Exception
    {
        public MaximumUndelegationEntriesException(Address address, long count)
            : base($"Undelegation {address} reached maximum entry size : {count}")
        {
        }
    }
}
