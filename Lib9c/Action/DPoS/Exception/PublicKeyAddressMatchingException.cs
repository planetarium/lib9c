using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class PublicKeyAddressMatchingException : System.Exception
    {
        public PublicKeyAddressMatchingException(Address expected, PublicKey publicKey)
            : base($"publicKey {publicKey} does not match to address " +
                  $": Expected {expected}, found {publicKey.Address}")
        {
        }
    }
}
