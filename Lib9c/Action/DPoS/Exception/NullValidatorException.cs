using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class NullValidatorException : System.Exception
    {
        public NullValidatorException(Address address)
            : base($"Validator {address} not found")
        {
        }
    }
}
