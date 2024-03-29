using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class NullRedelegationException : System.Exception
    {
        public NullRedelegationException(Address address)
            : base($"Redelegation {address} not found")
        {
        }
    }
}
