using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class NullUndelegationException : System.Exception
    {
        public NullUndelegationException(Address address)
            : base($"Undelegation {address} not found")
        {
        }
    }
}
